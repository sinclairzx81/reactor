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
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Reactor.Fusion.Protocol {

    /// <summary>
    /// ServerDatagramTransportHost: All server sockets are routed via a
    /// single udp socket. The responsibility of a host is route
    /// inbound/outbound packets based on a TCB. In Tao, the TCB is
    /// managed by way of the local / remote endpoints on the transport
    /// itself.
    /// </summary>
    public class ServerDatagramTransportHost : System.IDisposable {
        private List<Reactor.Fusion.Protocol.ServerDatagramTransport>          transports;
        private Reactor.Event<Reactor.Fusion.Protocol.ServerDatagramTransport> ontransport;
        private Reactor.Fusion.Protocol.IPacketSerializer                       serializer;
        private System.Threading.Thread                                      thread;
        private System.Net.Sockets.Socket                                    socket;
        private System.Net.EndPoint                                          localEndPoint;
        private System.Byte []                                               buffer;
        private System.Boolean                                               disposed;

        /// <summary>
        /// Creates a new datagram transport.
        /// </summary>
        /// <param name="serializer">The packet serializer for this transport.</param>
        /// <param name="socket">The socket to layer.</param>
        /// <param name="localEndPoint">The local endpoint for this socket.</param>
        /// <param name="remoteEndPoint">The remote endpoint for this socket.</param>
        public ServerDatagramTransportHost(Reactor.Fusion.Protocol.IPacketSerializer serializer,
                                      System.Net.EndPoint                   localEndPoint) {
            this.socket = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            this.socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            this.socket.Bind(localEndPoint);
            this.ontransport    = Reactor.Event.Create<ServerDatagramTransport>();
            this.transports     = new List<ServerDatagramTransport>();
            this.serializer     = serializer;
            this.localEndPoint  = localEndPoint;
            this.buffer         = new System.Byte[1400];
            this.disposed       = false;
            this.thread         = new Thread(this.ReadInternal);
            this.thread.Start();
        }

        #region Properties

        /// <summary>
        /// Gets the socket for this host.
        /// </summary>
        public System.Net.Sockets.Socket Socket {
            get {  return this.socket; }
        }

        /// <summary>
        /// Gets the packet serializer for this host.
        /// </summary>
        public IPacketSerializer Serializer {
            get {  return this.serializer; }
        }

        /// <summary>
        /// Gets the local endpoint for this host.
        /// </summary>
        public System.Net.EndPoint LocalEndPoint {
            get {  return this.localEndPoint; }
        }

        #endregion

        #region Events

        /// <summary>
        /// 
        /// </summary>
        /// <param name="callback"></param>
        public void OnTransport(Reactor.Action<ServerDatagramTransport> callback) {
            this.ontransport.On(callback);
        }

        #endregion

        #region Internal

        /// <summary>
        /// Called from the ServerDatagramTransport on Dispose to remove this transport.
        /// </summary>
        /// <param name="transport"></param>
        internal void RemoveTransport(Reactor.Fusion.Protocol.ServerDatagramTransport transport) {
            this.RemoveTransport(transport);
        }

        #endregion

        #region ReadInternal

        private void AcceptPacket(Packet packet, EndPoint remoteEndPoint) {
            // enumerate transports and accept on match.
            foreach (var transport in this.transports) {
                var endpoint_a = (IPEndPoint)transport.RemoteEndPoint;
                var endpoint_b = (IPEndPoint)remoteEndPoint;
                if(endpoint_a.Address.Equals(endpoint_b.Address) &&
                   endpoint_a.Port.Equals(endpoint_b.Port)) {
                    transport.Accept(packet);
                    return;
                }
            }
            // if no transport is found, it may be a new connection.
            if(packet.type == PacketType.Syn) {
                var transport = new ServerDatagramTransport(this, this.localEndPoint, remoteEndPoint);
                this.transports.Add(transport);
                this.ontransport.Emit(transport);
                transport.Accept(packet);
            }
        }

        /// <summary>
        /// Reads data from the socket, forwards to local transport.
        /// </summary>
        private void ReadInternal() {
            var remoteEndPoint = new IPEndPoint(IPAddress.Any, 0) as EndPoint;
            while (!this.disposed) {
                try {
                    int read = this.socket.ReceiveFrom(this.buffer, ref remoteEndPoint);
                    PacketType packetType;
                    var data = new byte[read];
                    System.Buffer.BlockCopy(this.buffer, 0, data, 0, read);
                    var packet = this.serializer.Deserialize(data, out packetType);
                    if (packetType != PacketType.Unknown) {
                        this.AcceptPacket(packet, remoteEndPoint);
                    }
                }
                catch{}
            }
        }

        #endregion

        #region IDisposable

        private void Dispose(bool disposing) {
            if (!this.disposed) {
                this.disposed = true;
                this.socket.Close();
            }
        }

        public void Dispose() {
            this.Dispose(true);
        }

        ~ServerDatagramTransportHost() {
            this.Dispose(false);
        }

        #endregion
    }
}
