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

namespace Reactor.Fusion.Protocol {
	
    /// <summary>
    /// SendBuffer: Handles outbound packet queuing and transmission buffering.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class SendBuffer : System.IDisposable {

        #region Internals
        internal class PendingItem {
            public Reactor.Deferred deferred;
            public SequencedPacket  packet;
        }
        internal class TransmitItem {
            public Reactor.Deferred deferred;
            public SequencedPacket  packet;
            public System.DateTime  timestamp;
            public System.Boolean   inflight;
        }
        internal class Fields {
            public Queue<PendingItem>     pending;
            public List<TransmitItem>     transmit;
            public System.UInt16          window;
            public System.Int32           timeout;
            public Fields(Queue<PendingItem> pending,
                          List<TransmitItem> transmit,
                          System.UInt16      window,
                          System.Int32       timeout) {
                this.pending  = pending;
                this.transmit = transmit;
                this.window   = window;
                this.timeout  = timeout;
            }
        } private Fields fields;
        #endregion

        #region Constructors
        public SendBuffer(System.UInt16 window, System.Int32 timeout) {
            this.fields = new Fields(pending  : new Queue<PendingItem>(),
                                     transmit : new List<TransmitItem>(),
                                     window   : window,
                                     timeout  : timeout);
        }
        #endregion

        #region Methods

        /// <summary>
        /// Writes a new packet to the buffer.
        /// </summary>
        /// <param name="ordinal"></param>
        /// <param name="value"></param>
        public Reactor.Future Write(SequencedPacket packet) {
            lock (this.fields) {
                var deferred = new Reactor.Deferred();
                this.fields.pending.Enqueue(new PendingItem{
                    deferred = deferred,
                    packet   = packet
                });
                return deferred.Future;
            }
        }

        /// <summary>
        /// Reads the next packet from the buffer if available.
        /// </summary>
        /// <returns></returns>
        public Packet Read() {
            lock (this.fields) {
                // ensure the transmit buffer is maxed
                while (this.fields.transmit.Count < this.fields.window) {
                    if (this.fields.pending.Count == 0) break;
                    var pending = this.fields.pending.Dequeue();
                    this.fields.transmit.Add(new TransmitItem {
                        deferred  = pending.deferred,
                        packet    = pending.packet,
                        timestamp = System.DateTime.Now,
                        inflight  = false
                    });
                }
                // timeout any packets in transmit buffer.
                foreach (var item in this.fields.transmit) {
                    var delta = System.DateTime.Now - item.timestamp;
                    if (delta.TotalMilliseconds > this.fields.timeout) {
                        item.inflight = false;
                    }
                }
                // select first and return...
                foreach (var item in this.fields.transmit) {
                    if (!item.inflight) {
                        item.timestamp = System.DateTime.Now;
                        item.inflight  = true;
                        return item.packet;
                    }
                }
                return null;
            }
        }
        
        /// <summary>
        /// acknowledges a packet in the transmit buffer.
        /// </summary>
        /// <param name="ordinal"></param>
        /// <returns>The number of packets acknowleged.</returns>
        public System.Int32 Ack(System.UInt32 seq) {
            lock (this.fields) {
                return this.fields.transmit.RemoveAll(item => {
                    var completed = item.packet.seq < seq;
                    if(completed)
                        item.deferred.Resolve();
                    return completed;
                });
            }
        }
        #endregion

        #region IDisposable
        private bool disposed = false;
        private void Dispose(bool disposing) {
            lock (this.fields) {
                if (!this.disposed) {
                    this.fields.pending.Clear();
                    this.fields.transmit.Clear();
                    this.disposed = true;
                }
            }
        }
        public void Dispose() {
            this.Dispose(true);
        }
        ~SendBuffer() {
            this.Dispose(false);
        }
        #endregion
    }
}
