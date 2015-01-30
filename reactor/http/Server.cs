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
using System.Net;

namespace Reactor.Http
{
    public class Server
    {
        private Reactor.Net.HttpListener httplistener;

        public Action<HttpContext> OnContext { get; set; }

        public Action<Exception>   OnError   { get; set; }

        public Server()
        {
            
        }

        public Server(Action<HttpContext> OnContext)
        {
            this.OnContext = OnContext;
        }

        public Server Listen(int port, Reactor.Action<Exception> callback)
        {
            try {

                this.httplistener = new Reactor.Net.HttpListener();

                this.httplistener.Prefixes.Add(string.Format("http://*:{0}/", port));

                this.httplistener.Start();
                
                this.GetContext();

                callback(null);
            }
            catch(Exception exception) {

                if(exception is HttpListenerException) {

                    callback(exception);
                }
                else {

                    callback(exception);
                }
            }

            return this;
        }

        public Server Listen(int port)
        {
            return this.Listen(port, (exception) => {

                if(exception != null) {

                    if (this.OnError != null) {

                        this.OnError(exception);
                    }
                }
            });
        }

        public Server Stop()
        {
            if (this.httplistener != null)
            {
                this.httplistener.Stop();
            }
            return this;
        }

        #region GetContext

        private void GetContext()
        {
            IO.GetContext(this.httplistener, (exception, context) =>
            {
                if(exception != null)
                {
                    if(this.OnError != null)
                    {
                        this.OnError(exception);
                    }

                    return;
                }

                if(this.OnContext != null)
                {
                    this.OnContext(new HttpContext(context));   
                }

                this.GetContext();
            });
        }

        #endregion

        #region Statics

        public static Server Create()
        {
            return new Server();
        }

        public static Server Create(Action<HttpContext> OnContext)
        {
            return new Server(OnContext);
        }

        #endregion
    }
}
