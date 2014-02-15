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

using System;
using System.Net;

namespace Reactor.Http
{
    public class Server
    {
        private HttpListener        HttpListener           { get; set; }

        private Action<HttpContext> OnContext              { get; set; }

        public  Action<Exception>   OnError                { get; set; }

        public Server(Action<HttpContext> OnContext)
        {
            this.OnContext = OnContext;
        }

        public Server Listen(int Port)
        {
            if (!System.Net.HttpListener.IsSupported)
            {
                System.Console.WriteLine("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");

                return this;
            }

            try
            {
                this.HttpListener = new System.Net.HttpListener();

                this.HttpListener.Prefixes.Add(string.Format("http://*:{0}/", Port));

                this.HttpListener.Start();
               
                this.GetContext();
            }
            catch(Exception exception)
            {
                if(exception is HttpListenerException)
                {
                    if(exception.Message.ToLower() == "access is denied")
                    {
                        string username = System.Security.Principal.WindowsIdentity.GetCurrent().Name;

                        string message  = string.Format("Access is denied. Run \"netsh http add urlacl url=http://127.0.0.1:{0}/ user={1}\" to grant access to this user.", Port, username);
                        
                        if(this.OnError != null) {

                            Loop.Post(() => {

                                this.OnError(new HttpListenerException(0, message));
                            });
                        }
                    }
                }
                else
                {
                    if(this.OnError != null)
                    {
                        this.OnError(exception);
                    }
                }
            }

            return this;
        }

        #region GetContext

        private void GetContext()
        {
            this.HttpListener.BeginGetContext((Result) =>
            {
                try
                {
                    var listenerContext = this.HttpListener.EndGetContext(Result);

                    var context = new HttpContext(listenerContext);

                    Loop.Post(() =>
                    {
                        this.OnContext(context);

                        this.GetContext();
                    });
                }
                catch (Exception exception)
                {
                    Loop.Post(() =>
                    {
                        if (this.OnError != null)
                        {
                            this.OnError(exception);
                        }
                    });
                }

            }, null);
         
        }

        #endregion

        #region Statics

        public static Server Create(Action<HttpContext> OnContext)
        {
            return new Server(OnContext);
        }

        #endregion
    }
}
