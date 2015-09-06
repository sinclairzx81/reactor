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
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Reactor.Fusion.Protocol {

    /// <summary>
    /// ClientDatagramTransport: A transport used by datagram clients.
    /// </summary>
    public class ClientDatagramTransport : Reactor.Fusion.Protocol.ITransport, System.IDisposable {
        private Reactor.Fusion.Protocol.IPacketSerializer     serializer;
        private Reactor.Event<Reactor.Fusion.Protocol.Packet> onread;
        private System.Threading.Thread                       thread;
        private System.Net.Sockets.Socket                     socket;
        private System.Net.EndPoint                           localEndPoint;
        private System.Net.EndPoint                           remoteEndPoint;
        private System.Byte []                                buffer;
        private System.Boolean                                disposed;

        /// <summary>
        /// Creates a new datagram transport.
        /// </summary>
        /// <param name="serializer">The packet serializer for this transport.</param>
        /// <param name="socket">The socket to layer.</param>
        /// <param name="localEndPoint">The local endpoint for this socket.</param>
        /// <param name="remoteEndPoint">The remote endpoint for this socket.</param>
        public ClientDatagramTransport(Reactor.Fusion.Protocol.IPacketSerializer serializer,
                                      System.Net.EndPoint                        localEndPoint,
                                      System.Net.EndPoint                        remoteEndPoint) {
            this.onread = new Reactor.Event<Reactor.Fusion.Protocol.Packet>();
            this.socket = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            this.socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            this.socket.Bind(localEndPoint);
            this.serializer     = serializer;
            this.localEndPoint  = localEndPoint;
            this.remoteEndPoint = remoteEndPoint;
            this.buffer         = new System.Byte[1400];
            this.disposed       = false;
            this.thread         = new Thread(this.ReadInternal);
            this.thread.Start();
        }

        #region Properties

        /// <summary>
        /// The localEndPoint for this transport.
        /// </summary>
        public System.Net.EndPoint LocalEndPoint {
            get {  return this.localEndPoint; }
        }

        /// <summary>
        /// The remoteEndPoint for this transport.
        /// </summary>
        public System.Net.EndPoint RemoteEndPoint {
            get {  return this.remoteEndPoint; }
        }
        #endregion

        #region ITransport
        
        /// <summary>
        /// Writes this packet to the transport.
        /// </summary>
        /// <param name="packet"></param>
        public void Write(Reactor.Fusion.Protocol.Packet packet) {
            var data = this.serializer.Serialize(packet);
            this.socket.SendTo(data, this.remoteEndPoint);
        }

        /// <summary>
        /// Subscribes this action to the OnRead event.
        /// </summary>
        /// <param name="callback">The callback to receive packets.</param>
        public void OnRead(Reactor.Action<Reactor.Fusion.Protocol.Packet> action) {
            this.onread.On(action);
        }

        /// <summary>
        /// Unsubscribes this action to the OnRead event.
        /// </summary>
        /// <param name="callback">The callback to receive packets.</param>
        public void RemoveRead(Reactor.Action<Reactor.Fusion.Protocol.Packet> action) {
            this.onread.Remove(action);
        }

        #endregion

        #region ReadInternal

        /// <summary>
        /// internally reads from the transport.
        /// </summary>
        private void ReadInternal() {
            while (!this.disposed) {
                try {
                    var remoteEndPoint = new IPEndPoint(IPAddress.Any, 0) as EndPoint;
                    var read = this.socket.ReceiveFrom(this.buffer, ref remoteEndPoint);
                    PacketType packetType;
                    var data = new byte[read];
                    System.Buffer.BlockCopy(this.buffer, 0, data, 0, read);
                    var packet = this.serializer.Deserialize(data, out packetType);
                    if (packetType != PacketType.Unknown) {
                        var endpoint_a = (IPEndPoint)this.remoteEndPoint;
                        var endpoint_b = (IPEndPoint)remoteEndPoint;
                        if(endpoint_a.Address.Equals(endpoint_b.Address) &&
                           endpoint_a.Port.Equals(endpoint_b.Port)) {
                            this.onread.Emit((Packet)packet);
                        }
                    }
                }catch{}
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

        ~ClientDatagramTransport() {
            this.Dispose(false);
        }

        #endregion
    }
}
