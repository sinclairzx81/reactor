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

namespace Reactor.Http.Protocol {

    /// <summary>
    /// Reactor HTTP Incoming Message
    /// </summary>
    public class BodyReader : Reactor.IReadable {

        #region States

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
        internal enum Mode {
            /// <summary>
            /// This stream is using flowing semantics.
            /// </summary>
            Flowing,
            /// <summary>
            /// This stream is using non-flowing semantics.
            /// </summary>
            NonFlowing
        }

        #endregion

        private Reactor.IReadable                   readable;
        private Reactor.Async.Event                 onreadable;
        private Reactor.Async.Event<Reactor.Buffer> onread;
        private Reactor.Async.Event<Exception>      onerror;
        private Reactor.Async.Event                 onend;
        private Reactor.Buffer                      buffer;
        private State                               state;
        private Mode                                mode;
        private System.Int64                        contentLength;
        private System.Int64                        received;

        #region Constructors

        /// <summary>
        /// Creates a HTTP body reader. 
        /// </summary>
        /// <param name="socket"></param>
        internal BodyReader(Reactor.IReadable readable, 
                            System.Int64      contentLength) {
            this.readable        = readable;
            this.onreadable      = Reactor.Async.Event.Create();
            this.onread          = Reactor.Async.Event.Create<Reactor.Buffer>();
            this.onerror         = Reactor.Async.Event.Create<Exception>();
            this.onend           = Reactor.Async.Event.Create();
            this.buffer          = Reactor.Buffer.Create();
            this.state           = State.Pending;
            this.mode            = Mode.NonFlowing;
            this.contentLength   = contentLength;
            this.received        = 0;
        }

        #endregion

        #region Events

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
            this.onreadable.On(callback);
            this.mode = Mode.NonFlowing;
            if (this.state == State.Paused) {
                this.state = State.Pending;
            }
            this._Read(); 
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
            this.onreadable.Once(callback);
            this.mode = Mode.NonFlowing;
            if (this.state == State.Paused) {
                this.state = State.Pending;
            }
            this._Read();
        }

        /// <summary>
        /// Unsubscribes this action from the 'readable' event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveReadable(Reactor.Action callback) {
			this.onreadable.Remove(callback);
        }

        /// <summary>
        /// Subscribes this action to the 'read' event. Attaching a data event 
        /// listener to a stream that has not been explicitly paused will 
        /// switch the stream into flowing mode and begin reading immediately. 
        /// Data will then be passed as soon as it is available.
        /// </summary>
        /// <param name="callback"></param>
        public void OnRead (Reactor.Action<Reactor.Buffer> callback) {
            this.onread.On(callback);
            if (this.state == State.Pending) {
                this.Resume();
            }
        }

        /// <summary>
        /// Subscribes this action once to the 'read' event. Attaching a data event 
        /// listener to a stream that has not been explicitly paused will 
        /// switch the stream into flowing mode and begin reading immediately. 
        /// Data will then be passed as soon as it is available.
        /// </summary>
        /// <param name="callback"></param>
        public void OnceRead(Reactor.Action<Reactor.Buffer> callback) {
            this.onread.Once(callback);
            if (this.state == State.Pending) {
                this.Resume();
            }
        }

        /// <summary>
        /// Unsubscribes this action from the 'read' event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveRead (Reactor.Action<Reactor.Buffer> callback) {
            this.onread.Remove(callback);
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
            var result = Reactor.Buffer.Create(this.buffer.Read(count));
            if (result.Length > 0) {
                this.onread.Emit(result);
            }
            if (this.buffer.Length == 0) {
                this.mode = Mode.NonFlowing;
                this._Read();
            }
            return result;
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
            this.buffer.Unshift(buffer);
        }

        /// <summary>
        /// Pauses this stream. This method will cause a 
        /// stream in flowing mode to stop emitting data events, 
        /// switching out of flowing mode. Any data that becomes 
        /// available will remain in the internal buffer.
        /// </summary>
        public void Pause() {
            this.mode  = Mode.NonFlowing;
            this.state = State.Paused;
        }

        /// <summary>
        /// This method will cause the readable stream to resume emitting data events.
        /// This method will switch the stream into flowing mode. If you do not want 
        /// to consume the data from a stream, but you do want to get to its end event, 
        /// you can call readable.resume() to open the flow of data.
        /// </summary>
        public void Resume() {
            this.mode  = Mode.Flowing;
            this.state = State.Pending;
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

        #region Machine

        /// <summary>
        /// Begins reading from the underlying stream.
        /// </summary>
        private void _Read () {
            if (this.state == State.Pending) {
                this.state = State.Reading;
                /* any data resident in the buffer
                 * needs to emitted prior to issuing
                 * a request for more, normal operation
                 * would assume that the callers only 
                 * need to read if they have emptied 
                 * the buffer, however, this rule is
                 * broken in instances where the user
                 * may have unshifted data inbetween
                 * reads. The following overrides the
                 * default behaviour and calls to 
                 * _data() directly with a cloned
                 * buffer.
                 */
                if (this.buffer.Length > 0) {
                    var clone = this.buffer.Clone();
                    this.buffer.Clear();
                    this._Data(clone);
                }
                /* here, we handle a special case for
                 * http body readers. In this scenerio, we
                 * detect if we have received data equal
                 * to or greater than the content-length. 
                 * This case is generally true for GET requests
                 * where there is no content to receive, but
                 * the caller still expects a "end" event
                 * when attempting to read from this stream. 
                 * Also note that this condition is only 
                 * going to fire if the previous condition
                 * isn't true. The intent is to ensure the
                 * caller has received 'all' data prior
                 * to ending. The end is pushed on the 
                 * event loop to allow the caller to
                 * attach appropriate event handlers.
                 */
                else if (this.received >= this.contentLength) {
                    Loop.Post(this._End);
                }
                /* here, we make a actual request on the
                 * underlying socket. This is a conceptually
                 * similar approach taken by other readable
                 * streams.
                 */
                else {
                    this.readable.OnceRead(data => {
                        this.readable.Pause();
                        this._Data(data);
                    }); this.readable.Resume();
                }
            }
        }

        /// <summary>
        /// Handles incoming data from the stream.
        /// </summary>
        /// <param name="buffer"></param>
        private void _Data (Reactor.Buffer buffer) {
            if (this.state == State.Reading) {
                this.state = State.Pending;

                bool ended = false;
                var length = buffer.Length;
                this.received = this.received + length;
                if (this.received >= this.contentLength) {
                    var overflow = this.received - this.contentLength;
                    length = length - (int)overflow;
                    //-----------------------------------
                    // questionable. the original
                    // implementation called for a
                    // buffer = buffer.Slice(0, length);
                    //-----------------------------------
                    var truncated = Reactor.Buffer.Create();
                    truncated.Write(buffer.ToArray(), 0, length);
                    buffer.Dispose();
                    buffer = truncated;
                    ended  = true;
                }

                this.buffer.Write(buffer);
                buffer.Dispose();
                switch (this.mode) {
                    case Mode.Flowing:
                        var clone = this.buffer.Clone();
                        this.buffer.Clear();
                        this.onread.Emit(clone);
                        if(ended)
                            this._End();
                        else
                            this._Read();
                        break;
                    case Mode.NonFlowing:
                        this.onreadable.Emit();
                        if(ended)
                            this._End();
                        break;
                }
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
        public void _End    () {
            if (this.state != State.Ended) {
                this.state = State.Ended;
                this.onend.Emit();
            }
        }

        #endregion
    }
}
