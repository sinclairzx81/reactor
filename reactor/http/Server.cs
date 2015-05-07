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

        /// <summary>
        /// Creates a new http server.
        /// </summary>
        public Server() {
            this.server    = Reactor.Tcp.Server.Create(this.Accept);
            this.onread    = Reactor.Async.Event.Create<Reactor.Http.Context>();
            this.onerror   = Reactor.Async.Event.Create<System.Exception>();
            this.onend     = Reactor.Async.Event.Create();
        }

        #endregion

        #region Events

        /// <summary>
        /// Gets the internal events for this server.
        /// </summary>
        /// <returns></returns>
        public Events GetEvents() {
            return new Events {
                Read  = this.onread,
                Error = this.onerror,
                End   = this.onend
            };
        }

        /// <summary>
        /// Subscribes this action to the 'read' event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnRead (Reactor.Action<Reactor.Http.Context> callback) {
            this.onread.On(callback);
        }

        /// <summary>
        /// Unsubscribes this action from the 'read' event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveRead (Reactor.Action<Reactor.Http.Context> callback) {
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
        public void OnEnd(Reactor.Action callback) {
            this.onend.On(callback);
        }

        /// <summary>
        /// Unsubscribes this action from the 'end' event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveEnd(Reactor.Action callback) {
            this.onend.Remove(callback);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Starts this server listening on the specified port.
        /// </summary>
        /// <param name="port"></param>
        public void Listen (int port) {
            this.server.Listen(port);
        }

        /// <summary>
        /// Stops this server.
        /// </summary>
        public void Stop() {
            this.server.Dispose();
        }

        #endregion

        #region Internals

        /// <summary>
        /// Accepts incoming tcp requests and processes
        /// them as http requests.
        /// </summary>
        /// <param name="socket"></param>
        private void Accept (Reactor.Tcp.Socket socket) {
            Reactor.Http.Binder.Bind(socket, context =>{
                this.onread.Emit(context);
            });
        }

        #endregion

        #region Statics

        /// <summary>
        /// Returns a new http server.
        /// </summary>
        /// <returns></returns>
        public static Server Create() {
            return new Reactor.Http.Server();
        }

        /// <summary>
        /// Returns a new http server.
        /// </summary>
        /// <param name="callback">A callback to handle incoming http requests.</param>
        /// <returns></returns>
        public static Server Create(Reactor.Action<Reactor.Http.Context> callback) {
            var server = new Reactor.Http.Server();
            server.OnRead(callback);
            return server;
        }

        #endregion
    }
}