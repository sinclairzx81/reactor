/*--------------------------------------------------------------------------

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

using Reactor.Http;
using System;

namespace Reactor.Web
{
    public delegate void OnWebContext(WebContext Context);

    public class WebServer
    {
        private Reactor.Http.Server Server { get; set; }

        public WebServer()
        {
            this.Server = Reactor.Http.Server.Create(this.OnContext);
        }

        private void OnContext(HttpContext Context)
        {
            throw new NotImplementedException();
        }

        public void Use(Middleware Middleware)
        {
            throw new NotImplementedException();
        }

        public void Get(string pattern,  OnWebContext OnWebContext)
        {
            throw new NotImplementedException();
        }

        public void Post(string pattern, OnWebContext OnWebContext)
        {
            throw new NotImplementedException();
        }

        public void Put(string pattern, OnWebContext OnWebContext)
        {
            throw new NotImplementedException();
        }

        public void Delete(string pattern, OnWebContext OnWebContext)
        {
            throw new NotImplementedException();
        }

        public void Get(string pattern, Middleware [] Middleware, OnWebContext OnWebContext)
        {
            throw new NotImplementedException();
        }

        public void Post(string pattern, Middleware[] Middleware, OnWebContext OnWebContext)
        {
            throw new NotImplementedException();
        }

        public void Put(string pattern, Middleware[] Middleware, OnWebContext OnWebContext)
        {
            throw new NotImplementedException();
        }

        public void Delete(string pattern, Middleware[] Middleware, OnWebContext OnWebContext)
        {
            throw new NotImplementedException();
        }

        public WebServer Listen(int Port)
        {
            this.Server.Listen(Port);

            return this;
        }

        #region Statics

        public static WebServer Create()
        {
            return new WebServer();
        }

        #endregion
    }
}
