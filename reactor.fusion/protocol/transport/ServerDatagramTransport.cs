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

namespace Reactor.Fusion.Protocol {

    /// <summary>
    /// ServerDatagramTransport: The server datagram packet.
    /// </summary>
    public class ServerDatagramTransport: ITransport, System.IDisposable {
        private Reactor.Event<Reactor.Fusion.Protocol.Packet> onread;
        private ServerDatagramTransportHost                host;
        private System.Net.EndPoint                        localEndPoint;
        private System.Net.EndPoint                        remoteEndPoint;
        private System.Boolean                             disposed;

        /// <summary>
        /// Initializes a new ServerDatagramTransport.
        /// </summary>
        /// <param name="host">The ServerDatagramTransport host.</param>
        /// <param name="localEndPoint">The localEndPoint.</param>
        /// <param name="remoteEndPoint">The remoteEndPoint.</param>
        public ServerDatagramTransport(ServerDatagramTransportHost      host,
                                       System.Net.EndPoint              localEndPoint,
                                       System.Net.EndPoint              remoteEndPoint) {
            this.onread         = Reactor.Event.Create<Packet>();
            this.host           = host;
            this.localEndPoint  = localEndPoint;
            this.remoteEndPoint = remoteEndPoint;
            this.disposed       = false;
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
            var data = this.host.Serializer.Serialize(packet);
            this.host.Socket.SendTo(data, this.remoteEndPoint);
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

        #region Methods

        /// <summary>
        /// Accepts an inbound packet from a host.
        /// </summary>
        /// <param name="packet">The packet to accept.</param>
        public void Accept(Reactor.Fusion.Protocol.Packet packet) {
            this.onread.Emit(packet);
        }

        #endregion

        #region IDisposable

        private void Dispose(bool disposing) {
            if (!this.disposed) {
                this.disposed = true;
                this.host.RemoveTransport(this);
            }
        }

        public void Dispose() {
            this.Dispose(true);
        }

        ~ServerDatagramTransport() {
            this.Dispose(false);
        }

        #endregion
    }
}
