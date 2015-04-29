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

namespace Reactor.Streams {
    
    /// <summary>
    /// Provides a asynchronous write interface over System.IO.Stream.
    /// </summary>
    internal class Writer : IDisposable {
        private System.IO.Stream                 stream;
        private Reactor.Async.Queue              queue;
        private Reactor.Async.Event              ondrain;
        private Reactor.Async.Event<Exception>   onerror;
        private Reactor.Async.Event              onend;

        #region Constructors

        /// <summary>
        /// Creates a new Writer.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        public Writer(System.IO.Stream stream) {
            this.stream  = stream;
            this.queue   = Reactor.Async.Queue.Create(1);
            this.ondrain = Reactor.Async.Event.Create();
            this.onerror = Reactor.Async.Event.Create<Exception>();
            this.onend   = Reactor.Async.Event.Create();
        }

        #endregion

        #region Events

        /// <summary>
        /// Subscribes to the OnDrain event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnDrain(Reactor.Action callback) {
            this.ondrain.On(callback);
        }

        /// <summary>
        /// Unsubscribes from the OnDrain event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveDrain(Reactor.Action callback) {
            this.ondrain.Remove(callback);
        }

        /// <summary>
        /// Subscribes this action to OnError events.
        /// </summary>
        /// <param name="callback"></param>
        public void OnError (Reactor.Action<Exception> callback) {
            this.onerror.On(callback);
        }

        /// <summary>
        /// Unsubscribes this action from OnError events.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveError (Reactor.Action<Exception> callback) {
            this.onerror.Remove(callback);
        }

        /// <summary>
        /// Subscribes this action to OnEnd events.
        /// </summary>
        /// <param name="callback"></param>
        public void OnEnd (Reactor.Action callback) {
            this.onend.On(callback);
        }

        /// <summary>
        /// Unsubscribes this action from OnEnd events.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveEnd (Reactor.Action callback) {
            this.onend.Remove(callback);
        }

        #endregion

        #region Methods
        
        /// <summary>
        /// Writes this buffer to this stream.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="callback">A action called once the write has completed.</param>
        public Reactor.Async.Future Write (Reactor.Buffer buffer) {
            var clone = buffer.ToArray();
            return new Reactor.Async.Future((resolve, reject) => {
                this.queue.Run(next => {
                    try {
                        this.stream.BeginWrite(clone, 0, clone.Length, result => {
                            Loop.Post(() => {
                                try {
                                    this.stream.EndWrite(result);
                                    this.ondrain.Emit();
                                    resolve();
                                    next();
                                }
                                catch (Exception error) {
                                    this.onerror.Emit(error);
                                    this.onend.Emit();
                                    this.Dispose();
                                    reject(error);
                                    next();
                                }
                            });
                        }, null);
                    }
                    catch(Exception error) {
                        this.onerror.Emit(error);
                        this.onend.Emit();
                        this.Dispose();
                        reject(error);
                        next();
                    }
                });
            });
        }

        /// <summary>
        /// Flush data in this stream.
        /// </summary>
        /// <param name="callback">A action called once the stream has been flushed.</param>
        public Reactor.Async.Future Flush () {
            return new Reactor.Async.Future((resolve, reject) => {
                this.queue.Run(next => {
                    try {
                        this.stream.Flush();
                        resolve();
                        next();
                    }
                    catch (Exception error) {
                        this.onerror.Emit(error);
                        this.onend.Emit();
                        this.Dispose();
                        reject(error);
                        next();
                    }   
                });
            });
        }

        /// <summary>
        /// Ends this stream. Followed by disposing of this stream.
        /// </summary>
        /// <param name="callback">A action called once the stream has been ended.</param>
        public Reactor.Async.Future End () {
            this.Uncork();
            return new Reactor.Async.Future((resolve, reject) => {  
                this.queue.Run(next => {
                    try {
                        this.stream.Dispose();
                        this.onend.Emit();
                        this.Dispose();
                        resolve();
                        next();
                    }
                    catch (Exception error) {
                        this.onerror.Emit(error);
                        this.onend.Emit();
                        this.Dispose();
                        reject(error);
                        next();
                    }
                });
            });
        }

        /// <summary>
        /// Forces buffering of all writes. Buffered data will be 
        /// flushed either at .Uncork() or at .End() call.
        /// </summary>
        public void Cork() {
            this.queue.Pause();
        }

        /// <summary>
        /// Flush all data, buffered since .Cork() call.
        /// </summary>
        public void Uncork() {
            this.queue.Resume();
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Disposes of this writer.
        /// </summary>
        public void Dispose() {
            this.queue.Dispose();
            this.stream.Dispose();
        }

        #endregion

        #region Statics

        /// <summary>
        /// Creates a new Writer.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="buffersize">The size of this writers internal buffer.</param>
        /// <returns></returns>
        public static Writer Create(System.IO.Stream stream, int buffersize) {
            return new Writer(stream);
        }

        /// <summary>
        /// Creates a new Writer.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <returns></returns>
        public static Writer Create(System.IO.Stream stream) {
            return new Writer(stream);
        }

        #endregion
    }
}
