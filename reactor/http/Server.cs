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

namespace Reactor.Http {

    /// <summary>
    /// Reactor HTTP Server
    /// </summary>
    public class Server {

        private Reactor.Net.HttpListener             listener;
        private Reactor.Event<Reactor.Http.Context>  onread;
        private Reactor.Event<Exception>             onerror;
        private Reactor.Event                        onend;
        private bool                                 listening;

        #region Constructor

        public Server() {
            this.onread    = Reactor.Event.Create<Reactor.Http.Context>();
            this.onerror   = Reactor.Event.Create<Exception>();
            this.onend     = Reactor.Event.Create();
            this.listening = false;            
        }

        #endregion

        #region Methods

        public Server Listen(int port) {
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
            return this;
        }

        public Server Stop() {
            this.listening = false;
            this.listener.Stop();
            this.listener.Close();
            return this;
        }

        #endregion

        #region Events

        /// <summary>
        /// Subscribes this action to the OnSocket event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnRead (Reactor.Action<Reactor.Http.Context> callback) {
            this.onread.On(callback);
        }

        /// <summary>
        /// Unsubscribes this action from the OnSocket event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveRead (Reactor.Action<Reactor.Http.Context> callback) {
            this.onread.Remove(callback);
        }

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
        /// Reads incoming sockets.
        /// </summary>
        private void _Read() {
            this.Accept().Then(context => {
                try {
                    this.onread.Emit(new Reactor.Http.Context(context));
                    if (this.listening) this._Read();
                    else this._End();
                }
                catch (Exception error) {
                    this._Error(error);
                }
            }).Error(this._Error);
        }

        /// <summary>
        /// Handles errors.
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
            return new Server();
        }

        public static Server Create(Reactor.Action<Reactor.Http.Context> callback) {
            var server = new Reactor.Http.Server();
            server.OnRead(callback);
            return server;
        }

        #endregion
    }
}
