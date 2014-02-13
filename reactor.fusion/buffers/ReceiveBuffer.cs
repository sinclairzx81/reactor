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
    /// The Reactor Fusion ReceiveBuffer. This buffer accepts 
    /// payloads received over the wire. It will handle sequencing
    /// payloads, and keeping track of the sequence number.
    /// </summary>
    internal class ReceiveBuffer
    {
        private List<PayloadSyn> Payloads           { get; set; }

        /// <summary>
        /// The maximum size of this receive buffer.
        /// </summary>
        public ushort            MaxWindowSize      { get; set; }

        /// <summary>
        /// The "next" sequence this receive buffer expects.
        /// </summary>
        public uint              SequenceNumber     { get; private set; }

        /// <summary>
        /// Creates a new Receive Buffer.
        /// </summary>
        /// <param name="SequenceNumber">The starting sequence number.</param>
        /// <param name="MaxWindowSize">The maximum size of the receive buffer.</param>
        public ReceiveBuffer(uint SequenceNumber, ushort MaxWindowSize)
        {
            this.Payloads            = new List<PayloadSyn>();

            this.SequenceNumber = SequenceNumber;

            this.MaxWindowSize      = MaxWindowSize;
        }

        #region Properties

        /// <summary>
        /// Returns the window size of this receive buffer. (Payloads - MaxWindowSize)
        /// </summary>
        public ushort WindowSize
        {
            get
            {
                return (ushort)(this.MaxWindowSize - this.Payloads.Count);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Writes a payload to the buffer. Will ignore the payload if the 
        /// payload sequence number is less than the buffers sequence number, or
        /// if the buffer already has this payload.
        /// </summary>
        /// <param name="payload">The payload to write.</param>
        public void Write(PayloadSyn payload)
        {
            lock (this.Payloads)
            {
                if(this.WindowSize > 0)
                {
                    if (payload.SequenceNumber >= this.SequenceNumber)
                    {
                        foreach (var _packet in this.Payloads)
                        {
                            if (_packet.SequenceNumber == payload.SequenceNumber)
                            {
                                return;
                            }
                        }

                        this.Payloads.Add(payload);

                        this.Payloads.Sort();
                    }
                }
            }
        }
        /// <summary>
        /// Dequeues any data in this buffer. Will only dequeue sequenced
        /// payloads, will push this sequence number up to the next expected
        /// sequence number.
        /// </summary>
        /// <returns>Payload byte data if available. otherwise 0</returns>
        public byte[] Dequeue()
        {
            lock (this.Payloads)
            {
                if (this.Payloads.Count == 0)
                {
                    return new byte[0];
                }

                uint next_number          = this.SequenceNumber;

                var sequenced_payloads    = new List<PayloadSyn>();

                for (int i = 0; i < this.Payloads.Count; i++) {

                    if (this.Payloads[i].SequenceNumber == next_number) {

                        sequenced_payloads.Add(this.Payloads[i]);

                        next_number = next_number + 1;

                        this.Payloads.Remove(this.Payloads[i]);

                        i = i - 1;
                    }
                }

                this.SequenceNumber     = next_number;

                var buffer              = new List<byte>();

                foreach (var packet in sequenced_payloads) {

                    buffer.AddRange(packet.Data);
                }

                return buffer.ToArray();
            }
        }

        #endregion
    }
}
