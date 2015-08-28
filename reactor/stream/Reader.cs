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
    public class Reader2 : IDisposable {
        private System.IO.Stream                      stream;
        private Reactor.Async.Queue                   queue;
        private byte[]                                buffer;

        #region Constructors

        /// <summary>
        /// Creates a new Reader.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="buffersize">The read buffer size in bytes.</param>
        public Reader2 (System.IO.Stream stream, int buffersize) {
            this.stream  = stream;
            this.queue   = Reactor.Async.Queue.Create(1);
            this.buffer  = new byte[buffersize];
        }

        #endregion

        #region Methods

        /// <summary>
        /// Reads up to this many bytes from this stream. A null buffer signals end of stream.
        /// </summary>
        public Reactor.Async.Future<Reactor.Buffer> Read(int count) {
            count = (count > this.buffer.Length) 
                ? this.buffer.Length : count;
            return new Reactor.Async.Future<Reactor.Buffer>((resolve, reject) => {
                this.queue.Run(next => {
                    try {
                        this.stream.BeginRead(this.buffer, 0, count, result => {
                            Loop.Post(() => {
                                try {
                                    int read = this.stream.EndRead(result);
                                    if (read == 0) {
                                        resolve(null);
                                        this.Dispose();
                                        next();
                                        return;
                                    }
                                    resolve(Reactor.Buffer.Create(this.buffer, 0, read));
                                    next();
                                }
                                catch (Exception error) {
                                    reject(error);
                                    this.Dispose();
                                    next();
                                }
                            });
                        }, null);
                    }
                    catch(Exception error) {
                        reject(error);
                        this.Dispose();
                        next();
                    }
                });
            });
        }

        /// <summary>
        /// Reads from this stream. A null buffer signals end of stream.
        /// </summary>
        public Reactor.Async.Future<Reactor.Buffer> Read() {
            return this.Read(this.buffer.Length);
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Disposes of this reader.
        /// </summary>
        public void Dispose() {
            this.queue.Dispose();
            this.stream.Dispose();
            this.buffer = null;
        }

        #endregion

        #region Statics

        /// <summary>
        /// Creates a new Reader.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="buffersize">The read buffer size in bytes.</param>
        /// <returns></returns>
        public static Reader2 Create(System.IO.Stream stream, int buffersize) {
            return new Reader2(stream, buffersize);
        }


        /// <summary>
        /// Creates a new Reader.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="buffersize">The read buffer size in bytes.</param>
        /// <returns></returns>
        public static Reader2 Create(System.IO.Stream stream) {
            return new Reader2(stream, Reactor.Settings.DefaultBufferSize);
        }

        #endregion
    }
}
