/*--------------------------------------------------------------------------

Reactor.Fusion

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

using Reactor.Fusion.Protocol;
using Reactor.Fusion.Protocol;
using System;
using System.Collections.Generic;
using System.Net;

namespace Reactor.Fusion {
	
    /// <summary>
    /// Reactor Tao Socket.
    /// </summary>
    public class Socket : IDuplexable {

        #region States
        
        /// <summary>
        /// Readable state.
        /// </summary>
        internal enum ReadState {
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
            /// A stream in a paused state will bypass attempts
            /// to read on the underlying resource. A paused
            /// stream must be resumed by the caller.
            /// </summary>
            Paused,
            /// <summary>
            /// Indicates this stream has ended. Streams can end
            /// by way of reaching the end of the stream, or through
            /// error.
            /// </summary>
            Ended
        }

        /// <summary>
        /// Readable mode.
        /// </summary>
        internal enum ReadMode {
            /// <summary>
            /// Unknown read mode.
            /// </summary>
            Unknown,
            /// <summary>
            /// This stream is flowing.
            /// </summary>
            Flowing,
            /// <summary>
            /// This stream is non-flowing.
            /// </summary>
            NonFlowing
        }

        #endregion

        private ITransport                      transport;
        private IRandomizer                     randomizer;
        private Reactor.Queue                   queue;
        private Reactor.Event                   onconnect;
        private Reactor.Event                   ondrain;
        private Reactor.Event                   onreadable;
        private Reactor.Event<Reactor.Buffer>   onread;
        private Reactor.Event<System.Exception> onerror;
        private Reactor.Event                   onend;
        private ProtocolSender                  sender;
        private ProtocolReceiver                receiver;
        private Reactor.Buffer                  buffer;
        //private Reactor.Interval                poll;
        private ReadState                       readstate;
        private ReadMode                        readmode;
        //private bool                            corked;
		
        public Socket(ProtocolSender sender, ProtocolReceiver receiver) {
            this.queue      = Reactor.Queue.Create(1);
            this.onconnect  = Reactor.Event.Create();
            this.ondrain    = Reactor.Event.Create();
            this.onreadable = Reactor.Event.Create();
            this.onread     = Reactor.Event.Create<Reactor.Buffer>();
            this.onerror    = Reactor.Event.Create<System.Exception>();
            this.onend      = Reactor.Event.Create();
            this.readstate  = ReadState.Pending;
            this.readmode   = ReadMode.Unknown;
            //this.corked     = false;
            this.buffer     = Reactor.Buffer.Create ();
            this.sender     = sender;
            this.receiver   = receiver;
            this.receiver.OnData (this.OnData);
            this.receiver.OnFin  (this.OnFin);
        }

        public Socket(System.Net.IPEndPoint localEndPoint, System.Net.IPEndPoint remoteEndPoint) {
            this.randomizer = new Randomizer();
            this.queue      = Reactor.Queue.Create(1);
            this.onconnect  = Reactor.Event.Create();
            this.ondrain    = Reactor.Event.Create();
            this.onreadable = Reactor.Event.Create();
            this.onread     = Reactor.Event.Create<Reactor.Buffer>();
            this.onerror    = Reactor.Event.Create<System.Exception>();
            this.onend      = Reactor.Event.Create();
            this.readstate  = ReadState.Pending;
            this.readmode   = ReadMode.Unknown;
            //this.corked     = false;

            this.queue.Pause();
            this.transport = new ClientDatagramTransport (new PacketSerializer(), localEndPoint, remoteEndPoint);
            System.UInt32 send_isn = this.randomizer.Next();
            System.UInt32 recv_isn = 0;
            Reactor.Action<Packet> onread = null; onread = packet => {
                if (packet.type == PacketType.SynAck) {
                    var synack = (SynAck)packet;
                    send_isn = synack.ack;
                    recv_isn = synack.seq + 1;
                    transport.Write(new Ack(recv_isn));
                    this.transport.RemoveRead(onread);
                    this.buffer    = Reactor.Buffer.Create ();
                    this.sender   = new ProtocolSender  (transport, send_isn, 16);
                    this.receiver = new ProtocolReceiver(transport, recv_isn, 16);
                    this.receiver.OnData (this.OnData);
                    this.receiver.OnFin  (this.OnFin);
                    this.queue.Resume();
                    this.onconnect.Emit();
                }
            }; 
            this.transport.OnRead(onread);
            this.transport.Write(new Syn(send_isn));
        }

        #region Events

        /// <summary>
        /// Subscribes this action to the 'connect' event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnConnect (Reactor.Action callback) {
            this.onconnect.On(callback);
        }

        /// <summary>
        /// Unsubscribes this action from the 'connect' event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveConnect (Reactor.Action callback) {
            this.onconnect.Remove(callback);
        }

        /// <summary>
        /// Subscribes this action to the 'drain' event. The event indicates
        /// when a write operation has completed and the caller should send
        /// more data.
        /// </summary>
        /// <param name="callback"></param>
        public void OnDrain (Reactor.Action callback) {
            this.ondrain.On(callback);
        }

        /// <summary>
        /// Subscribes this action once to the 'drain' event. The event indicates
        /// when a write operation has completed and the caller should send
        /// more data.
        /// </summary>
        /// <param name="callback"></param>
        public void OnceDrain (Reactor.Action callback) {
            this.ondrain.Once(callback);
        }

        /// <summary>
        /// Unsubscribes from the OnDrain event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveDrain (Reactor.Action callback) {
            this.ondrain.Remove(callback);
        }

        /// <summary>
        /// Subscribes this action to the 'readable' event. When a chunk of 
        /// data can be read from the stream, it will emit a 'readable' event.
        /// Listening for a 'readable' event will cause some data to be read 
        /// into the internal buffer from the underlying resource. If a stream 
        /// happens to be in a 'paused' state, attaching a readable event will 
        /// transition into a pending state prior to reading from the resource.
        /// </summary>
        /// <param name="callback"></param>
        public void OnReadable (Reactor.Action callback) {
            this.queue.Run(next => {
                if(this.readmode == ReadMode.Unknown ||
                   this.readmode == ReadMode.NonFlowing) {
                    this.readmode = ReadMode.NonFlowing;
                    this.onreadable.On(callback);
                    if (this.readstate == ReadState.Pending) {
                        this.readstate = ReadState.Reading;
                        this._Read();
                    }
                } next();
            });
        }

        /// <summary>
        /// Subscribes this action once to the 'readable' event. When a chunk of 
        /// data can be read from the stream, it will emit a 'readable' event.
        /// Listening for a 'readable' event will cause some data to be read 
        /// into the internal buffer from the underlying resource. If a stream 
        /// happens to be in a 'paused' state, attaching a readable event will 
        /// transition into a pending state prior to reading from the resource.
        /// </summary>
        /// <param name="callback"></param>
        public void OnceReadable(Reactor.Action callback) {
            this.queue.Run(next => {
                if(this.readmode == ReadMode.Unknown ||
                   this.readmode == ReadMode.NonFlowing) {
                    this.readmode = ReadMode.NonFlowing;
                    this.onreadable.Once(callback);
                    if (this.readstate == ReadState.Pending) {
                        this.readstate = ReadState.Reading;
                        this._Read();
                    }
                } next();
            });
        }

        /// <summary>
        /// Unsubscribes this action from the 'readable' event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveReadable(Reactor.Action callback) {
            this.queue.Run(next => {
                if(this.readmode == ReadMode.Unknown ||
                   this.readmode == ReadMode.NonFlowing) {
                    this.readmode = ReadMode.NonFlowing;
                    this.onreadable.Remove(callback);
                } next();
            });
        }

        /// <summary>
        /// Subscribes this action to the 'read' event. Attaching a data event 
        /// listener to a stream that has not been explicitly paused will 
        /// switch the stream into flowing mode and begin reading immediately. 
        /// Data will then be passed as soon as it is available.
        /// </summary>
        /// <param name="callback"></param>
        public void OnRead (Reactor.Action<Reactor.Buffer> callback) {
            this.queue.Run(next => {
                if(this.readmode == ReadMode.Unknown ||
                   this.readmode == ReadMode.Flowing) {
                    this.readmode = ReadMode.Flowing;
                    this.onread.On(callback);
                    if (this.readstate == ReadState.Pending) {
                        this.readstate = ReadState.Reading;
                        this._Read();
                    }
                } next();
            });
        }

        /// <summary>
        /// Subscribes this action once to the 'read' event. Attaching a data event 
        /// listener to a stream that has not been explicitly paused will 
        /// switch the stream into flowing mode and begin reading immediately. 
        /// Data will then be passed as soon as it is available.
        /// </summary>
        /// <param name="callback"></param>
        public void OnceRead(Reactor.Action<Reactor.Buffer> callback) {
            this.queue.Run(next => {
                if(this.readmode == ReadMode.Unknown ||
                   this.readmode == ReadMode.Flowing) {
                    this.readmode = ReadMode.Flowing;
                    this.onread.Once(callback);
                    if (this.readstate == ReadState.Pending) {
                        this.readstate = ReadState.Reading;
                        this._Read();
                    }
                } next();
            });
        }

        /// <summary>
        /// Unsubscribes this action from the 'read' event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveRead (Reactor.Action<Reactor.Buffer> callback) {
            this.queue.Run(next => {
                if(this.readmode == ReadMode.Unknown ||
                   this.readmode == ReadMode.Flowing) {
                    this.readmode = ReadMode.Flowing;
                    this.onread.Remove(callback);
                } next();
            });
        }

        /// <summary>
        /// Subscribes this action to the 'error' event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnError (Reactor.Action<Exception> callback) {
            this.queue.Run(next => {
                this.onerror.On(callback);
                next();
            });
        }

        /// <summary>
        /// Unsubscribes this action from the 'error' event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveError (Reactor.Action<Exception> callback) {
            this.queue.Run(next => {
                this.onerror.Remove(callback);
                next();
            });
        }

        /// <summary>
        /// Subscribes this action to the 'end' event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnEnd (Reactor.Action callback) {
            this.queue.Run(next => {
                this.onend.On(callback);
                next();
            });
        }

        /// <summary>
        /// Unsubscribes this action from the 'end' event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveEnd (Reactor.Action callback) {
            this.queue.Run(next => {
                this.onend.Remove(callback);
                next();
            });
        }

        #endregion

        #region Methods

        /// <summary>
        /// Will read this number of bytes out of the internal buffer. If there 
        /// is no data available, then it will return a zero length buffer. If 
        /// the internal buffer has been completely read, then this method will 
        /// issue a new read request on the underlying resource in non-flowing 
        /// mode. Any data read with a length > 0 will also be emitted as a 'read' 
        /// event.
        /// </summary>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns></returns>
        public Reactor.Buffer Read (int count) {
            if(this.readmode == ReadMode.Unknown ||
               this.readmode == ReadMode.NonFlowing) {
                this.readmode = ReadMode.NonFlowing;
                var data = this.buffer.Read(count);
                if (this.buffer.Length == 0) {
                    if (this.readstate == ReadState.Pending) {
                        this.readstate = ReadState.Reading;
                        this._Read();
                    }
                } return Reactor.Buffer.Create(data);
            } return null;
        }

        /// <summary>
        /// Will read all data out of the internal buffer. If no data is available 
        /// then it will return a zero length buffer. This method will then issue 
        /// a new read request on the underlying resource in non-flowing mode. Any 
        /// data read with a length > 0 will also be emitted as a 'read' event.
        /// </summary>
        public Reactor.Buffer Read () {
            return this.Read(this.buffer.Length);
        }

        /// <summary>
        /// Unshifts this buffer back to this stream.
        /// </summary>
        /// <param name="buffer">The buffer to unshift.</param>
        public void Unshift (Reactor.Buffer buffer) {
            buffer.Locked = true;
            this.buffer.Unshift(buffer);
            buffer.Dispose();
        }

        /// <summary>
        /// Writes this buffer to the stream.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="callback">A callback to signal when this buffer has been written.</param>
        public Reactor.Future Write (Reactor.Buffer buffer) {
            return new Reactor.Future((resolve, reject) => {
                this.queue.Run(next => {
                    this._Write(buffer)
                               .Then(resolve)
                               .Error(reject)
                               .Finally(next);
                });
            });
        }

        /// <summary>
        /// Flushes this stream.
        /// </summary>
        /// <param name="callback">A callback to signal when this buffer has been flushed.</param>
        public Reactor.Future Flush () {
            return null;
            //return new Reactor.Future((resolve, reject) => {
            //    this.queue.Run(next => {
            //        this.writer.Flush()
            //                   .Then(resolve)
            //                   .Error(reject)
            //                   .Finally(next);
            //    });
            //});
        }

        /// <summary>
        /// Ends this stream.
        /// </summary>
        /// <param name="callback">A callback to signal when this stream has ended.</param>
        public Reactor.Future End () {
            return new Reactor.Future((resolve, reject) => {
                this.queue.Run(next => {
                    this._End()
                             .Then(resolve)
                             .Error(reject)
                             .Finally(next);
                });
            });
        }

        /// <summary>
        /// Forces buffering of all writes. Buffered data will be 
        /// flushed either at .Uncork() or at .End() call.
        /// </summary>
        public void Cork() {
            //this.queue.Run(next => {
            //    this.corked = true;
            //    if(this.writer != null)
            //        this.writer.Cork();
            //    next();
            //});
        }

        /// <summary>
        /// Flush all data, buffered since .Cork() call.
        /// </summary>
        public void Uncork() {
            //this.queue.Run(next => {
            //    this.corked = false;
            //    if(this.writer != null)
            //        this.writer.Uncork();
            //    next();
            //});
        }

        /// <summary>
        /// Pauses this stream. This method will cause a 
        /// stream in flowing mode to stop emitting data events, 
        /// switching out of flowing mode. Any data that becomes 
        /// available will remain in the internal buffer.
        /// </summary>
        public void Pause() {
            if(this.readmode == ReadMode.Unknown ||
                this.readmode == ReadMode.Flowing) {
                this.readmode  = ReadMode.Flowing;
                this.readstate = ReadState.Paused;
            }           
        }

        /// <summary>
        /// This method will cause the readable stream to resume emitting data events.
        /// This method will switch the stream into flowing mode. If you do not want 
        /// to consume the data from a stream, but you do want to get to its end event, 
        /// you can call readable.resume() to open the flow of data.
        /// </summary>
        public void Resume() {
            this.queue.Run(next => {
                if(this.readmode == ReadMode.Unknown ||
                   this.readmode == ReadMode.Flowing) {
                    this.readmode = ReadMode.Flowing;
                    if(this.readstate  == ReadState.Paused ||
                        this.readstate == ReadState.Pending) {
                        this.readstate = ReadState.Reading;
                        this._Read();
                    }
                } next();
            });
        }

        /// <summary>
        /// Pipes data to a writable stream.
        /// </summary>
        /// <param name="writable"></param>
        /// <returns></returns>
        public Reactor.IReadable Pipe (Reactor.IWritable writable) {
            this.OnRead(buffer => {
                this.Pause();
                writable.Write(buffer)
                        .Then(this.Resume)
                        .Error(this._Error);
            }); this.OnEnd (() => writable.End());
            return this;
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
            return this.Write(Reactor.Buffer.Create(buffer));
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

        #endregion

        #region IReadable Extension

        /// <summary>
        /// Reads a boolean from this stream.
        /// </summary>
        /// <returns></returns>
        public System.Boolean ReadBool () {
            var data = this.Read(sizeof(System.Boolean));
            return BitConverter.ToBoolean(data.ToArray(), 0);
        }

        /// <summary>
        /// Reads a Int16 value from this stream.
        /// </summary>
        /// <returns></returns>
        public System.Int16 ReadInt16 () {
            var data = this.Read(sizeof(System.Int16));
            return BitConverter.ToInt16(data.ToArray(), 0);
        }

        /// <summary>
        /// Reads a UInt16 value from this stream.
        /// </summary>
        /// <returns></returns>
        public System.UInt16 ReadUInt16 () {
            var data = this.Read(sizeof(System.UInt16));
            return BitConverter.ToUInt16(data.ToArray(), 0);
        }

        /// <summary>
        /// Reads a Int32 value from this stream.
        /// </summary>
        /// <returns></returns>
        public System.Int32 ReadInt32 () {
            var data = this.Read(sizeof(System.Int32));
            return BitConverter.ToInt32(data.ToArray(), 0);
        }

        /// <summary>
        /// Reads a UInt32 value from this stream.
        /// </summary>
        /// <returns></returns>
        public System.UInt32 ReadUInt32 () {
            var data = this.Read(sizeof(System.UInt32));
            return BitConverter.ToUInt32(data.ToArray(), 0);
        }

        /// <summary>
        /// Reads a Int64 value from this stream.
        /// </summary>
        /// <returns></returns>
        public System.Int64 ReadInt64 () {
            var data = this.Read(sizeof(System.Int64));
            return BitConverter.ToInt64(data.ToArray(), 0);
        }

        /// <summary>
        /// Reads a UInt64 value from this stream.
        /// </summary>
        /// <returns></returns>
        public System.UInt64 ReadUInt64 () {
            var data = this.Read(sizeof(System.UInt64));
            return BitConverter.ToUInt64(data.ToArray(), 0);
        }

        /// <summary>
        /// Reads a Single precision value from this stream.
        /// </summary>
        /// <returns></returns>
        public System.Single ReadSingle () {
            var data = this.Read(sizeof(System.Single));
            return BitConverter.ToSingle(data.ToArray(), 0);
        }

        /// <summary>
        /// Reads a Double precision value from this stream.
        /// </summary>
        /// <returns></returns>
        public System.Double ReadDouble () {
            var data = this.Read(sizeof(System.Double));
            return BitConverter.ToDouble(data.ToArray(), 0);
        }

        /// <summary>
        /// Unshifts this data to the stream.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        public void Unshift (byte[] buffer, int index, int count) {
            this.Unshift(Reactor.Buffer.Create(buffer, 0, count));
        }

        /// <summary>
        /// Unshifts this data to the stream.
        /// </summary>
        /// <param name="buffer"></param>
        public void Unshift (byte[] buffer) {
            this.Unshift(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Unshifts this data to the stream.
        /// </summary>
        /// <param name="data"></param>
        public void Unshift (char data) {
            this.Unshift(data.ToString());
        }

        /// <summary>
        /// Unshifts this data to the stream.
        /// </summary>
        /// <param name="data"></param>
        public void Unshift (string data) {
            this.Unshift(System.Text.Encoding.UTF8.GetBytes(data));
        }

        /// <summary>
        /// Unshifts this data to the stream.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void Unshift (string format, params object[] args) {
            format = string.Format(format, args);
            this.Unshift(System.Text.Encoding.UTF8.GetBytes(format));
        }

        /// <summary>
        /// Unshifts this data to the stream.
        /// </summary>
        /// <param name="data"></param>
        public void Unshift (byte data) {
            this.Unshift(new byte[1] { data });
        }

        /// <summary>
        /// Unshifts a System.Boolean value to the stream.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (bool value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts a System.Int16 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (short value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts a System.UInt16 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (ushort value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts a System.Int32 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (int value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts a System.UInt32 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (uint value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts a System.Int64 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (long value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts a System.UInt64 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (ulong value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts a System.Single value to the stream.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (float value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts a System.Double value to the stream.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (double value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        #endregion

        #region Internal

        private Reactor.Future _Write(Reactor.Buffer buffer) {
            var deferred = new Reactor.Deferred();
            var list = new List<Future<Packet>>();
            while (buffer.Length > 0) {
                var data = buffer.Read(1000);
                list.Add(this.sender.Data(data));
            }
            Future.All<Packet>(list).Then(_ => {
                deferred.Resolve();
            }).Error(deferred.Reject);
            return deferred.Future;
        }

        /// <summary>
        /// Handles OnDrain events.
        /// </summary>
        private void _Drain () {
            this.ondrain.Emit();
        }

        /// <summary>
        /// Begins reading from the underlying stream.
        /// </summary>
        private void _Read  () {
            if (this.buffer.Length > 0) {
                var clone = this.buffer.Clone();
                this.buffer.Clear();
                this.onread.Emit(clone);
            }
            Loop.Post(() => {
                if(this.readstate == ReadState.Reading)
                    _Read();
            });
        }

        /// <summary>
        /// Handles stream errors.
        /// </summary>
        /// <param name="error"></param>
        private void _Error (Exception error) {
            if (this.readstate != ReadState.Ended) { 
                this.onerror.Emit(error);
                this._End();
            }
        }

        /// <summary>
        /// Terminates the stream.
        /// </summary>
        private Reactor.Future _End   () {
            var deferred = new Reactor.Deferred();
            this.sender.Fin().Then(_ => {
                deferred.Resolve();
                this.readstate = ReadState.Ended;
            }).Error(deferred.Reject);
            return deferred.Future;
        }

        #endregion

        private void OnData(byte[] data) {
            this.buffer.Write(data);
        }

        private void OnFin() {
            this.readstate = ReadState.Ended;
            this.sender.Fin().Then(_ => {
                this.onend.Emit();
            });
        }

        #region Statics

        /// <summary>
        /// Creates a new socket.
        /// </summary>
        /// <param name="local">The local endpoint to bind this socket to.</param>
        /// <param name="remote">The remote endpoint of this socket.</param>
        /// <returns></returns>
        public static Socket Create (IPEndPoint local, IPEndPoint remote) {
            return new Socket(local, remote);
        }

        /// <summary>
        /// Creates a new socket.
        /// </summary>
        /// <param name="remote">The remote endpoint of this socket.</param>
        /// <returns></returns>
        public static Socket Create (IPEndPoint remote) {
            return new Socket(new System.Net.IPEndPoint(IPAddress.Any, 0), remote);
        }

        /// <summary>
        /// Creates a new socket. Connects to localhost on given port.
        /// </summary>
        /// <param name="port">The port to connect to on localhost.</param>
        /// <returns></returns>
        public static Socket Create (int port) {
            return new Socket(new IPEndPoint(IPAddress.Any, 0), new IPEndPoint(IPAddress.Loopback, port));         
        }

        #endregion
    }
}
