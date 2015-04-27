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

namespace Reactor.Streams {

    /// <summary>
    /// Provides a asynchronous read interface over System.IO.Stream.
    /// </summary>
    internal class Reader : IDisposable {
        private System.IO.Stream                      stream;
        private Reactor.Async.Event<Reactor.Buffer>   onread;
        private Reactor.Async.Event<Exception>        onerror;
        private Reactor.Async.Event                   onend;
        private Reactor.Async.Queue                   queue;
        private byte[]                                read_buffer;

        #region Constructors

        /// <summary>
        /// Creates a new Reader.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="buffersize">The read buffer size in bytes.</param>
        public Reader (System.IO.Stream stream, int buffersize) {
            this.stream       = stream;
            this.queue        = Reactor.Async.Queue.Create(1);
            this.onread       = Reactor.Async.Event.Create<Reactor.Buffer>();
            this.onerror      = Reactor.Async.Event.Create<Exception>();
            this.onend        = Reactor.Async.Event.Create();
            this.read_buffer  = new byte[buffersize];
        }

        #endregion

        #region Events

        /// <summary>
        /// Subscribes this action to OnRead events.
        /// </summary>
        /// <param name="callback"></param>
        public void OnRead(Reactor.Action<Reactor.Buffer> callback) {
            this.onread.On(callback);
        }

        /// <summary>
        /// Unsubscribes this action from OnRead events.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveRead(Reactor.Action<Reactor.Buffer> callback) {
            this.onread.Remove(callback);
        }

        /// <summary>
        /// Subscribes this action to OnError events.
        /// </summary>
        /// <param name="callback"></param>
        public void OnError(Reactor.Action<Exception> callback) {
            this.onerror.On(callback);
        }

        /// <summary>
        /// Unsubscribes this action from OnError events.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveError(Reactor.Action<Exception> callback) {
            this.onerror.Remove(callback);
        }

        /// <summary>
        /// Subscribes this action to OnEnd events.
        /// </summary>
        /// <param name="callback"></param>
        public void OnEnd(Reactor.Action callback) {
            this.onend.On(callback);
        }

        /// <summary>
        /// Unsubscribes this action from OnEnd events.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveEnd(Reactor.Action callback) {
            this.onend.Remove(callback);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Reads from this stream. Data will be submitted to OnRead handlers.
        /// </summary>
        public void Read() {
            this.queue.Run(next => {
                try {
                    this.stream.BeginRead(this.read_buffer, 0, this.read_buffer.Length, result => {
                        Loop.Post(() => {
                            try {
                                int read = this.stream.EndRead(result);
                                if (read == 0) {
                                    this.onend.Emit();
                                    this.Dispose();
                                    next();
                                    return;
                                }
                                var buffer = Reactor.Buffer.Create(this.read_buffer, 0, read);
                                this.onread.Emit(buffer);
                                next();
                            }
                            catch (Exception error) {
                                this.onerror.Emit(error);
                                this.onend.Emit();
                                this.Dispose();
                                next();
                            }
                        });
                    }, null);
                }
                catch(Exception error) {
                    this.onerror.Emit(error);
                    this.onend.Emit();
                    this.Dispose();
                    next();
                }
            });
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Disposes of this reader.
        /// </summary>
        public void Dispose() {
            this.queue.Dispose();
            this.stream.Dispose();
            this.read_buffer = null;
        }

        #endregion

        #region Statics

        /// <summary>
        /// Creates a new Reader.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="buffersize">The read buffer size in bytes.</param>
        /// <returns></returns>
        public static Reader Create(System.IO.Stream stream, int buffersize) {
            return new Reader(stream, buffersize);
        }

        /// <summary>
        /// Creates a new Reader.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <returns></returns>
        public static Reader Create(System.IO.Stream stream) {
            return new Reader(stream, Reactor.Settings.DefaultBufferSize);
        }

        #endregion
    }
}
