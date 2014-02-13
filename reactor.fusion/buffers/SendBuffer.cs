/*--------------------------------------------------------------------------

The MIT License (MIT)

Copyright (c) 2014 Haydn Paterson (sinclair) <haydn.developer@gmail.com>

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

using System.Collections.Generic;

namespace Reactor.Fusion
{
    /// <summary>
    /// Fusion SendBuffer: Front side buffer for outbound data.
    /// </summary>
    internal class SendBuffer
    {
        private object               Lock           { get; set; }

        private List<PayloadSyn>     Packets        { get; set; }

        private int                  PacketSize     { get; set; }

        private uint                 SequenceNumber { get; set; }

        public SendBuffer(uint SequenceNumber, int PacketSize)
        {
            this.Lock             = new object();

            this.Packets          = new List<PayloadSyn>();

            this.SequenceNumber   = SequenceNumber;

            this.PacketSize       = PacketSize;
        }


        #region Properties

        public int Length 
        {
            get 
            {
                return this.Packets.Count * this.PacketSize;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Writes data into this buffer. 
        /// </summary>
        /// <param name="data">The data to write.</param>
        public void Write(byte [] data)
        {
            lock(this.Lock)
            {
                if (data.Length > 0)
                {
                    var chunks    = data.Length / this.PacketSize;

                    var remainder = data.Length % this.PacketSize;

                    for (var i = 0; i < chunks; i++)
                    {
                        var chunk = new byte[this.PacketSize];

                        System.Buffer.BlockCopy(data, i * this.PacketSize, chunk, 0, this.PacketSize);

                        this.Packets.Add(new PayloadSyn(this.SequenceNumber, chunk, 0));

                        this.SequenceNumber = this.SequenceNumber + 1;
                    }

                    if (remainder > 0)
                    {
                        var chunk = new byte[remainder];

                        System.Buffer.BlockCopy(data, chunks * this.PacketSize, chunk, 0, remainder);

                        this.Packets.Add(new PayloadSyn(this.SequenceNumber, chunk, 0));

                        this.SequenceNumber = this.SequenceNumber + 1;
                    }
                }            
            }
        }

        internal void End()
        {
            this.Packets.Add(new PayloadSyn(this.SequenceNumber, new byte [0], 1));
        }

        /// <summary>
        /// Reads payloads from the buffer. Will always read index from 0.
        /// </summary>
        /// <param name="numberOfPayloads">The number of payloads to read.</param>
        /// <returns>The payloads, or empty if none.</returns>
        public List<PayloadSyn> Read(int numberOfPayloads)
        {
            lock (this.Lock)
            {
                var result = new List<PayloadSyn>();

                for(var i = 0; i < numberOfPayloads; i++) 
                {
                    if(i < this.Packets.Count)
                    {
                        result.Add(this.Packets[i]);
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// Acknowledges payloads. Will remove any payloads with sequence numbers
        /// below the supplied acknowledgement number.
        /// </summary>
        /// <param name="acknowledgementNumber">The acknowlegement number</param>
        public void Acknowledge(uint acknowledgementNumber)
        {
            lock (this.Lock)
            {
                for (int i = 0; i < this.Packets.Count; i++)
                {
                    if(this.Packets[i].SequenceNumber == acknowledgementNumber)
                    {
                        this.Packets.RemoveRange(0, i);

                        return;
                    }
                }

                this.Packets.Clear();
            }
        }

        #endregion


    }
}
