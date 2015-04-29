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

    public class Request : IWritable {
        private Reactor.Tcp.Socket                     socket;
        private Reactor.Async.Queue                    queue;
        private Reactor.Async.Event<IncomingMessage>   onresponse;
        private Reactor.Http.Headers                   headers;
        private Uri                                    uri;
        private string                                 method;
        private int                                    content_length;
        private bool                                   header_sent;

        public Request(Uri uri) {
            this.queue          = new Reactor.Async.Queue(1);
            this.onresponse     = new Reactor.Async.Event<IncomingMessage>();
            this.headers        = new Reactor.Http.Headers();
            this.uri            = uri;
            this.method         = "GET";
            this.content_length = 0;
            this.header_sent    = false;
            this.queue.Pause();
        }


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

        public void OnResponse (Reactor.Action<IncomingMessage> callback) {
            this.onresponse.On(callback);
        }

        public void RemoveResponse (Reactor.Action<IncomingMessage> callback) {
            this.onresponse.Remove(callback);
        }

        public void OnError (Reactor.Action<Exception> callback) {
            this.socket.OnError(callback);
        }

        public void RemoveError (Reactor.Action<Exception> callback) {
            this.socket.RemoveError(callback);
        }

        #endregion

        #region IWritable

        public Reactor.Async.Future Write (Reactor.Buffer buffer) {
            return new Reactor.Async.Future((resolve, reject) => {
                if (!this.header_sent) {
                    this.header_sent = true;
                    this.BeginRequest();
                }
                this.queue.Run(next => {
                    this.socket.Write(buffer)
                               .Then(resolve)
                               .Then(next)
                               .Error(reject);
                });
            });
        }

        public Reactor.Async.Future End () {
            return new Reactor.Async.Future((resolve, reject) => {
                if (!this.header_sent) {
                    this.header_sent = true;
                    this.BeginRequest();
                }
                this.queue.Run(next => {
                    this.socket.End()
                               .Then(resolve)
                               .Then(next)
                               .Error(reject);
                    
                });
            });
        }



        #endregion

        #region Internals

        /// <summary>
        /// Writes the HTTP Header.
        /// </summary>
        private void WriteHeader () {
            var culture_info = CultureInfo.InvariantCulture;
            headers.Set_Internal("Accept",          "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            headers.Set_Internal("Accept-Encoding", "gzip, deflate, sdch");
            headers.Set_Internal("Accept-Language", "en-US,en;q=0.8");
            headers.Set_Internal("Cache-Control",   "no-cache");
            headers.Set_Internal("Connection",      "keep-alive");
            headers.Set_Internal("Pragma",          "no-cache");
            headers.Set_Internal("User-Agent",      "reactor http client");
            if (this.content_length == 0) {
                headers.Set_Internal("Transfer-Encoding", "chunked");
            }
            else {
                headers.Set_Internal("Content-Length", content_length.ToString());
            }
            /* write: */
            var buffer = Reactor.Buffer.Create(128);
            buffer.Write("{0} HTTP/1.1 {1}\r\n", this.method, this.uri.PathAndQuery);
            buffer.Write(this.headers.ToString());
            this.socket.Write(buffer);
            this.queue.Resume();
        }

        private void BeginRequest() {
            this.socket = Reactor.Tcp.Socket.Create(this.uri.DnsSafeHost, this.uri.Port);
            this.socket.OnError(error => Console.WriteLine(error));
            this.socket.OnConnect(() => {
                Console.WriteLine("connected");
                this.WriteHeader();

            });
            this.socket.OnRead(data => {
                Console.WriteLine("HAVE ADA");
                Console.WriteLine(data);
                });
            
        }

        #endregion

        #region Buffer

        public void Write (byte[] buffer, int index, int count) {
            this.Write(Reactor.Buffer.Create(buffer, 0, count));
        }

        public void Write (byte[] buffer) {
            this.Write(Reactor.Buffer.Create(buffer));
        }

        public void Write (string data) {
            this.Write(System.Text.Encoding.UTF8.GetBytes(data));
        }

        public void Write (string format, params object[] args) {
            format = string.Format(format, args);
            this.Write(System.Text.Encoding.UTF8.GetBytes(format));
        }

        public void Write (byte data) {
            this.Write(new byte[1] { data });
        }

        public void Write (bool value) {
            this.Write(BitConverter.GetBytes(value));
        }

        public void Write (short value) {
            this.Write(BitConverter.GetBytes(value));
        }

        public void Write (ushort value) {
            this.Write(BitConverter.GetBytes(value));
        }

        public void Write (int value) {
            this.Write(BitConverter.GetBytes(value));
        }

        public void Write (uint value) {
            this.Write(BitConverter.GetBytes(value));
        }

        public void Write (long value) {
            this.Write(BitConverter.GetBytes(value));
        }

        public void Write (ulong value) {
            this.Write(BitConverter.GetBytes(value));
        }

        public void Write (float value) {
            this.Write(BitConverter.GetBytes(value));
        }

        public void Write (double value) {
            this.Write(BitConverter.GetBytes(value));
        }

        #endregion

        #region Static

        public static Reactor.Http.Request Create(string endpoint, Reactor.Action<IncomingMessage> callback) {
            var request = new  Request(new Uri(endpoint));
            request.OnResponse(callback);
            return request;
        }

        #endregion

        public void OnDrain(Action callback)
        {
            throw new NotImplementedException();
        }

        

        public void RemoveDrain(Action callback)
        {
            throw new NotImplementedException();
        }

        public void OnEnd(Action callback)
        {
            throw new NotImplementedException();
        }

        public void RemoveEnd(Action callback)
        {
            throw new NotImplementedException();
        }

        Async.Future IWritable.Write(Buffer buffer)
        {
            throw new NotImplementedException();
        }

        public Async.Future Flush()
        {
            throw new NotImplementedException();
        }

        Async.Future IWritable.End()
        {
            throw new NotImplementedException();
        }

        public void OnceDrain(Action callback)
        {
            throw new NotImplementedException();
        }
    }
}
