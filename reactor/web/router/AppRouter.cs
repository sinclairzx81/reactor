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
using System.Text.RegularExpressions;

namespace Reactor.Web {

    /// <summary>
    /// Specialized HTTP context with extended with various web framework functionality.
    /// </summary>
    public class Context {

        #region Fields 

        private Reactor.Http.Context         context;
        private Dictionary<string, string>   parameters;
        private Dictionary<string, object>   state;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new reactor web context.
        /// </summary>
        /// <param name="route">(optional) the route associated with this context.</param>
        /// <param name="context">The incoming http context.</param>
        internal Context(Reactor.Web.Route route, Reactor.Http.Context context) {
            this.context = context;
            this.state   = new Dictionary<string, object>();
            this.parameters = (route == null)
                    ? new Dictionary<string, string>()
                    : route.ReadUriParameters(this.context.Request.Url.AbsolutePath);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the http request associated with this context.
        /// </summary>
        public Reactor.Http.ServerRequest Request {
            get { return this.context.Request; }
        }
        /// <summary>
        /// Gets the http response associated with this context.
        /// </summary>
        public Reactor.Http.ServerResponse Response {
            get { return this.context.Response; }
        }
        
        /// <summary>
        /// Gets the http request uri parameters associated with this context.
        /// </summary>
        public IDictionary<string, string> Parameters {
            get { return this.parameters; }
        }

        /// <summary>
        /// Gets the web request objects associated with this request.
        /// </summary>
        public IDictionary<string, object> State {
            get { return this.state; }
        }

        #endregion
    }

    /// <summary>
    /// Reactor web route.
    /// </summary>
    internal class Route {

        #region Fields
        private  string                       method;
        private  string                       pattern;
        internal List<string>                 names;
        internal Regex                        regex;
        private List<Reactor.Action<Reactor.Web.Context,
                                    Reactor.Action<Reactor.Web.Context>>> middlewares;
        #endregion

        #region Constructor

        public Route(string pattern, string method, List<Reactor.Action<Reactor.Web.Context,
                                                                        Reactor.Action<Reactor.Web.Context>>> middlewares) {
            this.method      = method;
            this.middlewares = middlewares;
            this.pattern     = pattern;
            this.names       = this.ComputeNames();
            this.regex       = this.ComputeRegex();
        }

        #endregion

        #region Methods

        public void Process(Reactor.Web.Context context) {
            if(this.middlewares.Count == 0) {
                // dead end..
            } else {
                var index = 0;
                Action<Reactor.Web.Context> step = null;
                step = new Action<Reactor.Web.Context>(input => {
                    this.middlewares[index++](input, output => {
                        if (index == this.middlewares.Count) { 
                            // dead end..
                        }
                        else
                            step(output);
                    });
                });
                step(context);
            }
        }

        #endregion

        #region Properties

        public string Method {
            get { return this.method; }
        }

        public string Pattern {
            get { return this.pattern;  }
        }

        #endregion

        #region Internals

        /// <summary>
        /// Computes the key/value pairs from the url.
        /// </summary>
        /// <param name="request">The reactor http server request.</param>
        /// <returns>A dictionary of key value pairs obtained from the request uri.</returns>
        internal Dictionary<string, string> ReadUriParameters(string uri) {
            var dict = new Dictionary<string, string>();
            var path = Reactor.Net.HttpUtility.UrlDecode(uri);
            var match = this.regex.Match(path);
            int index = -1;
            foreach (var group in match.Groups) {
                if (index >= 0) {
                    dict[this.names[index]] = group.ToString();
                } index++;
            } return dict;
        }

        /// <summary>
        /// For the given server request, returns if this route matches the criteria.
        /// </summary>
        /// <param name="request">The reactor http server request.</param>
        /// <returns>True is matched.</returns>
        internal bool Match(Reactor.Http.ServerRequest request) {
            if (string.Equals(request.Method, this.method, StringComparison.InvariantCultureIgnoreCase)) {
                var path = Reactor.Net.HttpUtility.UrlDecode(request.Url.AbsolutePath);
                return this.regex.IsMatch(path);
            } return false;
        }

        #endregion

        #region Privates

        /// <summary>
        /// From the given pattern, build a list of expected keys from the pattern.
        /// </summary>
        /// <returns></returns>
        private List<string> ComputeNames() {
            bool open = false;
            var buf   = string.Empty;
            var names = new List<string>();
            for (int i = 0; i < this.pattern.Length; i++) {
                if (this.pattern[i] == ':') {
                    open = true;
                    continue;
                }
                if (this.pattern[i] == '/') {
                    if (buf.Length > 0) {
                        names.Add(buf);
                    }
                    open = false;
                    buf = string.Empty;
                    continue;
                }
                if (open) {
                    buf += this.pattern[i];
                }
            }

            if (buf.Length > 0) {
                names.Add(buf);
            } return names;
        }

        /// <summary>
        /// Computes the regular expression used to match a request uri.
        /// </summary>
        /// <returns>The regular expression for this route.</returns>
        private Regex ComputeRegex() {
            var expression = this.pattern;
            foreach (var item in this.names) {
                expression = expression.Replace(":" + item, "([^/]*)");
            } return new Regex("^" + expression + "$");
        }

        #endregion
    }

    /// <summary>
    /// Reactor web router.
    /// </summary>
    public class AppRouter {

        #region Fields
        private List<Route> routes;
        private List<Reactor.Action<Reactor.Http.Context,
                     Reactor.Action<Reactor.Http.Context>>> middlewares;
        #endregion

        #region Constructor

        public AppRouter () {
            this.routes      = new List<Reactor.Web.Route>();
            this.middlewares = new List<Reactor.Action<Reactor.Http.Context,
                                        Reactor.Action<Reactor.Http.Context>>>();
        }

        #endregion

        #region Process

        /// <summary>
        /// Router processor. Processes a Reactor http request.
        /// </summary>
        /// <param name="context">The incoming http context.</param>
        /// <param name="next">The next function.</param>
        public void Process(Reactor.Http.Context context, Reactor.Action<Reactor.Http.Context> next) {
            if (this.middlewares.Count == 0) {
                var route = this.routes.Find(n => n.Match(context.Request));
                if (route != null)
                    route.Process(new Reactor.Web.Context(route, context));
                else next(context);
            } else {
                var index = 0;
                Action<Reactor.Http.Context> step = null;
                step = new Action<Reactor.Http.Context>(input => {
                    this.middlewares[index++](input, output => {
                        if (index == this.middlewares.Count) {
                            var route = this.routes.Find(n => n.Match(context.Request));
                            if (route != null)
                                route.Process(new Reactor.Web.Context(route, context));
                            else next(context);
                        } else step(output);
                    });
                }); step(context);
            }
        }

        #endregion

        #region Routing

        /// <summary>
        /// Creates middleware for this router.
        /// </summary>
        /// <param name="middleware">The middleware function.</param>
        /// <returns>This router</returns>
        public Reactor.Web.AppRouter Use(Reactor.Action<Reactor.Http.Context,
                          Reactor.Action<Reactor.Http.Context>> middleware) {
            this.middlewares.Add(middleware);
            return this;
        }

        /// <summary>
        /// Creates a HTTP GET request handler.
        /// </summary>
        /// <param name="pattern">The Uri pattern.</param>
        /// <param name="handler">The http handler</param>
        /// <returns>This router</returns>
        public Reactor.Web.AppRouter Get(string pattern, Reactor.Action<Reactor.Web.Context> handler) {
            var middleware = new List<Reactor.Action<Reactor.Web.Context,
                                      Reactor.Action<Reactor.Web.Context>>>()
            { (context, next) => handler(context) };
            this.routes.Add(new Route(pattern, "GET", middleware));
            return this;
        }

        /// <summary>
        /// Creates a HTTP POST request handler.
        /// </summary>
        /// <param name="pattern">The Uri pattern.</param>
        /// <param name="handler">The http handler</param>
        /// <returns>This router</returns>
        public Reactor.Web.AppRouter Post(string pattern, Reactor.Action<Reactor.Web.Context> handler) {
            var middleware = new List<Reactor.Action<Reactor.Web.Context,
                                      Reactor.Action<Reactor.Web.Context>>>()
            { (context, next) => handler(context) };
            this.routes.Add(new Route(pattern, "POST", middleware));
            return this;
        }

        /// <summary>
        /// Creates a HTTP PUT request handler.
        /// </summary>
        /// <param name="pattern">The Uri pattern.</param>
        /// <param name="handler">The http handler</param>
        /// <returns>This router</returns>
        public Reactor.Web.AppRouter Put(string pattern, Reactor.Action<Reactor.Web.Context> handler) {
            var middleware = new List<Reactor.Action<Reactor.Web.Context,
                                      Reactor.Action<Reactor.Web.Context>>>()
            { (context, next) => handler(context) };
            this.routes.Add(new Route(pattern, "PUT", middleware));
            return this;
        }

        /// <summary>
        /// Creates a HTTP DELETE request handler.
        /// </summary>
        /// <param name="pattern">The Uri pattern.</param>
        /// <param name="handler">The http handler</param>
        /// <returns>This router</returns>
        public Reactor.Web.AppRouter Delete(string pattern, Reactor.Action<Reactor.Web.Context> handler) {
            var middleware = new List<Reactor.Action<Reactor.Web.Context,
                                      Reactor.Action<Reactor.Web.Context>>>()
            { (context, next) => handler(context) };
            this.routes.Add(new Route(pattern, "DELETE", middleware));
            return this;
        }

        /// <summary>
        /// Creates a HTTP OPTIONS request handler.
        /// </summary>
        /// <param name="pattern">The Uri pattern.</param>
        /// <param name="handler">The http handler</param>
        /// <returns>This router</returns>
        public Reactor.Web.AppRouter Options(string pattern, Reactor.Action<Reactor.Web.Context> handler) {
            var middleware = new List<Reactor.Action<Reactor.Web.Context,
                                      Reactor.Action<Reactor.Web.Context>>>()
            { (context, next) => handler(context) };
            this.routes.Add(new Route(pattern, "OPTIONS", middleware));
            return this;
        }

        /// <summary>
        /// Creates a HTTP GET request handler.
        /// </summary>
        /// <param name="pattern">The Uri pattern.</param>
        /// <param name="middleware">Middleware associated with this handler.</param>
        /// <param name="handler">The http handler</param>
        /// <returns>This router</returns>
        public Reactor.Web.AppRouter Get(string pattern, List<Reactor.Action<Reactor.Web.Context,
                                               Reactor.Action<Reactor.Web.Context>>> middleware, 
                                               Reactor.Action<Reactor.Web.Context> handler) {
            middleware.Add((context, next) => handler(context));
            this.routes.Add(new Route(pattern, "GET", middleware));
            return this;
        }

        /// <summary>
        /// Creates a HTTP POST request handler.
        /// </summary>
        /// <param name="pattern">The Uri pattern.</param>
        /// <param name="middleware">Middleware associated with this handler.</param>
        /// <param name="handler">The http handler</param>
        /// <returns>This router</returns>
        public Reactor.Web.AppRouter Post(string pattern, List<Reactor.Action<Reactor.Web.Context,
                                                Reactor.Action<Reactor.Web.Context>>> middleware, 
                                                Reactor.Action<Reactor.Web.Context> handler) {
            middleware.Add((context, next) => handler(context));
            this.routes.Add(new Route(pattern, "POST", middleware));
            return this;
        }

        /// <summary>
        /// Creates a HTTP PUT request handler.
        /// </summary>
        /// <param name="pattern">The Uri pattern.</param>
        /// <param name="middleware">Middleware associated with this handler.</param>
        /// <param name="handler">The http handler</param>
        /// <returns>This router</returns>
        public Reactor.Web.AppRouter Put(string pattern, List<Reactor.Action<Reactor.Web.Context,
                                                Reactor.Action<Reactor.Web.Context>>> middleware, 
                                                Reactor.Action<Reactor.Web.Context> handler) {
            middleware.Add((context, next) => handler(context));
            this.routes.Add(new Route(pattern, "PUT", middleware));
            return this;
        }

        /// <summary>
        /// Creates a HTTP DELETE request handler.
        /// </summary>
        /// <param name="pattern">The Uri pattern.</param>
        /// <param name="middleware">Middleware associated with this handler.</param>
        /// <param name="handler">The http handler</param>
        /// <returns>This router</returns>
        public Reactor.Web.AppRouter Delete(string pattern, List<Reactor.Action<Reactor.Web.Context,
                                                  Reactor.Action<Reactor.Web.Context>>> middleware, 
                                                  Reactor.Action<Reactor.Web.Context> handler) {
            middleware.Add((context, next) => handler(context));
            this.routes.Add(new Route(pattern, "DELETE", middleware));
            return this;
        }

        /// <summary>
        /// Creates a HTTP OPTIONS request handler.
        /// </summary>
        /// <param name="pattern">The Uri pattern.</param>
        /// <param name="middleware">Middleware associated with this handler.</param>
        /// <param name="handler">The http handler</param>
        /// <returns>This router</returns>
        public Reactor.Web.AppRouter Options(string pattern, List<Reactor.Action<Reactor.Web.Context,
                                                   Reactor.Action<Reactor.Web.Context>>> middleware, 
                                                   Reactor.Action<Reactor.Web.Context> handler) {
            middleware.Add((context, next) => handler(context));
            this.routes.Add(new Route(pattern, "OPTIONS", middleware));
            return this;
        }

        #endregion

        #region Statics

        /// <summary>
        /// Creates a new web router.
        /// </summary>
        /// <returns>The web router.</returns>
        public static AppRouter Create() {
            return new AppRouter();
        }

        #endregion

        #region Implicit

        /// <summary>
        /// Implicit cast to middleware.
        /// </summary>
        /// <param name="router"></param>
        public static implicit operator Reactor.Action<Reactor.Http.Context, 
                                        Reactor.Action<Reactor.Http.Context>> (Reactor.Web.AppRouter router) {
            return router.Process;
        }

        #endregion
    }
}
