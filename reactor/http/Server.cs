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

namespace Reactor.Http {

    /// <summary>
    /// Reactor HTTP Server.
    /// </summary>
    public class Server {

        #region Events

        public class Events {
            public Reactor.Async.Event<Reactor.Http.Context> Read  {  get; set; }
            public Reactor.Async.Event<System.Exception>     Error {  get; set; }
            public Reactor.Async.Event                       End   {  get; set; }
        }

        #endregion

        private Reactor.Tcp.Server                        server;
        private Reactor.Async.Event<Reactor.Http.Context> onread;
        private Reactor.Async.Event<System.Exception>     onerror;
        private Reactor.Async.Event                       onend;

        #region Constructors

        public Server() {
            this.server    = Reactor.Tcp.Server.Create(this._Socket);
            this.onread    = Reactor.Async.Event.Create<Reactor.Http.Context>();
            this.onerror   = Reactor.Async.Event.Create<System.Exception>();
            this.onend     = Reactor.Async.Event.Create();
        }

        #endregion

        #region Events

        public Events GetEvents() {
            return new Events {
                Read  = this.onread,
                Error = this.onerror,
                End   = this.onend
            };
        }

        public void OnRead (Reactor.Action<Reactor.Http.Context> callback) {
            this.onread.On(callback);
        }

        public void OnError (Reactor.Action<Exception> callback) {
            this.onerror.On(callback);
        }

        public void RemoveContext (Reactor.Action<Reactor.Http.Context> callback) {
            this.onread.Remove(callback);
        }

        public void RemoveError (Reactor.Action<Exception> callback) {
            this.onerror.Remove(callback);
        }

        public void Listen (int port) {
            this.server.Listen(port);
        }

        public void Stop() {
            this.server.Dispose();
        }

        #endregion

        #region Internals

        private void _Socket (Reactor.Tcp.Socket socket) {
            var request  = new Reactor.Http.IncomingMessage (socket);
            var response = new Reactor.Http.ServerResponse  (socket);
            request.BeginRequest().Then(() => {
                this.onread.Emit(Reactor.Http.Context.Create(request, response));
            }).Error(error => {
                response.StatusCode = 400;
                response.StatusDescription = "Bad Request";
                response.End();
            });
        }

        #endregion

        #region Statics

        public static Server Create() {
            return new Reactor.Http.Server();
        }

        public static Server Create(Reactor.Action<Reactor.Http.Context> callback) {
            var server = new Reactor.Http.Server();
            server.OnRead(callback);
            return server;
        }

        #endregion
    }
}
