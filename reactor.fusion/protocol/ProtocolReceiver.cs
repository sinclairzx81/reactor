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

using Reactor.Fusion.Transport;

namespace Reactor.Fusion.Protocol {

    /// <summary>
    /// ProtocolReceiver: 
    /// </summary>
    public class ProtocolReceiver: System.IDisposable {

        #region Fields
        internal class Fields {
            public ReceiveBuffer                buffer;
            public Reactor.Event<System.Byte[]> ondata;
            public Reactor.Event                onfin;
            public System.UInt32                seq;
            public System.Boolean               reading;
            public Fields(ReceiveBuffer                buffer,
                          Reactor.Event<System.Byte[]> ondata,
                          Reactor.Event                onfin,
                          System.UInt32                seq,
                          System.Boolean               reading) {
                this.buffer     = buffer;
                this.ondata     = ondata;
                this.onfin      = onfin;
                this.seq        = seq;
                this.reading    = reading;
            }
        }
        #endregion

        private ITransport transport;
        private Fields     fields;

        #region Constructors

        /// <summary>
        /// Initializes a new ProtocolReceiver.
        /// </summary>
        /// <param name="seq">The sequence number obtained from a syn / synack packet.</param>
        /// <param name="window">The window size (needs to match the sender)</param>
        /// <param name="transport">The transport in which to send packets.</param>
        public ProtocolReceiver(System.UInt32 seq, System.UInt16 window, ITransport transport) {
            this.fields = new Fields(buffer  : new ReceiveBuffer(window),
                                     ondata  : Reactor.Event.Create<byte[]>(),
                                     onfin   : Reactor.Event.Create(),
                                     seq     : seq,
                                     reading : true);
            this.transport = transport;
            this.transport.Receive(this.Receive);
        }
        
        #endregion

        #region Events

        /// <summary>
        /// Subscribes this action to the OnData event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnData(Reactor.Action<System.Byte[]> callback) {
            lock (this.fields) {
                this.fields.ondata.On(callback);
            }
        }
        
        /// <summary>
        /// Subscribes this action to the OnFin event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnFin(Reactor.Action callback) {
            lock (this.fields) {
                this.fields.onfin.On(callback);
            }
        }

        #endregion

        #region Receive
        
        /// <summary>
        /// Receives a packet.
        /// </summary>
        /// <param name="packet"></param>
        public void Receive(Packet packet) {
            lock (this.fields) {
                if(packet.type == PacketType.Data || 
                   packet.type == PacketType.Fin) {
                    var sequential_packet = packet as SequencedPacket;
                    if (sequential_packet.seq < this.fields.seq) {
                        this.transport.Send(new Ack ( 
                            ack: this.fields.seq 
                            ));
                        return;
                    }

                    this.fields.buffer.Write(sequential_packet);
                    while (this.fields.buffer.Length > 0) {
                        var current = this.fields.buffer.Read();
                        if (current.seq != this.fields.seq) {
                            this.fields.buffer.Unshift(current);
                            break;
                        }
                        switch (current.type) {
                            case  PacketType.Data:
                                this.fields.seq += 1;
                                var data = (Data)current;
                                this.fields.ondata.Emit(data.data);
                                break;
                            case  PacketType.Fin:
                                this.fields.seq += 1;
                                var fin = (Fin)current;
                                this.fields.onfin.Emit();
                                this.fields.reading = false;
                                break;
                        }
                    }
                    this.transport.Send(new Ack( 
                        ack: this.fields.seq 
                        ));
                }
            }
        }

        #endregion

        #region IDisposable

        private bool disposed = false;
        private void Dispose(bool disposing) {
            lock (this.fields) {
                if (!this.disposed) {
                    this.fields.buffer.Dispose();
                    this.fields.ondata.Dispose();
                    this.fields.onfin.Dispose();
                    this.disposed = true;
                }
            }
        }
        public void Dispose() {
            this.Dispose(true);
        }
        ~ProtocolReceiver() {
            this.Dispose(false);
        }
        #endregion
    }
}
