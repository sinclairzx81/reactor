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
using System.Collections.Generic;
using System.Text;

namespace Reactor.Http {

    public class Request : Reactor.IWritable {
        private Reactor.Action<Reactor.Http.Response> onresponse;
        private Reactor.Async.Queue  queue;
        private Reactor.Tcp.Socket   socket;
        private Reactor.Http.Headers headers;
        private System.Uri           uri;
        private System.String        method;
        private System.Boolean       headers_sent;

        public Request(Uri uri, Reactor.Action<Reactor.Http.Response> onresponse) {
            this.onresponse   = onresponse;
            this.queue        = Reactor.Async.Queue.Create(1);
            this.uri          = uri;
            this.socket       = Reactor.Tcp.Socket.Create(this.uri.DnsSafeHost, this.uri.Port);
            this.headers      = new Reactor.Http.Headers();
            this.method       = "GET";
            this.headers_sent = false;
        }

        #region Properties

        public string Method {
            get { return this.method; }
        }

        public Headers Headers {
            get {  return this.headers; }
        }

        #endregion

        #region Events

        public void OnDrain(Reactor.Action action) {
            this.socket.OnDrain(action);
        }

        public void OnceDrain(Reactor.Action action) {
            this.socket.OnceDrain(action);
        }

        public void RemoveDrain(Reactor.Action action) {
            this.socket.RemoveDrain(action);
        }

        public void OnError(Reactor.Action<Exception> action) {
            this.socket.OnError(action);
        }

        public void RemoveError(Reactor.Action<Exception> action) {
            this.socket.RemoveError(action);
        }

        public void OnEnd(Reactor.Action action) {
            this.socket.OnEnd(action);
        }

        public void RemoveEnd(Reactor.Action action) {
            this.socket.RemoveEnd(action);
        }

        #endregion

        #region Methods

        public Reactor.Async.Future Write(Reactor.Buffer buffer) {
            return new Reactor.Async.Future((resolve, reject) => {
                this.queue.Run(next => {
                    this.WriteHeaders()
                        .Error  (reject)
                        .Then   (() => { 
                            this.socket.Write(buffer)
                                       .Error(reject)
                                       .Then(resolve)
                                       .Finally(next);
                        });
                });
            });
        }

        public Reactor.Async.Future Flush() {
            return new Reactor.Async.Future((resolve, reject) => {
                this.queue.Run(next => {
                    this.WriteHeaders()
                        .Error  (reject)
                        .Then   (() => { 
                            this.socket.Flush()
                                       .Error(reject)
                                       .Then(resolve)
                                       .Finally(next);
                        });
                });
            });
        }

        public Async.Future End() {
            return new Reactor.Async.Future((resolve, reject) => {
                this.queue.Run(next => {
                    this.WriteHeaders()
                        .Error  (reject)
                        .Then   (() => { 
                            this.socket.End()
                                       .Error(reject)
                                       .Then(resolve)
                                       .Finally(next);
                        });
                });
            });
        }

        public void Cork() {
            this.socket.Cork();
        }

        public void Uncork() {
            this.socket.Uncork();
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

        #region Internal

        private Reactor.Async.Future WriteHeaders() {
            if (this.headers_sent) { 
                return Reactor.Async.Future.Resolved();
            }
            return Reactor.Async.Future.Create((resolve, reject) => {
                this.headers["Host"] = this.uri.DnsSafeHost + this.uri.Port.ToString();
                var buffer = Reactor.Buffer.Create(128);
                buffer.Write("{0} {1} HTTP/1.1\r\n", this.method, this.uri.PathAndQuery);
                buffer.Write(this.headers.ToString());
                this.socket.Write(buffer)
                           .Then(() => {
                                 this.headers_sent = true;
                           //      Reactor.Http.Protocol.HeaderReader.ReadResponse(socket)
                           //                                        .Then(header => {
                           //          var response = new Reactor.Http.Response(socket);
                           //          this.onresponse(response);
                           //      }).Error(error => {
                           //    reject(error);
                           //});
                           })
                           .Error(error => {
                               reject(error);
                           });
            });
        }
        

        #endregion

        #region Machine

        private void _Error(Exception error) {

        }

        private void _End() {

        }

        #endregion

        #region Statics

        public static Reactor.Http.Request Create(string url, Reactor.Action<Reactor.Http.Response> onresponse) {
            return new Reactor.Http.Request(new Uri(url), onresponse);
        }

        #endregion

    }
}
