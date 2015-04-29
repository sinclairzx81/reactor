/*--------------------------------------------------------------------------

Reactor.Web

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

using Reactor.Http;
using System;

namespace Reactor.Web
{
    public class Server
    {
        private Reactor.Http.Server                             http;

        private Reactor.Web.Router                              router;

        private Reactor.Async.Event<System.Exception>           onerror;

        private Reactor.Async.Event<Reactor.Http.Context> oncontext;

        public Server(Reactor.Http.Server http) {
            
            this.oncontext            = new Async.Event<Reactor.Http.Context>();

            this.onerror              = new Async.Event<Exception>();
            
            this.router               = new Router();
            
            this.http                 = http;
            
            this.http.OnError   (this.onerror.Emit);

            //-------------------------------------
            // reassign events
            //-------------------------------------

            var events = this.http.GetEvents();
            
            foreach (var listener in events.Read.Subscribers()) {
                
                events.Read.Remove(listener);
                
                this.oncontext.On(listener);
            };

            this.http.OnRead (this._Context);
        }

        public Server(): this(Reactor.Http.Server.Create()) {

            
        }

        private void _Context(Reactor.Http.Context context)
        {
            var _context = new Reactor.Web.Context(context);

            //-----------------------------------------
            // try handle request
            //-----------------------------------------
            this.router.Handler(_context, () => {

                //-----------------------------------------
                // if we have external subscribers
                //-----------------------------------------
                if (Reactor.Enumerable.Create(this.oncontext.Subscribers()).Count() > 0) {

                    this.oncontext.Emit(context);

                    return;
                }

                //-----------------------------------------
                // otherwise, emit 404
                //-----------------------------------------

                context.Response.StatusCode  = 404;

                context.Response.ContentType = "text/plain";

                context.Response.Write(context.Request.Url.AbsolutePath + " not found");

                context.Response.End();
            });
        }

        #region Methods

        public Router Use(Reactor.Web.Middleware middleware)
        {
            return this.router.Use(middleware);
        }

        public Router Get(string pattern, Reactor.Action<Context> handler)
        {
            return this.router.Get(pattern, handler);
        }

        public Router Post(string pattern, Reactor.Action<Context> handler)
        {
            return this.router.Post(pattern, handler);
        }

        public Router Put(string pattern, Reactor.Action<Context> handler)
        {
            return this.router.Put(pattern, handler);
        }

        public Router Delete(string pattern, Reactor.Action<Context> handler)
        {
            return this.router.Delete(pattern, handler);
        }

        public Router Options(string pattern, Reactor.Action<Context> handler)
        {
            return this.router.Options(pattern, handler);
        }

        public Router Get(string pattern, Reactor.Web.Middleware[] middleware, Reactor.Action<Context> handler)
        {
            return this.router.Get(pattern, middleware, handler);
        }

        public Router Post(string pattern, Reactor.Web.Middleware[] middleware, Reactor.Action<Context> handler)
        {
            return this.router.Post(pattern, middleware, handler);
        }

        public Router Put(string pattern, Reactor.Web.Middleware[] middleware, Reactor.Action<Context> handler)
        {
            return this.router.Put(pattern, middleware, handler);
        }

        public Router Delete(string pattern, Reactor.Web.Middleware[] middleware, Reactor.Action<Context> handler)
        {
            return this.router.Delete(pattern, middleware, handler);
        }

        public Router Options(string pattern, Reactor.Web.Middleware[] middleware, Reactor.Action<Context> handler)
        {
            return this.router.Options(pattern, middleware, handler);
        }

        #endregion

        public Server Listen(int port) {

            this.http.Listen(port);

            return this;
        }

        public Server Stop() {

            this.http.Stop();

            return this;
        }

        #region Statics

        public static Server Create(Reactor.Http.Server http) {

            return new Server(http);
        }

        public static Server Create() {

            return new Server();
        }

        #endregion
    }
}