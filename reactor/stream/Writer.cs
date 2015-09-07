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
    public class Writer : IDisposable {

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

        private System.IO.Stream           stream;
        private Reactor.Queue              queue;
        private Reactor.Event              ondrain;
        private Reactor.Event<Exception>   onerror;
        private Reactor.Event              onend;
        private State                      state;

        #region Constructors

        /// <summary>
        /// Creates a new Writer.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        public Writer(System.IO.Stream stream) {
            this.stream  = stream;
            this.queue   = Reactor.Queue.Create(1);
            this.ondrain = Reactor.Event.Create();
            this.onerror = Reactor.Event.Create<Exception>();
            this.onend   = Reactor.Event.Create();
            this.state   = State.Writing;
        }

        #endregion

        #region Events

        /// <summary>
        /// Subscribes this action to the 'drain' event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnDrain(Reactor.Action callback) {
            this.ondrain.On(callback);
        }

        /// <summary>
        /// Subscribes this action once to the 'drain' event.
        /// </summary>
        /// <param name="action"></param>
        public void OnceDrain(Reactor.Action action) {
            this.ondrain.Once(action);
        }

        /// <summary>
        /// Unsubscribes this action from the 'drain' event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveDrain(Reactor.Action callback) {
            this.ondrain.Remove(callback);
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
        /// Writes this buffer to this stream. Once the data has been written, 
        /// this buffer is disposed of.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="callback">A action called once the write has completed.</param>
        public Reactor.Future Write (Reactor.Buffer buffer) {
            buffer.Locked = true;
            return new Reactor.Future((resolve, reject) => {
                Loop.Post(() => {
                    this.queue.Run(next => {
                        try {
                            var data = buffer.ToArray();
                            buffer.Dispose();
                            this.stream.BeginWrite(data, 0, data.Length, result => {
                                Loop.Post(() => {
                                    try {
                                        this.stream.EndWrite(result);
                                        this._Drain();
                                        resolve();
                                        next();
                                    }
                                    catch (Exception error) {
                                        this._Error(error);
                                        reject(error);
                                        next();
                                    }
                                });
                            }, null);
                        }
                        catch(Exception error) {
                            this._Error(error);
                            reject(error);
                            next();
                        }
                    });
                });
            });
        }

        /// <summary>
        /// Flush data in this stream.
        /// </summary>
        /// <param name="callback">A action called once the stream has been flushed.</param>
        public Reactor.Future Flush () {
            return new Reactor.Future((resolve, reject) => {
                Loop.Post(() => {
                    this.queue.Run(next => {
                        try {
                            this.stream.Flush();
                            resolve();
                            next();
                        }
                        catch (Exception error) {
                            this._Error(error);
                            reject(error);
                            next();
                        }   
                    });
                });
            });
        }

        /// <summary>
        /// Ends this stream. Followed by disposing of this stream.
        /// </summary>
        /// <param name="callback">A action called once the stream has been ended.</param>
        public Reactor.Future End () {
            return new Reactor.Future((resolve, reject) => { 
                Loop.Post(() => {
                    this.queue.Run(next => {
                        this._End();
                        resolve();
                        next();
                    });
                });
            });
        }

        /// <summary>
        /// Immediately forces buffering of all writes. Buffered data will be 
        /// flushed either at .Uncork() or at .End() call.
        /// </summary>
        public void Cork() {
            this.queue.Pause();
        }

        /// <summary>
        /// Resumes writing on this stream.
        /// </summary>
        public void Uncork() {
            this.queue.Resume();
        }

        #endregion

        #region Machine

        /// <summary>
        /// Emits the drain event.
        /// </summary>
        private void _Drain() {
            if (this.state != State.Ended) {
                this.ondrain.Emit();
            }
        }

        /// <summary>
        /// Emits error on this stream, then ends.
        /// </summary>
        /// <param name="error"></param>
        private void _Error(Exception error) {
            if (this.state != State.Ended) {
                this.onerror.Emit(error);
                this._End();
            }
        }

        /// <summary>
        /// Ends and disposes of this stream.
        /// </summary>
        private void _End() {
            if (this.state != State.Ended) {
                this.state = State.Ended;
                this.stream.Dispose();
                this.onend.Emit();  
            }
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Disposes of this writer.
        /// </summary>
        public void Dispose() {
            this.End();
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