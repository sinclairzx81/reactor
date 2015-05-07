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
using System.Text;

namespace Reactor.Http.Protocol {

    /// <summary>
    /// Reactor HTTP chunked body writer. Layers a reactor writable
    /// as a transfer-encoded 'chunked' stream.
    /// </summary>
    public class ChunkedBodyWriter : Reactor.IWritable {

        private Reactor.IWritable writable;

        #region Constructors

        public ChunkedBodyWriter(Reactor.IWritable writable) {
            this.writable = writable;
        }

        #endregion

        #region Events

        /// <summary>
        /// Subscribes this action to the 'drain' event. The event indicates
        /// when a write operation has completed and the caller should send
        /// more data.
        /// </summary>
        /// <param name="callback"></param>
        public void OnDrain (Reactor.Action action) {
            this.writable.OnDrain(action);
        }

        /// <summary>
        /// Subscribes this action once to the 'drain' event. The event indicates
        /// when a write operation has completed and the caller should send
        /// more data.
        /// </summary>
        /// <param name="callback"></param>
        public void OnceDrain (Reactor.Action action) {
            this.writable.OnceDrain(action);
        }

        /// <summary>
        /// Unsubscribes from the 'drain' event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveDrain (Reactor.Action action) {
            this.writable.RemoveDrain(action);
        }

        /// <summary>
        /// Subscribes to this action to the 'error' event.
        /// </summary>
        /// <param name="action"></param>
        public void OnError (Reactor.Action<Exception> action) {
            this.writable.OnError(action);
        }

        /// <summary>
        /// Unsubscribes this action from the 'error' event.
        /// </summary>
        /// <param name="action"></param>
        public void RemoveError (Reactor.Action<Exception> action) {
            this.writable.RemoveError(action);
        }

        /// <summary>
        /// Subscribes this action to the 'end' event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnEnd (Reactor.Action callback) {
            this.writable.OnEnd(callback);
        }

        /// <summary>
        /// Unsubscribes this action from the 'end' event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveEnd (Reactor.Action callback) {
            this.writable.RemoveEnd(callback);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Writes this buffer to the stream.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="callback">A callback to signal when this buffer has been written.</param>
        public Reactor.Async.Future Write (Reactor.Buffer buffer) {
            var chunk = Reactor.Buffer.Create();
            chunk.Write(String.Format("{0:x}\r\n", buffer.Length));
            chunk.Write(buffer);
            chunk.Write(Encoding.ASCII.GetBytes("\r\n"));
            return this.writable.Write(chunk);
        }

        /// <summary>
        /// Flushes this stream.
        /// </summary>
        /// <param name="callback">A callback to signal when this buffer has been flushed.</param>
        public Reactor.Async.Future Flush () {
            return this.writable.Flush();
        }

        /// <summary>
        /// Ends this stream.
        /// </summary>
        /// <param name="callback">A callback to signal when this stream has ended.</param>
        public Reactor.Async.Future End () {
            var chunk = Reactor.Buffer.Create();
            chunk.Write("0\r\n");
            chunk.Write("\r\n");
            this.writable.Write(chunk);
            return this.writable.End();
        }

        /// <summary>
        /// Forces buffering of all writes. Buffered data will be 
        /// flushed either at .Uncork() or at .End() call.
        /// </summary>
        public void Cork() {
            this.writable.Cork();
        }

        /// <summary>
        /// Flush all data, buffered since .Cork() call.
        /// </summary>
        public void Uncork() {
             this.writable.Uncork();
        }

        #endregion
    }
}
