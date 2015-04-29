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
using System.Globalization;
using System.Text;

namespace Reactor.Http {

    /// <summary>
    /// Reactor HTTP request.
    /// </summary>
    public class Request : Reactor.IWritable {
        private Reactor.Tcp.Socket                     socket;
        private Reactor.Async.Event                    ondrain;
        private Reactor.Async.Event<Response>          onread;
        private Reactor.Async.Event<Exception>         onerror;
        private Reactor.Async.Event                    onend;
        private Reactor.Http.Headers                   headers;
        private Uri                                    uri;
        private string                                 method;
        private string                                 connection;
        private int                                    content_length;
        private bool                                   started;

        #region Constructors

        public Request(Uri uri, Reactor.Action<Reactor.Http.Response> callback) {
            this.ondrain        = new Reactor.Async.Event();
            this.onread         = new Reactor.Async.Event<Response>();
            this.onerror        = new Reactor.Async.Event<Exception>();
            this.onend          = new Reactor.Async.Event();
            this.onread.On(callback);

            this.headers        = new Reactor.Http.Headers();
            this.uri            = uri;
            this.method         = "GET";
            this.content_length = 0;
            this.connection     = "close";
            this.started        = false;
        }

        #endregion

        #region Properties

        public Reactor.Http.Headers Headers {
            get { return this.headers; }
        }

        public string Method {
            get { return this.method; }
            set { this.method = value; }
        }

        public int ContentLength {
            get {  return this.content_length; }
            set {  this.content_length = value; }
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
        /// <param name="callback"></param>
        public void OnceDrain(Reactor.Action callback) {
            this.ondrain.Once(callback);
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
            this.socket.OnError(callback);
        }

        /// <summary>
        /// Unsubscribes this action from the 'error' event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveError (Reactor.Action<Exception> callback) {
            this.socket.RemoveError(callback);
        }

        /// <summary>
        /// Subscribes this action to the 'end' event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnEnd(Reactor.Action callback) {

        }
        /// <summary>
        /// Unsubscribes this action from the 'end' event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveEnd(Reactor.Action callback) {

        }

        #endregion

        #region Methods

        public Reactor.Async.Future Write (Reactor.Buffer buffer) {
            return new Reactor.Async.Future((resolve, reject) => {
                this.Begin().Then(() => {
                    this.socket.Write(buffer)
                               .Then(resolve)
                               .Error(reject);
                }).Error(reject);
            });
        }

        public Reactor.Async.Future Flush() {
            return new Reactor.Async.Future((resolve, reject) => {
                this.Begin().Then(() => {
                    this.socket.Flush()
                               .Then(resolve)
                               .Error(reject);
                }).Error(reject);
            });
        }

        public Reactor.Async.Future End () {
            return new Reactor.Async.Future((resolve, reject) => {
                this.Begin().Then(resolve).Error(reject);
            });
        }

        #endregion

        #region Internals

        /// <summary>
        /// Writes the http headers to this socket.
        /// </summary>
        private Reactor.Async.Future WriteHeaders () {
            return new Reactor.Async.Future((resolve, reject) => {
                headers["Host"]       = this.uri.DnsSafeHost + this.uri.Port.ToString();
                headers["Connection"] = this.connection;
                var buffer = Reactor.Buffer.Create(128);
                buffer.Write("{0} {1} HTTP/1.1\r\n", this.method, this.uri.PathAndQuery);
                buffer.Write(this.headers.ToString());
                this.socket.Write(buffer).Then(resolve).Error(reject);
            });
        }

        private Reactor.Async.Future Begin() {
            /* if this request is already 
             * started, then resolve immediately
             */
            if(this.started) {
                return new Reactor.Async.Future((resolve, reject) => resolve());
            }
            return new Reactor.Async.Future((resolve, reject) => {
                this.socket = Reactor.Tcp.Socket.Create(this.uri.DnsSafeHost, this.uri.Port);
                this.WriteHeaders().Then(() => {
                    var response = new Reactor.Http.Response(this.socket);
                    response.Begin().Then(() =>{
                        this.onread.Emit(response);
                        this.started = true;
                    }).Error(reject);
                }).Then(resolve).Error(reject);
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

        #region Static

        /// <summary>
        /// Returns a new http request object.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static Reactor.Http.Request Create(string endpoint, Reactor.Action<Response> callback) {
            return new Reactor.Http.Request(new Uri(endpoint), callback);
        }

        #endregion
    }
}
