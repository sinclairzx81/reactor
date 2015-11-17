/*--------------------------------------------------------------------------

Reactor

The MIT License (MIT)

Copyright (c) 2015 Haydn Paterson (sinclair) <haydn.developer@gmail.com>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

---------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Reactor.Web {

    /// <summary>
    /// Reactor web socket class.
    /// </summary>
    public class Socket {


        /// <summary>
        /// Readable state.
        /// </summary>
        internal enum State {
            /// <summary>
            /// The initial state of this stream. A stream
            /// in a pending state signals that the stream
            /// is waiting on the caller to issue a read request
            /// the the underlying resource, by attaching a
            /// OnRead, OnReadable, or calling Read().
            /// </summary>
            Pending,
            /// <summary>
            /// A stream in a reading state signals that the
            /// stream is currently requesting data from the
            /// underlying resource and is waiting on a 
            /// response.
            /// </summary>
            Reading,
            /// <summary>
            /// Indicates this stream has ended. Streams can end
            /// by way of reaching the end of the stream, or through
            /// error.
            /// </summary>
            Ended
        }

        #region Fields

        private Reactor.IDuplexable             duplexable;
        private Reactor.Event                   onconnect;
        private Reactor.Event                   onreadable;
        private Reactor.Event<Reactor.Buffer>   ondata;
        private Reactor.Event<System.Exception> onerror;
        private Reactor.Event                   onend;
        private State state;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new web socket from this duplex stream.
        /// </summary>
        /// <param name="duplexable"></param>
        public Socket(IDuplexable duplexable) {
            this.duplexable  = duplexable;
            this.onconnect   = Reactor.Event.Create();
            this.onreadable  = Reactor.Event.Create();
            this.ondata      = Reactor.Event.Create<Reactor.Buffer>();
            this.onerror     = Reactor.Event.Create<System.Exception>();
            this.onend       = Reactor.Event.Create();
            this.state       = State.Reading;
            this._Read();
        }

        #endregion

        #region Events

        /// <summary>
        /// Subscribes this action to the 'connect' event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnConnect(Reactor.Action action) {
            this.onconnect.On(action);
        }
        
        /// <summary>
        /// Unsubscribes this action to the 'connect' event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveConnect(Reactor.Action action) {
            this.onconnect.Remove(action);
        }

        /// <summary>
        /// Subscribes this action to the 'read' event. Attaching a data event 
        /// listener to a stream that has not been explicitly paused will 
        /// switch the stream into flowing mode and begin reading immediately. 
        /// Data will then be passed as soon as it is available.
        /// </summary>
        /// <param name="callback"></param>
        public void OnData (Reactor.Action<Reactor.Buffer> callback) {
            this.ondata.On(callback);
        }

        /// <summary>
        /// Subscribes this action once to the 'read' event. Attaching a data event 
        /// listener to a stream that has not been explicitly paused will 
        /// switch the stream into flowing mode and begin reading immediately. 
        /// Data will then be passed as soon as it is available.
        /// </summary>
        /// <param name="callback"></param>
        public void OnceData (Reactor.Action<Reactor.Buffer> callback) {
            this.ondata.Once(callback);
        }

        /// <summary>
        /// Unsubscribes this action from the 'read' event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveData (Reactor.Action<Reactor.Buffer> callback) {
            this.ondata.Remove(callback);
        }

        /// <summary>
        /// Subscribes this action to the 'error' event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnError (Reactor.Action<Exception> callback) {
            this.onerror.On(callback);
        }

        /// <summary>
        /// Unsubscribes this action from the 'error' event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveError (Reactor.Action<Exception> callback) {
            this.onerror.Remove(callback);
        }

        /// <summary>
        /// Subscribes this action to the 'end' event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnEnd (Reactor.Action callback) {
            this.onend.On(callback);
        }

        /// <summary>
        /// Unsubscribes this action from the 'end' event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveEnd (Reactor.Action callback) {
            this.onend.Remove(callback);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Writes this buffer to the stream.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        public Reactor.Future Write(Buffer buffer) {
            buffer.Locked = true;
            var writable = this.duplexable as IWritable;
            var frame = Reactor.Web.Frame.CreateFrame(
                Reactor.Web.Fin.Final,
                Reactor.Web.Opcode.TEXT,
                Reactor.Web.Mask.Unmask, buffer.ToArray(), false);
            return writable.Write(Reactor.Buffer.Create(frame.ToByteArray()));
        }

        /// <summary>
        /// Ends the stream.
        /// </summary>
        public Reactor.Future End() {
            var writable = this.duplexable as IWritable;
            var frame = Reactor.Web.Frame.CreateCloseFrame(
                Reactor.Web.Mask.Unmask,
                Reactor.Web.CloseStatusCode.Normal, "");
            writable.Write(Reactor.Buffer.Create(frame.ToByteArray()));
            return writable.End().Finally(this._End);
        }

        #endregion

        #region IWritable Extension

        /// <summary>
        /// Writes this data to the stream.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Future Write (byte[] buffer, int index, int count) {
            return this.Write(Reactor.Buffer.Create(buffer, 0, count));
        }

        /// <summary>
        /// Writes this data to the stream.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Future Write (byte[] buffer) {
            return this.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Writes this data to the stream.
        /// </summary>
        /// <param name="data"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Future Write (string data) {
            return this.Write(System.Text.Encoding.UTF8.GetBytes(data));
        }

        /// <summary>
        /// Writes this data to the stream.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Future Write (string format, params object[] args) {
            format = string.Format(format, args);
            return this.Write(System.Text.Encoding.UTF8.GetBytes(format));
        }

        /// <summary>
        /// Writes this data to the stream.
        /// </summary>
        /// <param name="data"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Future Write (byte data) {
            return this.Write(new byte[1] { data });
        }

        /// <summary>
        /// Writes a System.Boolean value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Future Write (bool value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.Int16 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Future Write (short value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.UInt16 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Future Write (ushort value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.Int32 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Future Write (int value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.UInt32 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Future Write (uint value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.Int64 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Future Write (long value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.UInt64 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Future Write (ulong value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.Single value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Future Write (float value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.Double value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Future Write (double value) {
            return this.Write(BitConverter.GetBytes(value));
        }
        
        /// <summary>
        /// Writes this data to the stream then ends the stream.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <returns>A future resolved when this write has completed and the stream has ended.</returns>
        public Reactor.Future End (byte[] buffer, int index, int count) {
            this.Write(Reactor.Buffer.Create(buffer, 0, count));
            return this.End();
        }

        /// <summary>
        /// Writes this data to the stream then ends the stream.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns>A future resolved when this write has completed and the stream has ended.</returns>
        public Reactor.Future End(byte[] buffer) {
            this.Write(buffer, 0, buffer.Length);
            return this.End();
        }

        /// <summary>
        /// Writes this data to the stream then ends the stream.
        /// </summary>
        /// <param name="data"></param>
        /// <returns>A future resolved when this write has completed and the stream has ended.</returns>
        public Reactor.Future End(string data) {
            this.Write(System.Text.Encoding.UTF8.GetBytes(data));
            return this.End();
        }

        /// <summary>
        /// Writes this data to the stream then ends the stream.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns>A future resolved when this write has completed and the stream has ended.</returns>
        public Reactor.Future End(string format, params object[] args) {
            format = string.Format(format, args);
            this.Write(System.Text.Encoding.UTF8.GetBytes(format));
            return this.End();
        }

        /// <summary>
        /// Writes this data to the stream then ends the stream.
        /// </summary>
        /// <param name="data"></param>
        /// <returns>A future resolved when this write has completed and the stream has ended.</returns>
        public Reactor.Future End(byte data) {
            this.Write(new byte[1] { data });
            return this.End();
        }

        /// <summary>
        /// Writes a System.Boolean value to the stream then ends the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed and the stream has ended.</returns>
        public Reactor.Future End(bool value) {
            this.Write(BitConverter.GetBytes(value));
            return this.End();
        }

        /// <summary>
        /// Writes a System.Int16 value to the stream then ends the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed and the stream has ended.</returns>
        public Reactor.Future End(short value) {
            this.Write(BitConverter.GetBytes(value));
            return this.End();
        }

        /// <summary>
        /// Writes a System.UInt16 value to the stream then ends the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed and the stream has ended.</returns>
        public Reactor.Future End(ushort value) {
            this.Write(BitConverter.GetBytes(value));
            return this.End();
        }

        /// <summary>
        /// Writes a System.Int32 value to the stream then ends the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed and the stream has ended.</returns>
        public Reactor.Future End(int value) {
            this.Write(BitConverter.GetBytes(value));
            return this.End();
        }

        /// <summary>
        /// Writes a System.UInt32 value to the stream then ends the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed and the stream has ended.</returns>
        public Reactor.Future End(uint value) {
            this.Write(BitConverter.GetBytes(value));
            return this.End();
        }

        /// <summary>
        /// Writes a System.Int64 value to the stream then ends the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed and the stream has ended.</returns>
        public Reactor.Future End(long value) {
            this.Write(BitConverter.GetBytes(value));
            return this.End();
        }

        /// <summary>
        /// Writes a System.UInt64 value to the stream then ends the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed and the stream has ended.</returns>
        public Reactor.Future End(ulong value) {
            this.Write(BitConverter.GetBytes(value));
            return this.End();
        }

        /// <summary>
        /// Writes a System.Single value to the stream then ends the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed and the stream has ended.</returns>
        public Reactor.Future End(float value) {
            this.Write(BitConverter.GetBytes(value));
            return this.End();
        }

        /// <summary>
        /// Writes a System.Double value to the stream then ends the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed and the stream has ended.</returns>
        public Reactor.Future End(double value) {
            this.Write(BitConverter.GetBytes(value));
            return this.End();
        }

        #endregion

        #region Internal

        /// <summary>
        /// Reads from the underlying duplexable.
        /// </summary>
        /// <returns></returns>
        private void _Read() {
            var readable = this.duplexable as IReadable;
            readable.OnData(buffer => {
                while(true) {
                    try {
                        var frame = Reactor.Web.Frame.Parse(buffer, true);
                        buffer.Read((int)frame.FrameLength);
                        if (frame.IsClose) {
                            readable.Pause();
                            this._End();
                        } else {
                            this.ondata.Emit(Reactor.Buffer.Create(frame.Payload.ToByteArray()));
                            if (buffer.Length == 0) {
                                break;
                            }
                        }
                    } catch { }
                }
            });
        }

        /// <summary>
        /// Handles errors from this socket.
        /// </summary>
        /// <param name="error"></param>
        private void _Error(Exception error) {
            if(this.state == State.Reading) {
                this.onerror.Emit(error);
                this._End();
            }
        }

        /// <summary>
        /// Internally ends this socket.
        /// </summary>
        /// <returns></returns>
        private void _End() {
            if (this.state == State.Reading) {
                this.state = State.Ended;
                this.onend.Emit();
            }
        }

        #endregion
    }
}
