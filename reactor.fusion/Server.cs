/*--------------------------------------------------------------------------

Reactor.Fusion

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

using Reactor.Fusion.Protocol;
using System;
using System.Net;

namespace Reactor.Fusion {

    /// <summary>
    /// Reactor Fusion server.
    /// </summary>
    public class Server {

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
        
        private ServerDatagramTransportHost          host;
        private IRandomizer                          randomizer;
        private Reactor.Event<Reactor.Fusion.Socket> onread;
        private Reactor.Event<Exception>             onerror;
        private Reactor.Event                        onend;

        #region Constructor

        /// <summary>
        /// Creates a new TCP Server.
        /// </summary>
        public Server() {
            this.randomizer = new Randomizer();
            this.onread     = Reactor.Event.Create<Reactor.Fusion.Socket>();
            this.onerror    = Reactor.Event.Create<Exception>();
            this.onend      = Reactor.Event.Create();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the local endpoint.
        /// </summary>
        public EndPoint LocalEndPoint {
            get {
                if (this.host != null) {
                    return this.host.LocalEndPoint;
                }
                return null;
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Subscribes this action to the OnSocket event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnRead (Reactor.Action<Reactor.Fusion.Socket> callback) {
            this.onread.On(callback);
        }

        /// <summary>
        /// Unsubscribes this action from the OnSocket event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveRead (Reactor.Action<Reactor.Fusion.Socket> callback) {
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
        public void RemoveError (Reactor.Action<System.Exception> callback) {
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
        public void RemoveEnd (Reactor.Action callback) {
            this.onend.Remove(callback);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Starts this server listening on this endpoint.
        /// </summary>
        /// <param name="localEndpoint">The local endpoint to bind to.</param>
        /// <param name="options">Socket options.</param>
        public Server Listen(IPEndPoint localEndpoint) {
            this.host = new ServerDatagramTransportHost(new PacketSerializer(), localEndpoint);
            this.host.OnTransport(transport => {
                System.UInt32 send_seq = this.randomizer.Next();
                System.UInt32 recv_seq = 0;
                Reactor.Action<Packet> onread = null; onread = packet => {
                    switch (packet.type) {
                        case PacketType.Syn:
                            var syn = (Syn)packet;
                            recv_seq = syn.seq + 1;
                            transport.Write(new SynAck(send_seq, recv_seq));
                            break;
                        case PacketType.Ack:
                            transport.RemoveRead(onread);
                            var ack = (Ack)packet;
                            send_seq     = ack.ack;
                            var sender   = new ProtocolSender  (transport, send_seq, 16);
                            var receiver = new ProtocolReceiver(transport, recv_seq, 16);
                            var socket   = new Reactor.Fusion.Socket(transport, sender, receiver);
                            this.onread.Emit(socket);
                            break;
                    }
                }; transport.OnRead(onread);
            });
            return this;
        }

        /// <summary>
        /// Starts this server on localhost bound to this port.
        /// </summary>
        /// <param name="port">The port to listen on.</param>
        public Server Listen(int port) {
            return this.Listen(new IPEndPoint(IPAddress.Any, port));
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
        public static Server Create(Reactor.Action<Reactor.Fusion.Socket> callback) {
            var server = new Server();
            server.OnRead(callback);
            return server;
        }

        #endregion
    }
}
