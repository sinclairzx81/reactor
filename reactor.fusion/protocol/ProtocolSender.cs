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

namespace Reactor.Fusion.Protocol {
    
    /// <summary>
    /// DataSender: Sends sequenced data packets to a transport.
    /// </summary>
    public class ProtocolSender : System.IDisposable {

        #region Fields
        internal class Fields {
            public SendBuffer buffer;
            public System.UInt32 seq;
            public Fields(SendBuffer buffer,
                          System.UInt32 seq) {
                this.buffer = buffer;
                this.seq    = seq;
            }
        }
        #endregion

        private Reactor.Interval sender;
        private ITransport       transport;
        private Fields           fields;

        #region Contructor

        /// <summary>
        /// Initializes a new ProtocolSender.
        /// </summary>
        /// <param name="seq">the initial sequence number (randomized from the handshake)</param>
        /// <param name="window">the sender window size (needs to match the receiver)</param>
        /// <param name="timeout">the timeout in which unacknowledged packets should be resent.</param>
        /// <param name="transport">(shared) the transport in which to send packets.</param>
        public ProtocolSender(ITransport    transport,
                              System.UInt32 isn,
                              System.UInt16 window) {
            this.fields = new Fields(
                    seq    : isn,
                    buffer : new SendBuffer(
                        window : window, 
                        timeout: 20
                        ));
            this.transport = transport;
            this.transport.OnRead(this.Receive);
            this.sender = Reactor.Interval.Create(this.Send);
        }
        #endregion

        #region Methods

        /// <summary>
        /// Sets the seq value for this sender.
        /// </summary>
        /// <param name="seq"></param>
        public void Seq(System.UInt32 seq) {
            lock (this.fields) {
                this.fields.seq = seq;
            }
        }

        /// <summary>
        /// Sends a data packet to the transport.
        /// </summary>
        /// <param name="data"></param>
        /// <returns>A future </returns>
        public Reactor.Future<Packet> Data(System.Byte[] data) {
            lock (this.fields) {
                var future = this.fields.buffer.Write(new Data (
                    seq : this.fields.seq,
                    data: data
                    ));
                this.fields.seq += 1;
                return future;
            }
        }

        /// <summary>
        /// Sends a fin packet to the transport.
        /// </summary>
        /// <returns>A future to signal once this end has been acknowledged.</returns>
        public Reactor.Future<Packet> Fin() {
            lock (this.fields) {
                var future = this.fields.buffer.Write(new Fin (
                        seq : this.fields.seq
                        ));
                this.fields.seq += 1;
                return future;
            }
        }

        #endregion

        #region Transport

        /// <summary>
        /// Writes packets to the transport.
        /// </summary>
        private void Send() {
            lock (this.fields) {
                var current = this.fields.buffer.Read();
                while (current != null) {
                    this.transport.Write(current);
                    current = this.fields.buffer.Read();
                }
            }
        }

        /// <summary>
        /// Receives packets from the transport.
        /// </summary>
        /// <param name="packet"></param>
        private void Receive(Packet packet) {
            lock (this.fields) {
                switch (packet.type) {
                    case PacketType.Ack:
                        var ack = (Ack)packet;
                        this.fields.buffer.Ack(ack.ack);
                        break;
                }
            }
        }

        #endregion

        #region IDisposable

        private bool disposed = false;
        private void Dispose(bool disposing) {
            lock (this.fields) {
                if (!this.disposed) {
                    this.sender.Clear();
                    this.fields.buffer.Dispose();
                    this.disposed = true;
                }
            }
        }
        public void Dispose() {
            this.Dispose(true);
        }
        ~ProtocolSender() {
            this.Dispose(false);
        }
        #endregion
    }
}
