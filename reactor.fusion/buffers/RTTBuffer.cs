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

using System;

namespace Reactor.Fusion
{
    /// <summary>
    /// Reactor Fusion RTTBuffer. Measures round trip times for packets.
    /// </summary>
    internal class RTTBuffer
    {
        #region RTTSample

        /// <summary>
        /// The internal rtt sample.
        /// </summary>
        internal class RTTSample
        {
            public uint     SequenceNumber { get; set; }

            public DateTime TimeStamp      { get; set; }

            public RTTSample(uint SequenceNumber)
            {
                this.SequenceNumber = SequenceNumber;

                this.TimeStamp = DateTime.Now;
            }
        }

        #endregion

        private object           Lock          { get; set; }

        private RTTSample []     Samples       { get; set; }

        private double           Total         { get; set; }

        private int              Count         { get; set; }

        private int              Index         { get; set; }

        public RTTBuffer(int SamplerSize)
        {
            this.Lock        = new object();

            this.Samples     = new RTTSample[SamplerSize];

            this.Index       = 0;

            this.Total       = 0;

            this.Count       = 0;
        }

        #region Properties
        
        public double Average
        {
            get
            {
                return (this.Count > 0) ? (this.Total / this.Count) : 1000;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Writes this sequence number to the buffer.
        /// </summary>
        /// <param name="SequenceNumber">The sequence number to write.</param>
        public void Write(uint SequenceNumber)
        {
            lock (this.Lock)
            {
                this.Samples[this.Index] = new RTTSample(SequenceNumber);

                this.Index = ((this.Index + 1) % this.Samples.Length);
            }
        }

        /// <summary>
        /// Acknowledges this sequence number.
        /// </summary>
        /// <param name="SequenceNumber">The sequence number to acknowledge</param>
        public void Acknowledge(uint SequenceNumber)
        {
            lock(this.Lock)
            {
                for (int i = 0; i < this.Samples.Length; i++) {

                    if(this.Samples[i] != null) {

                        if (this.Samples[i].SequenceNumber == SequenceNumber) {

                            var delta   = DateTime.Now - this.Samples[i].TimeStamp;

                            this.Total += delta.TotalMilliseconds;

                            this.Count += 1;

                            this.Samples[i] = null;
                        }
                    }
                }
            }
        }

        #endregion
    }
}
