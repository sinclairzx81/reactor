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

using System.Collections.Generic;

namespace Reactor.Web
{
    public class Router
    {
        private List<Route>      routes;

        private List<Middleware> middleware;

        public Router()
        {
            this.routes     = new List<Route>();

            this.middleware = new List<Middleware>();
        }

        #region Handler

        public void Handler(Reactor.Web.Context context, Reactor.Action next) {

            Reactor.Web.MiddlewareProcessor.Process(context, this.middleware, () => {

                foreach (var route in this.routes) {

                    if (route.Match(context.Request)) {

                        context.Params = route.ComputeParams(context.Request);

                        route.Invoke(context);

                        return;
                    }
                }

                next();
            });
        }

        #endregion

        #region Methods

        public Router Use(Reactor.Web.Middleware middleware)
        {
            this.middleware.Add(middleware);

            return this;
        }

        public Router Get(string pattern, Reactor.Action<Context> handler)
        {
            this.routes.Add(new Route(pattern, "GET", handler));

            return this;
        }

        public Router Post(string pattern, Reactor.Action<Context> handler)
        {
            this.routes.Add(new Route(pattern, "POST", handler));

            return this;
        }

        public Router Put(string pattern, Reactor.Action<Context> handler)
        {
            this.routes.Add(new Route(pattern, "PUT", handler));

            return this;
        }

        public Router Delete(string pattern, Reactor.Action<Context> handler)
        {
            this.routes.Add(new Route(pattern, "DELETE", handler));

            return this;
        }

        public Router Options(string pattern, Reactor.Action<Context> handler)
        {
            this.routes.Add(new Route(pattern, "OPTIONS", handler));

            return this;
        }

        public Router Get(string pattern, Reactor.Web.Middleware[] middleware, Reactor.Action<Context> handler)
        {
            this.routes.Add(new Route(pattern, "GET", middleware, handler));

            return this;
        }

        public Router Post(string pattern, Reactor.Web.Middleware[] middleware, Reactor.Action<Context> handler)
        {
            this.routes.Add(new Route(pattern, "POST", middleware, handler));

            return this;
        }

        public Router Put(string pattern, Reactor.Web.Middleware[] middleware, Reactor.Action<Context> handler)
        {
            this.routes.Add(new Route(pattern, "PUT", middleware, handler));

            return this;
        }

        public Router Delete(string pattern, Reactor.Web.Middleware[] middleware, Reactor.Action<Context> handler)
        {
            this.routes.Add(new Route(pattern, "DELETE", middleware, handler));

            return this;
        }

        public Router Options(string pattern, Reactor.Web.Middleware[] middleware, Reactor.Action<Context> handler)
        {
            this.routes.Add(new Route(pattern, "OPTIONS", middleware, handler));

            return this;
        }

        #endregion

        #region Statics

        public static Router Create()
        {
            return new Router();
        }

        #endregion
    }
}
