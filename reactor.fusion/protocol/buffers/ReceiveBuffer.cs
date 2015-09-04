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
    /// ReceiveBuffer: Handles inbound packet sequencing.
    /// </summary>
    public class ReceiveBuffer : System.IDisposable {

        #region Fields
        internal class Fields {
            public LinkedList<SequencedPacket> list;
            public System.UInt16               window;
            public Fields(LinkedList<SequencedPacket> list,
                          System.UInt16 window) {
                this.list   = list;
                this.window = window;
            }
        } private Fields fields;
        #endregion

        #region Constructors
        public ReceiveBuffer(System.UInt16 window) {
            this.fields = new Fields(new LinkedList<SequencedPacket>(), window);
        }
        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the Window size on this buffer.
        /// </summary>
        public System.UInt16 Window {
            get { lock(this.fields) return this.fields.window; }
            set { lock(this.fields) this.fields.window = value; }
        }

        /// <summary>
        /// The number of elements in this buffer.
        /// </summary>
        public int Length {
            get { lock (this.fields.list) return this.fields.list.Count; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Writes this packet to the buffer. Internally,
        /// this packet is sequenced by its ordinal value.
        /// </summary>
        /// <param name="packet">The item to write.</param>
        public void Write(SequencedPacket packet) {
            lock (this.fields) {
                if (this.fields.list.Count > this.fields.window) {
                    return;
                }

                if (this.fields.list.Count == 0) {
                    this.fields.list.AddFirst(packet);
                    return;
                }
                var current = this.fields.list.First;
                while (current != null) {
                    var compare = packet.CompareTo(current.Value);
                    if      (compare ==  0) return;
                    else if (compare == -1) {
                        this.fields.list.AddBefore(current, packet);
                        return;
                    }
                    else current = current.Next;
                } this.fields.list.AddLast(packet);
            }
        }

        /// <summary>
        /// Reads the next packet from the buffer, if
        /// empty, will return null.
        /// </summary>
        /// <returns></returns>
        public SequencedPacket Read() {
            lock (this.fields) {
                if (this.fields.list.Count > 0) {
                    var first = this.fields.list.First;
                    this.fields.list.Remove(first);
                    return first.Value;
                }
                return null;
            }
        }

        /// <summary>
        /// Unshifts this packet to the buffer.
        /// </summary>
        /// <param name="packet"></param>
        public void Unshift(SequencedPacket packet) {
            lock (this.fields) {
                if (this.fields.list.Count == 0) {
                    this.fields.list.AddFirst(packet);
                    return;
                }
                var current = this.fields.list.First;
                var compare = packet.CompareTo(current.Value);
                if (compare == -1) {
                    this.fields.list.AddBefore(current, packet);
                }
            }
        }

        #endregion

        #region IDisposable
        private bool disposed = false;
        private void Dispose(bool disposing) {
            lock (this.fields) {
                if (!this.disposed) {
                    this.fields.list.Clear();
                    this.disposed = true;
                }
            }
        }
        public void Dispose() {
            this.Dispose(true);
        }
        ~ReceiveBuffer() {
            this.Dispose(false);
        }
        #endregion
    }
}
