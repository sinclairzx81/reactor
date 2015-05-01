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
using System.Net.Sockets;

namespace Reactor.Tcp {

    /// <summary>
    /// Reactor TCP server.
    /// </summary>
    public class Server : IDisposable {

        #region State

        /// <summary>
        /// Readable state.
        /// </summary>
        internal enum State {
            /// <summary>
            /// A state indicating a paused state.
            /// </summary>
            Paused,
            /// <summary>
            /// A state indicating a reading state.
            /// </summary>
            Reading,
            /// <summary>
            /// A state indicating a resume state.
            /// </summary>
            Resumed,
            /// <summary>
            /// A state indicating a ended state.
            /// </summary>
            Ended
        }

        #endregion

        private System.Net.Sockets.TcpListener            listener;
        private Reactor.Async.Event<Reactor.Tcp.Socket>   onread;
        private Reactor.Async.Event<Exception>            onerror;
        private Reactor.Async.Event                       onend;
        private bool                                      listening;

        #region Constructor

        /// <summary>
        /// Creates a new TCP Server.
        /// </summary>
        public Server() {
            this.onread    = Reactor.Async.Event.Create<Reactor.Tcp.Socket>();
            this.onerror   = Reactor.Async.Event.Create<Exception>();
            this.onend     = Reactor.Async.Event.Create();
            this.listening = false;
        }

        #endregion

        #region Events

        /// <summary>
        /// Subscribes this action to the OnSocket event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnRead (Reactor.Action<Reactor.Tcp.Socket> callback) {
            this.onread.On(callback);
        }

        /// <summary>
        /// Unsubscribes this action from the OnSocket event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveRead (Reactor.Action<Reactor.Tcp.Socket> callback) {
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

        #region Methods

        /// <summary>
        /// Starts this server listening on this port.
        /// </summary>
        /// <param name="port">The port to listen on.</param>
        public void Listen(int port) {
            if (!this.listening) {
                try {
                    this.listening = true;
                    this.listener  = new TcpListener(IPAddress.Any, port);
                    this.listener.ExclusiveAddressUse = false;
                    this.listener.Start();
                    this._Read();
                }
                catch (Exception error) {
                    this._Error(error);
                }
            }
        }

        #endregion

        #region Internals

        /// <summary>
        /// Accepts a socket from this listener.
        /// </summary>
        /// <returns></returns>
        private Reactor.Async.Future<System.Net.Sockets.Socket> Accept () {
            return new Reactor.Async.Future<System.Net.Sockets.Socket>((resolve, reject) => {
                try {
                    this.listener.BeginAcceptSocket(result => {
                        Loop.Post(() => {
                            try {
                                var socket = listener.EndAcceptSocket(result);
                                resolve(socket);
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
            this.Accept()
                .Then(socket => {
                    this.onread.Emit(new Reactor.Tcp.Socket(socket));
                })
                .Then(() => {
                    if (this.listening) this._Read();
                    else this._End();
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
                this.listening = false;
            }
            catch { }
            this.onend.Emit();
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Disposes of this TCP server.
        /// </summary>
        public void Dispose() {
            this._End();
        }
        #endregion

        #region Statics

        /// <summary>
        /// Creates a new TCP server.
        /// </summary>
        /// <returns></returns>
        public static Server Create() {
            return new Server();
        }

        /// <summary>
        /// Creates a new TCP server.
        /// </summary>
        /// <param name="callback">A callback to receive incoming sockets.</param>
        /// <returns></returns>
        public static Server Create(Reactor.Action<Reactor.Tcp.Socket> callback) {
            var server = new Server();
            server.OnRead(callback);
            return server;
        }

        #endregion
    }
}
