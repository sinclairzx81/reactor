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
    internal class SendQueue
    {
        private List<Packet>     packets;

        private int              packetsize;

        private uint             sequenceNumber;

        public SendQueue(uint sequenceNumber, int packetSize)
        {
            this.packets        = new List<Packet>();

            this.sequenceNumber = sequenceNumber;

            this.packetsize     = packetSize;
        }

        #region Properties

        public int Length
        {
            get
            {
                return this.packets.Count * this.packetsize;
            }
        }

        #endregion

        #region Methods

        public void Write(byte[] data)
        {
            if (data.Length > 0)
            {
                var chunks = data.Length / this.packetsize;

                var remainder = data.Length % this.packetsize;

                for (var i = 0; i < chunks; i++)
                {
                    var chunk = new byte[this.packetsize];

                    System.Buffer.BlockCopy(data, i * this.packetsize, chunk, 0, this.packetsize);

                    this.packets.Add(new DataSyn(this.sequenceNumber, chunk));

                    this.sequenceNumber = this.sequenceNumber + 1;
                }

                if (remainder > 0)
                {
                    var chunk = new byte[remainder];

                    System.Buffer.BlockCopy(data, chunks * this.packetsize, chunk, 0, remainder);

                    this.packets.Add(new DataSyn(this.sequenceNumber, chunk));

                    this.sequenceNumber = this.sequenceNumber + 1;
                }
            }
        }

        internal void End()
        {
            this.packets.Add(new FinSyn(this.sequenceNumber, 0));

            this.sequenceNumber = this.sequenceNumber + 1; // shouldn't need this 
        }

        /// <summary>
        /// Reads payloads from the buffer. Will always read index from 0.
        /// </summary>
        /// <param name="numberOfPayloads">The number of payloads to read.</param>
        /// <returns>The payloads, or empty if none.</returns>
        public List<Packet> Read(int numberOfPayloads)
        {
            var result = new List<Packet>();

            for (var i = 0; i < numberOfPayloads; i++)
            {
                if (i < this.packets.Count)
                {
                    result.Add(this.packets[i]);
                }
            }

            return result;
        }

        public void Acknowledge(uint acknowledgementNumber)
        {
            for (int i = 0; i < this.packets.Count; i++)
            {
                if (this.packets[i].SequenceNumber == acknowledgementNumber)
                {
                    //System.Console.WriteLine("found");

                    this.packets.RemoveRange(0, i);

                    return;
                }

               
            }

            this.packets.Clear();
        }

        #endregion
    }
}
