/*--------------------------------------------------------------------------

Reactor.Web.Sockets

The MIT License (MIT)

Copyright (c) 2014 Haydn Paterson (sinclair) <haydn.developer@gmail.com>

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

namespace Reactor.Web.Socket
{
    public class Server
    {
        public Reactor.Action<Reactor.Web.Socket.Context, Reactor.Action<bool, string>> OnUpgrade { get; set; }

        public Reactor.Action<Socket>                   OnSocket    { get; set; }

        public Reactor.Action<Exception>                OnError     { get; set; }

        private string                              path;

        private Reactor.Http.Server                     server;

        private Reactor.Action<Reactor.Http.HttpContext>    servercb;

        #region Constructors

        public Server(int port, string path)
        {
            this.path = path;

            this.server = Reactor.Http.Server.Create(context => {

                context.Response.StatusCode = 401;

                context.Response.ContentType = "text/plain";

                context.Response.Write("method not allowed");

                context.Response.End();

            }).Listen(port);

            this.servercb = this.server.OnContext;

            this.server.OnContext = this.OnContext;

            this.OnUpgrade = (context, callback) => callback(true, string.Empty);
        }

        public Server(Reactor.Http.Server server, string path)
        {
            this.path   = path;

            this.server = server;

            if(this.server.OnContext == null)
            {
                this.server.OnContext = context =>
                {
                    context.Response.StatusCode = 401;

                    context.Response.ContentType = "text/plain";

                    context.Response.Write("method not allowed");

                    context.Response.End();
                };
            }

            this.servercb = this.server.OnContext;

            this.server.OnContext = this.OnContext;

            this.OnUpgrade = (context, callback) => callback(true, string.Empty);
        }

        #endregion

        public void  OnContext(Reactor.Http.HttpContext context)
        {
            if(this.path != context.Request.Url.AbsolutePath)
            {
                this.servercb(context);

                return;
            }

            this.Upgrade(context, (exception, socket) => {

                if (exception != null) {

                    if (this.OnError != null) {

                        this.OnError(exception);
 
                        return;
                    }
                }

                if (socket != null) {

                    if (this.OnSocket != null) {

                        this.OnSocket(socket);
                    }
                }
            });

        }

        #region Upgrade

        private void Upgrade(Reactor.Http.HttpContext context, Reactor.Action<Exception, Reactor.Web.Socket.Socket> callback)
        {
            var request = ServerWebSocketUpgradeRequest.Create(context);

            //--------------------------------------------------------
            // if not a web socket attempt, defer to http callback.
            //--------------------------------------------------------

            if (request == null) {
                
                this.servercb(context);

                return;
            }

            var response       = ServerWebSocketUpgradeResponse.Create(request);

            var socket_context = new Reactor.Web.Socket.Context(context);

            this.OnUpgrade(socket_context, (success, reason) => {

                if (!success) {

                    response.Reject(reason == null ? "" : reason, (exception) => callback(exception, null));

                    return;
                }

                response.Accept((exception) => {

                    if(exception != null) {

                        callback(exception, null);

                        return;
                    }

                    var channel = new Transport(context.Connection);

                    var socket  = new Socket(channel);

                    socket.Context = socket_context;

                    callback(null, socket);
                });
            });
        }

        #endregion

        #region Statics

        public static Server Create(int port)
        {
            return new Server(port, "/");
        }

        public static Server Create(Reactor.Http.Server server)
        {
            return new Server(server, "/");
        }

        public static Server Create(int port, string path)
        {
            return new Server(port, path);
        }

        public static Server Create(Reactor.Http.Server server, string path)
        {
            return new Server(server, path);
        }

        public static Server Create(int port, Action<Socket> OnSocket)
        {
            var wsserver = new Server(port, "/");

            wsserver.OnSocket = OnSocket;

            return wsserver;
        }

        public static Server Create(Reactor.Http.Server server, Action<Socket> OnSocket)
        {
            var wsserver = new Server(server, "/");

            wsserver.OnSocket = OnSocket;

            return wsserver;
        }

        public static Server Create(int port, string path, Action<Socket> OnSocket)
        {
            var wsserver = new Server(port, path);

            wsserver.OnSocket = OnSocket;

            return wsserver;
        }

        public static Server Create(Reactor.Http.Server server, string path, Action<Socket> OnSocket)
        {
            var wsserver = new Server(server, path);

            wsserver.OnSocket = OnSocket;

            return wsserver;
        }        

        #endregion
    }
}
