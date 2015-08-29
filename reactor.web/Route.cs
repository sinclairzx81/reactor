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

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Reactor.Web
{
    public class Route
    {
        private string                        Method  { get; set; }

        private string                        Pattern { get; set; }

        private List<Reactor.Web.Middleware>  middleware;

        private Action<Context>               handler;

        private List<string>                  names;

        private Regex                         regex;

        public Route(string pattern, string method, Action<Context> handler): this(pattern, method, new Reactor.Web.Middleware[0], handler)
        {

        }

        public Route(string pattern, string method, Reactor.Web.Middleware[] middleware, Action<Context> handler)
        {
            this.Method     = method;

            this.middleware = new List<Reactor.Web.Middleware>(middleware);

            this.handler    = handler;

            this.Pattern    = pattern;

            this.names      = this.ComputeNames();

            this.regex      = this.ComputeRegex();
        }


        #region Publics

        public void Invoke(Reactor.Web.Context context)
        {
            Reactor.Web.MiddlewareProcessor.Process(context, this.middleware, () => this.handler(context));
        }

        #endregion

        #region Internals

        //--------------------------------------------
        // matches this route.
        //--------------------------------------------

        internal bool Match(Reactor.Http.ServerRequest request) {

            if (string.Equals(request.Method, this.Method, StringComparison.InvariantCultureIgnoreCase)) {

                var path = Reactor.Net.HttpUtility.UrlDecode(request.Url.AbsolutePath);

                var match = this.regex.IsMatch(path);

                return match;
            }

            return false;
        }

        //--------------------------------------------
        // computes the params for this route.
        //--------------------------------------------

        internal Dictionary<string, string> ComputeParams(Reactor.Http.ServerRequest request) {

            var dict = new Dictionary<string, string>();

            var path = Reactor.Net.HttpUtility.UrlDecode(request.Url.AbsolutePath);

            var match = this.regex.Match(path);

            int index = -1;

            foreach (var group in match.Groups) {

                if (index >= 0) {

                    dict[this.names[index]] = group.ToString();
                }

                index++;
            }

            return dict;
        }

        #endregion

        #region Privates

        //----------------------------------------
        // computes the names from a pattern.
        //----------------------------------------

        private List<string> ComputeNames()
        {
            bool open = false;

            var buf = string.Empty;

            var names = new List<string>();

            for (int i = 0; i < this.Pattern.Length; i++)
            {
                if (this.Pattern[i] == ':')
                {
                    open = true;

                    continue;
                }

                if (this.Pattern[i] == '/')
                {
                    if (buf.Length > 0)
                    {
                        names.Add(buf);
                    }

                    open = false;

                    buf = string.Empty;

                    continue;
                }

                if (open)
                {
                    buf += this.Pattern[i];
                }
            }

            if (buf.Length > 0)
            {
                names.Add(buf);
            }

            return names;
        }

        //--------------------------------------------------
        // computes the regular expression from the names
        //--------------------------------------------------

        private Regex ComputeRegex()
        {
            var expression = this.Pattern;

            foreach (var item in this.names)
            {
                expression = expression.Replace(":" + item, "([^/]*)");
            }

            return new Regex("^" + expression + "$");
        }

        #endregion
    }
}