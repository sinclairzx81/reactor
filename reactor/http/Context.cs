/*--------------------------------------------------------------------------

Reactor

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

using System.Net;
using System.Security.Principal;

namespace Reactor.Http
{
    public class HttpContext
    {
        private Reactor.Net.HttpListenerContext httpListenerContext  { get; set; }

        public IPrincipal                   User                     { get; set; }

        public ServerConnection             Connection               { get; set; }

        public ServerRequest                Request                  { get; set; }

        public ServerResponse               Response                 { get; set; }

        internal HttpContext(Reactor.Net.HttpListenerContext HttpListenerContext)
        {
            this.httpListenerContext    = HttpListenerContext;

            this.User                   = this.httpListenerContext.User;

            this.Connection             = new ServerConnection (this, this.httpListenerContext.Connection);

            this.Request                = new ServerRequest    (this, this.httpListenerContext.Request);

            this.Response               = new ServerResponse   (this, this.httpListenerContext.Response);
        }
    }
}
