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
using System.IO;

namespace Reactor.File {

    /// <summary>
    /// Reactor file writer.
    /// </summary>
    public class Writer : Reactor.IWritable, IDisposable {

        #region State

        /// <summary>
        /// Internal state of this writer.
        /// </summary>
        internal enum State {
            /// <summary>
            /// Indicates that this stream is still writing.
            /// </summary>
            Writing, 

            /// <summary>
            /// Indicates that this stream has ended.
            /// </summary>
            Ended
        }

        #endregion

        private Reactor.Async.Event            ondrain;
        private Reactor.Async.Event<Exception> onerror;
        private Reactor.Async.Event            onend;
        private Reactor.Streams.Writer         writer;
        private State                          state;
        
        #region Constructor

        /// <summary>
        /// Creates a new file writer.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="offset"></param>
        /// <param name="mode"></param>
        /// <param name="share"></param>
        public Writer(string filename, long offset, System.IO.FileMode mode, System.IO.FileShare share) {
            this.ondrain = Reactor.Async.Event.Create();
            this.onerror = Reactor.Async.Event.Create<Exception>();
            this.onend   = Reactor.Async.Event.Create();
            this.state   = State.Writing;
            var stream   = System.IO.File.Open(filename, mode, FileAccess.Write, share);
            offset       = (offset > stream.Length) ? stream.Length : offset;
            stream.Seek(offset, SeekOrigin.Begin);
            this.writer  = Reactor.Streams.Writer.Create(stream);
            this.writer.OnDrain (this._Drain);
            this.writer.OnError (this._Error);
            this.writer.OnEnd   (this._End);
        }

        #endregion

        #region Events

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
        /// Unsubscribes to from the OnDrain event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveDrain (Reactor.Action callback) {
            this.ondrain.Remove(callback);
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
        public void RemoveError(Reactor.Action<Exception> callback) {
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
        public void RemoveEnd(Reactor.Action callback) {
            this.onend.Remove(callback);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Writes this buffer to the stream.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="callback">A callback to signal when this data has been written.</param>
        public Reactor.Async.Future Write (Reactor.Buffer buffer) {
            buffer.Locked = true;
            return this.writer.Write(buffer);
        }

        /// <summary>
        /// Flushes this stream.
        /// </summary>
        /// <param name="callback"></param>
        public Reactor.Async.Future Flush () {
            return this.writer.Flush();
        }

        /// <summary>
        /// Ends the stream.
        /// </summary>
        /// <param name="callback">A callback to signal when this stream has ended.</param>
        public Reactor.Async.Future End () {
            return this.writer.End();
        }

        /// <summary>
        /// Forces buffering of all writes. Buffered data will be 
        /// flushed either at .Uncork() or at .End() call.
        /// </summary>
        public void Cork() {
            this.writer.Cork();
        }

        /// <summary>
        /// Flush all data, buffered since .Cork() call.
        /// </summary>
        public void Uncork() {
             this.writer.Uncork();
        }

        #endregion

        #region Buffer

        /// <summary>
        /// Writes this data to the stream.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (byte[] buffer, int index, int count) {
            return this.Write(Reactor.Buffer.Create(buffer, 0, count));
        }

        /// <summary>
        /// Writes this data to the stream.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (byte[] buffer) {
            return this.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Writes this data to the stream.
        /// </summary>
        /// <param name="data"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (string data) {
            return this.Write(System.Text.Encoding.UTF8.GetBytes(data));
        }

        /// <summary>
        /// Writes this data to the stream.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (string format, params object[] args) {
            format = string.Format(format, args);
            return this.Write(System.Text.Encoding.UTF8.GetBytes(format));
        }

        /// <summary>
        /// Writes this data to the stream.
        /// </summary>
        /// <param name="data"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (byte data) {
            return this.Write(new byte[1] { data });
        }

        /// <summary>
        /// Writes a System.Boolean value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (bool value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.Int16 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (short value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.UInt16 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (ushort value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.Int32 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (int value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.UInt32 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (uint value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.Int64 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (long value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.UInt64 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (ulong value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.Single value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (float value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.Double value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (double value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        #endregion

        #region Machine

        /// <summary>
        /// Emits the ondrain event.
        /// </summary>
        private void _Drain() {
            if (this.state != State.Ended) {
                this.ondrain.Emit();
            }
        }

        /// <summary>
        /// Emits the _Error event.
        /// </summary>
        /// <param name="error"></param>
        private void _Error(Exception error) {
            if (this.state != State.Ended) {
                this.onerror.Emit(error);
                this._End();
            }
        }

        /// <summary>
        /// Emits the 'end' event and disposes.
        /// </summary>
        private void _End() {
            if (this.state != State.Ended) {
                this.state = State.Ended;
                this.writer.Dispose();
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

        ~Writer() {
            Loop.Post(() => { this._End(); });
        }

        #endregion

        #region Statics

        /// <summary>
        /// Creates a new file writer.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="index"></param>
        /// <param name="mode"></param>
        /// <param name="share"></param>
        /// <returns></returns>
        public static Writer Create(string filename, long index, FileMode mode, FileShare share) {
            return new Writer(filename, index, mode, share);
        }

        /// <summary>
        /// Creates a new file writer.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="index"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static Writer Create(string filename, long index, FileMode mode) {
            return new Writer(filename, index, mode, FileShare.Write);
        }

        /// <summary>
        /// Creates a new file writer.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Writer Create(string filename, long index) {
            return new Writer(filename, index, FileMode.OpenOrCreate, FileShare.Write);
        }

        /// <summary>
        /// Creates a new file writer.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="mode"></param>
        /// <param name="share"></param>
        /// <returns></returns>
        public static Writer Create(string filename, FileMode mode, FileShare share) {
            return new Writer(filename, 0, mode, share);
        }

        /// <summary>
        /// Creates a new file writer.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static Writer Create(string filename, FileMode mode) {
            return new Writer(filename, 0, mode, FileShare.Write);
        }

        /// <summary>
        /// Creates a new file writer.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static Writer Create(string filename) {
            return new Writer(filename, 0, FileMode.OpenOrCreate, FileShare.Write);
        }

        #endregion
    }
}
