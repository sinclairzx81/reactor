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
using System.Net;

namespace Reactor.Http
{

    /// <summary>
    /// Reactor HTTP Server. Provides a asynchronous interface over a System.Net.HttpListener.
    /// </summary>
    /// <example><![CDATA[
    /// 
    ///     // the following will create a http server
    ///     // listening on port 5000 on localhost.
    /// 
    ///     Reactor.Http.Server.Create(context => {
    ///         context.Response.Write("hello world");
    ///         context.Response.End();
    ///     }).Listen(5000);
    /// ]]>
    /// </example>    
    /// <example><![CDATA[
    /// 
    ///     // the reactor http server allows for application
    ///     // middleware to intercept and optionally handle
    ///     // incoming http requests. The following will intercept
    ///     // all incoming requests and log the URI to the console.
    ///     
    ///     Reactor.Http.Server.Create(context => {  // second.
    ///         context.Response.Write("hello world");
    ///         context.Response.End();
    ///     })
    ///     .Use((context, next) => { // first.
    ///         Console.WriteLine(context.Request.Url);
    ///         next(context);
    ///     });
    ///     .Listen(5000);
    /// 
    ///     // note the order in which the callbacks are executed, middleware
    ///     // is 'always' executed in order before the main request handler. Another
    ///     // approach is to omit the default request handler and use
    ///     // only middleware.
    /// 
    ///     Reactor.Http.Server.Create()
    ///        .Use((context, next) => { // first.
    ///             Console.WriteLine(context.Request.Url);
    ///             next(context);
    ///         })
    ///         .Use((context, _) => { // second.
    ///             context.Response.Write("hello world");
    ///             context.Response.End();
    ///         })
    ///         .Listen(5000);
    /// ]]>
    /// </example>     
    public class Server {
        
        #region Fields

        private Reactor.Net.HttpListener             listener;
        private Reactor.Event<System.Exception>      onerror;
        private Reactor.Event                        onend;
        private bool                                 listening;
        private Reactor.Event<Reactor.Http.Context>  handler;
        private List<Reactor.Action<Reactor.Http.Context, 
                     Reactor.Action<Reactor.Http.Context>>> middlewares;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new reactor http server.
        /// </summary>
        public Server() {
            this.handler     = Reactor.Event.Create<Reactor.Http.Context>(false);
            this.onerror     = Reactor.Event.Create<Exception>();
            this.onend       = Reactor.Event.Create();
            this.listening   = false;
            this.middlewares = new List<Reactor.Action<Reactor.Http.Context, 
                                        Reactor.Action<Reactor.Http.Context>>>();           
        }

        #endregion

        #region Methods

        /// <summary>
        /// Applies a middleware function to the request handler.
        /// </summary>
        /// <param name="middleware">The middleware function to apply.</param>
        /// <returns>This http server.</returns>
        public Server Use(Reactor.Action<Reactor.Http.Context, Reactor.Action<Reactor.Http.Context>> middleware) {
            this.middlewares.Add(middleware);
            return this;
        }

        /// <summary>
        /// Starts this server listening on the given port.
        /// </summary>
        /// <param name="port">The port to listen on.</param>
        /// <returns>The server.</returns>
        public Server Listen(int port) {
            if(!this.listening) {
                try {
                    this.listener = new Reactor.Net.HttpListener();
                    this.listener.IgnoreWriteExceptions = true;
                    this.listener.Prefixes.Add(string.Format("http://*:{0}/", port));
                    this.listener.Start();
                    this.listening = true;
                    this._Read();
                }
                catch(Exception error) {
                   Loop.Post(() => this._Error(error));
                }
            }
            return this;
        }

        /// <summary>
        /// Stops this server listening.
        /// </summary>
        /// <returns>This server.</returns>
        public Server Stop() {
            if(this.listening) {
                this.listening = false;
                this.listener.Stop();
                this.listener.Close();
            }
            return this;
        }

        #endregion

        #region Events

        /// <summary>
        /// Subscribes this action to the OnError event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnError (Reactor.Action<System.Exception> callback) {
            this.onerror.On(callback);
        }

        /// <summary>
        /// Unsubscribes this action from the OnError event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveError(Reactor.Action<System.Exception> callback) {
            this.onerror.Remove(callback);
        }

        /// <summary>
        /// Subscribes this action to the OnEnd event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnEnd (Reactor.Action callback) {
            this.onend.On(callback);
        }

        /// <summary>
        /// Unsubscribes this action from the OnEnd event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveEnd  (Reactor.Action callback) {
            this.onend.Remove(callback);
        }

        #endregion

        #region Internals

        /// <summary>
        /// Accepts a http listener context.
        /// </summary>
        /// <returns></returns>
        private Reactor.Future<Reactor.Net.HttpListenerContext> Accept () {
            return new Reactor.Future<Reactor.Net.HttpListenerContext>((resolve, reject) => {
                try {
                    this.listener.BeginGetContext(result => {
                        Loop.Post(() => {
                            try {
                                var context = this.listener.EndGetContext(result);
                                resolve(context);
                            }
                            catch(Exception error) {
                                reject(error);
                            }
                        });
                    }, null);
                }
                catch(Exception error) {
                    reject(error);
                }
            });
        }
        
        #endregion

        #region Machine

        /// <summary>
        /// Processes the incoming request and attached middleware.
        /// </summary>
        /// <param name="context">The http context to process.</param>
        private void _Process(Reactor.Http.Context context) {
            if (this.middlewares.Count == 0) {
                this.handler.Emit(context);
            }
            else {
                var index = 0;
                Action<Reactor.Http.Context> step = null;
                step = new Action<Reactor.Http.Context>(input => {
                    this.middlewares[index++](input, output => {
                        if (index == this.middlewares.Count) {
                            this.handler.Emit(context);
                        } else {
                            step(output);
                        }
                    });
                }); step(context);
            }
        }

        /// <summary>
        /// Accepts a incoming http request.
        /// </summary>
        private void _Read() {
            this.Accept().Then(context => {
                try {
                    this._Process(new Reactor.Http.Context(context));
                    if (this.listening) this._Read();
                    else this._End();
                }
                catch (Exception error) {
                    this._Error(error);
                }
            }).Catch(this._Error);
        }

        /// <summary>
        /// Emits errors and ends the server.
        /// </summary>
        /// <param name="error"></param>
        private void _Error(Exception error) {
            this.onerror.Emit(error);
            this._End();
        }

        /// <summary>
        /// Ends the listener.
        /// </summary>
        private void _End() {
            try {
                this.listener.Stop();
                this.listener.Close();
                this.listening = false;
            }
            catch { }
            this.onend.Emit();
        }

        #endregion

        #region Statics

        public static Server Create() {
            var server = new Reactor.Http.Server();
            return server;
        }

        public static Server Create(Reactor.Action<Reactor.Http.Context> onrequest) {
            var server = new Reactor.Http.Server();
            server.handler.On(onrequest);
            return server;
        }

        #endregion
    }
}
