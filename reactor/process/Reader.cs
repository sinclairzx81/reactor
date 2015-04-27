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

using Reactor.Async;
using System;

namespace Reactor.Process
{
    /// <summary>
    /// Reactor file reader.
    /// </summary>
    public class Reader : Reactor.IReadable, IDisposable {

        #region States

        /// <summary>
        /// Readable state.
        /// </summary>
        internal enum State {
            /// <summary>
            /// A state indicating a paused state.
            /// </summary>
            Paused,
            /// <summary>
            /// A state indicating a reading state.
            /// </summary>
            Reading,
            /// <summary>
            /// A state indicating a resume state.
            /// </summary>
            Resumed,
            /// <summary>
            /// A state indicating a ended state.
            /// </summary>
            Ended
        }

        /// <summary>
        /// Readable mode.
        /// </summary>
        internal enum Mode {
            /// <summary>
            /// Flowing read semantics.
            /// </summary>
            Flowing,
            /// <summary>
            /// Non flowing read semantics.
            /// </summary>
            NonFlowing
        }

        #endregion

        private Reactor.Async.Event                   onreadable;
        private Reactor.Async.Event<Reactor.Buffer>   onread;
        private Reactor.Async.Event<Exception>        onerror;
        private Reactor.Async.Event                   onend;
        private Reactor.Streams.Reader                reader;
        private Reactor.Buffer                        buffer;
        private State                                 state;
        private Mode                                  mode;

        #region Constructors

        /// <summary>
        /// Creates a new file reader.
        /// </summary>
        /// <param name="filename">The file to read.</param>
        /// <param name="offset">The offset to begin reading.</param>
        /// <param name="count">The number of bytes to read.</param>
        /// <param name="mode">The file mode.</param>
        /// <param name="share">The file share.</param>
        internal Reader (System.IO.Stream stream) {
            this.onreadable = Reactor.Async.Event.Create();
            this.onread     = Reactor.Async.Event.Create<Reactor.Buffer>();
            this.onerror    = Reactor.Async.Event.Create<Exception>();
            this.onend      = Reactor.Async.Event.Create();
            this.buffer     = Reactor.Buffer.Create();
            this.state      = State.Paused;
            this.mode       = Mode.NonFlowing;
            this.reader     = Reactor.Streams.Reader.Create(stream, Reactor.Settings.DefaultBufferSize);
            this.reader.OnRead  (this._Data);
            this.reader.OnError (this._Error);
            this.reader.OnEnd   (this._End);
        }

        #endregion

        #region Events

        /// <summary>
        /// Subscribes this action to the OnReadable event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnReadable (Reactor.Action callback) {
            this.mode = Mode.NonFlowing;
            this.onreadable.On(callback);
            this.Resume();
        }

        /// <summary>
        /// Unsubscribes this action from the OnReadable event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveReadable (Reactor.Action callback) {
            this.onreadable.Remove(callback);
        }

        /// <summary>
        /// Subscribes this action to the OnRead event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnRead (Reactor.Action<Reactor.Buffer> callback) {
            this.mode = Mode.Flowing;
            this.onread.On(callback);
            this.Resume();
        }

        /// <summary>
        /// Unsubscribes this action from the OnRead event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveRead (Reactor.Action<Reactor.Buffer> callback) {
            this.onread.Remove(callback);
        }

        /// <summary>
        /// Subscribes this action to the OnError event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnError (Reactor.Action<Exception> callback) {
            this.onerror.On(callback);
        }

        /// <summary>
        /// Unsubscribes this action from the OnError event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveError (Reactor.Action<Exception> callback) {
            this.onerror.Remove(callback);
        }

        /// <summary>
        /// Subscribes this action to the OnEnd event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnEnd (Reactor.Action callback) {
            this.onend.On(callback);
        }

        /// <summary>
        /// Unsubscribes this action from the OnEnd event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveEnd (Reactor.Action callback) {
            this.onend.Remove(callback);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Reads bytes from this streams buffer.
        /// </summary>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns></returns>
        public Reactor.Buffer Read(int count) {
            var subset = this.buffer.Read(count);
            if (this.buffer.Length == 0) {
                this.Resume();
            }
            return Reactor.Buffer.Create(subset);
        }

        /// <summary>
        /// Reads bytes from this streams buffer.
        /// </summary>
        /// <returns></returns>
        public Reactor.Buffer Read() {
            return this.Read(this.buffer.Length);
        }

        /// <summary>
        /// Unshifts this buffer back to this stream.
        /// </summary>
        /// <param name="buffer">The buffer to unshift.</param>
        public void Unshift(Reactor.Buffer buffer) {
            this.buffer.Unshift(buffer);
        }

        /// <summary>
        /// Pauses this stream.
        /// </summary>
        public void Pause() {
            this.state = State.Paused;
        }

        /// <summary>
        /// Resumes this stream.
        /// </summary>
        public void Resume() {
            this.state = State.Resumed;
            this._Read();
        }

        /// <summary>
        /// Pipes data to a writable stream.
        /// </summary>
        /// <param name="writable"></param>
        /// <returns></returns>
        public Reactor.IReadable Pipe (Reactor.IWritable writable) {
            this.OnRead(data => {
                this.Pause();
                writable.Write(data)
                        .Then(this.Resume)
                        .Error(this._Error);
            });
            this.OnEnd (() => writable.End());
            return this;
        }

        #endregion

        #region Buffer

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
            this.Unshift(Reactor.Buffer.Create(buffer));
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

        #region State Machine

        /// <summary>
        /// Reads from the internal stream if we are
        /// in a resume state.
        /// </summary>
        private void _Read () {
            if (this.state == State.Resumed) {
                this.state = State.Reading;
                this.reader.Read();
            }
        }

        /// <summary>
        /// Handles incoming data from the stream.
        /// </summary>
        /// <param name="buffer"></param>
        private void _Data (Reactor.Buffer buffer) {
            this.state = State.Resumed;
            switch (this.mode) {
                case Mode.Flowing:
                    this.onread.Emit(buffer);
                    this.buffer.Clear();
                    this._Read();
                    break;
                case Mode.NonFlowing:
                    this.buffer.Write(buffer);
                    this.onreadable.Emit();
                    break;
            } 
        }

        /// <summary>
        /// Handles stream errors.
        /// </summary>
        /// <param name="error"></param>
        private void _Error (Exception error) {
            if (this.state != State.Ended) {
                this.onerror.Emit(error);
                this._End();
            }
        }

        /// <summary>
        /// Terminates the stream.
        /// </summary>
        private void _End () {
            if (this.state != State.Ended) {
                this.state = State.Ended;
                this.reader.Dispose();
                this.onend.Emit();
            }
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Disposes of this stream.
        /// </summary>
        public void Dispose() {
            this._End();
        }

        #endregion
        
        #region Statics

        /// <summary>
        /// Creates a new process reader.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="mode"></param>
        /// <param name="share"></param>
        /// <returns></returns>
        internal static Reader Create(System.IO.Stream stream) {
            return new Reader(stream);
        }

        #endregion
    }
}
