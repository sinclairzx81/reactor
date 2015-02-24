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

namespace Reactor.Fusion.Queues
{
    internal class RecvQueue
    {
        private List<DataSyn> packets        { get; set; }

        private ushort        maxWindowSize  { get; set; }
        
        public uint           SequenceNumber { get; private set; }

        public RecvQueue(uint sequenceNumber, ushort maxWindowSize)
        {
            this.packets       = new List<DataSyn>();

            this.SequenceNumber = sequenceNumber;

            this.maxWindowSize  = maxWindowSize;
        }

        #region Properties

        public ushort WindowSize
        {
            get
            {
                return (ushort)(this.maxWindowSize - this.packets.Count);
            }
        }
        #endregion

        #region Methods

        public void Write(DataSyn payload)
        {
            if (this.WindowSize > 0)
            {
                if (payload.SequenceNumber >= this.SequenceNumber)
                {
                    foreach (var _packet in this.packets)
                    {
                        if (_packet.SequenceNumber == payload.SequenceNumber)
                        {
                            return;
                        }
                    }

                    this.packets.Add(payload);

                    this.packets.Sort();
                }
            }
        }

        /// <summary>
        /// Attempt to dequeue what we can from the receive buffer (basically a read)
        /// </summary>
        public byte[] Dequeue()
        {
            if (this.packets.Count == 0)
            {
                return new byte[0];
            }

            var next_number = this.SequenceNumber;

            var sequenced_payloads = new List<DataSyn>();
                
            for (int i = 0; i < this.packets.Count; i++) {

                if (this.packets[i].SequenceNumber == next_number) {

                    sequenced_payloads.Add(this.packets[i]);

                    next_number = next_number + 1;

                    this.packets.Remove(this.packets[i]);

                    i = i - 1;
                }
            }

            this.SequenceNumber = next_number;

            var buffer = new List<byte>();

            foreach (var packet in sequenced_payloads)
            {
                buffer.AddRange(packet.Data);
            }

            return buffer.ToArray();
        }

        #endregion
    }

}
