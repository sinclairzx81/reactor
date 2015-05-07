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
using System.Globalization;
using System.Text;

namespace Reactor.Http {

    /// <summary>
    /// Reactor HTTP server response.
    /// </summary>
    public class ServerResponse : Reactor.IWritable {
        private Reactor.IWritable     writable;
        private Reactor.Async.Queue   queue;
        private Reactor.Http.Headers  headers;
        private Reactor.Http.Cookies  cookies;
        private System.Version        version;
        private System.Int32          status_code;
        private System.String         status_description;

        private bool                  header_sent;

        #region Constructors

        public ServerResponse(Reactor.IWritable writable) {
            this.writable           = writable;
            this.queue              = Reactor.Async.Queue.Create(1);
            this.headers            = new Reactor.Http.Headers();
            this.cookies            = new Reactor.Http.Cookies();
            this.version            = new Version(1, 1);
            this.status_code        = 200; 
            this.status_description = "OK";
            this.header_sent        = false;

            /* defaults */
            this.headers["Server"]            = "Reactor-HTTP Server";
            this.headers["Transfer-Encoding"] = "chunked";
            this.headers["Connection"]        = "closed";
            this.headers["Cache-Control"]     = "no-cache";
            this.headers["Date"]              = DateTime.UtcNow.ToString("r");
        }

        #endregion

        #region Properties

        /// <summary>
        /// The HTTP headers for this response.
        /// </summary>
        public Reactor.Http.Headers Headers {
            get {
                return this.headers;
            }
        }

        /// <summary>
        /// The HTTP cookies for this response.
        /// </summary>
        public Reactor.Http.Cookies Cookies {
            get {
                return this.cookies;
            }
        }

        /// <summary>
        /// The HTTP status code.
        /// </summary>
        public int StatusCode {
            get {
                return this.status_code;
            }
            set {
                this.status_code = value;
            }
        }
        /// <summary>
        /// The HTTP Status description.
        /// </summary>
        public string StatusDescription  {
            get {
                return this.status_description;
            }
            set {
                this.status_description = value;
            }
        }
        /// <summary>
        /// The Content-Length for this request. Note: if 
        /// setting a value for the Content-Length, the 
        /// transfer-encoding header will be 
        /// removed from this response.
        /// </summary>
        public long ContentLength {
            get  {
                long result = 0;
                long.TryParse(this.headers["Content-Length"], out result);
                return result;
            }
            set  {
                this.headers.Remove("Transfer-Encoding");
                this.headers["Content-Length"] = value.ToString();
            }
        }

        public string ContentType {
            get {
                return this.headers["Content-Type"];
            }
            set {
                this.headers["Content-Type"] = value;
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Subscribes this action to the 'drain' event. The event indicates
        /// when a write operation has completed and the caller should send
        /// more data.
        /// </summary>
        /// <param name="callback"></param>
        public void OnDrain(Reactor.Action callback) {
            this.writable.OnDrain(callback);
        }

        /// <summary>
        /// Subscribes this action once to the 'drain' event. The event indicates
        /// when a write operation has completed and the caller should send
        /// more data.
        /// </summary>
        /// <param name="callback"></param>
        public void OnceDrain(Reactor.Action callback) {
            this.writable.OnceDrain(callback);
        }

        /// <summary>
        /// Unsubscribes from the 'drain' event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveDrain(Reactor.Action callback) {
            this.writable.RemoveDrain(callback);
        }

        /// <summary>
        /// Subscribes this action to the 'end' event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnEnd(Reactor.Action callback) {
            this.writable.OnEnd(callback);
        }

        /// <summary>
        /// Unsubscribes this action from the 'end' event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveEnd(Reactor.Action callback) {
            this.writable.RemoveEnd(callback);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Writes this buffer to the stream. This method returns a Reactor.Future
        /// which resolves once this buffer has been written.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public Reactor.Async.Future Write (Reactor.Buffer buffer) {
            return this._Write(buffer);
        }

        /// <summary>
        /// Flushes this stream. This method returns a Reactor.Future which
        /// resolves once the stream has been flushed.
        /// </summary>
        /// <returns></returns>
        public Reactor.Async.Future Flush() {
            return this._Flush();
        }

        /// <summary>
        /// Ends and disposes of the underlying resource. This method returns
        /// a Reactor.Future which resolves once this stream has been ended.
        /// </summary>
        /// <returns></returns>
        public Reactor.Async.Future End () {
            return this._End();
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

        public void OnError     (Reactor.Action<Exception> callback)
        {
            this.writable.OnError(callback);
        }

        public void RemoveError (Reactor.Action<Exception> callback) {

            this.writable.RemoveError(callback);
        }

        #endregion

        #region Internal

        /// <summary>
        /// Writes headers to this writable. If headers
        /// have already been sent, this call is resolved
        /// immediately. In addition, once the headers
        /// have been sent, this method will detect
        /// the transfer-encoding as 'chunked' and 
        /// swap out this writable for a chunked body
        /// writable.
        /// </summary>
        /// <returns></returns>
        private Reactor.Async.Future _WriteHeaders() {
            if(this.header_sent) return Reactor.Async.Future.Resolved();
            return new Reactor.Async.Future((resolve, reject) => {
                var buffer = Reactor.Buffer.Create(128);
                buffer.Write("HTTP/{0} {1} {2}\r\n", version, status_code, status_description);
                buffer.Write(this.headers.ToString());
                this.writable.Write(buffer)
                             .Then(() => {
                                this.header_sent = true;
                                this.writable    = (this.headers["Transfer-Encoding"] == "chunked") ? 
                                    (Reactor.IWritable)new Reactor.Http.Protocol.ChunkedBodyWriter(this.writable) :
                                    (Reactor.IWritable)this.writable;
                             }).Then(resolve)
                               .Error(reject);
            });
        }

        /// <summary>
        /// Writes this buffer to the underlying writable.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private Reactor.Async.Future _Write (Reactor.Buffer buffer) {
            return new Reactor.Async.Future((resolve, reject) => {
                this.queue.Run(next => {
                    this._WriteHeaders().Then(() => {
                        this.writable.Write(buffer)
                                     .Then(resolve)
                                     .Error(reject)
                                     .Finally(next);
                    }).Error(reject)
                      .Finally(next);
                });
            });
        }

        /// <summary>
        /// Flushes the underlying writable.
        /// </summary>
        /// <returns></returns>
        private Reactor.Async.Future _Flush () {
            return new Reactor.Async.Future((resolve, reject) => {
                this.queue.Run(next => {
                    this._WriteHeaders().Then(() => {
                        this.writable.Flush()
                                     .Then(resolve)
                                     .Error(reject)
                                     .Finally(next);
                    }).Error(reject)
                      .Finally(next);
                });
            });
        }

        /// <summary>
        /// Ends the underlying writable.
        /// </summary>
        /// <returns></returns>
        private Reactor.Async.Future _End () {
            return new Reactor.Async.Future((resolve, reject) => {
                this.queue.Run(next => {
                    this._WriteHeaders().Then(() => {
                        this.writable.End()
                                     .Then(resolve)
                                     .Error(reject)
                                     .Finally(next);
                    }).Error(reject)
                      .Finally(next);
                });
            });
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
            return this.Write(Reactor.Buffer.Create(buffer));
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
    }
}
