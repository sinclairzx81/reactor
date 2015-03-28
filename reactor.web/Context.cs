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
using System.Collections.Generic;
using System.Security.Principal;

namespace Reactor.Web
{
    public class Context
    {
        public IPrincipal                   User     { get; set; }

        public Reactor.Http.ServerRequest   Request  { get; set; }

        public Reactor.Http.ServerResponse  Response { get; set; }

        public Dictionary<string, string>   Params   { get; set; }

        private Dictionary<string, object>  Items    { get; set; }

        public Context(Reactor.Http.Context context)
        {
            this.User     = context.User;

            this.Request  = context.Request;

            this.Response = context.Response;

            this.Items    = new Dictionary<string, object>();

            this.Params   = new Dictionary<string, string>();
        }

        #region Methods

        public void Set<T>(string name, T item)
        {
            this.Items[name] = item;
        }

        public T Get<T>(string name)
        {
            return (T)this.Items[name];
        }

        #endregion
    }
}