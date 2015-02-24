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

using System;
using System.Collections.Generic;

namespace Reactor.Fusion.Protocol
{
    internal class DataAck : Packet
    {
        public ushort WindowSize            { get; set; }
        
        public DataAck(uint acknowledgementnumber, ushort windowsize) 
            
            : base(0, acknowledgementnumber)
        {
            this.WindowSize            = windowsize;
        }

        public override byte[] Serialize()
        {
            var result = new List<byte>();

            result.Add((byte)PacketType.PayloadAck);

            result.AddRange(BitConverter.GetBytes(this.AcknowledgementNumber));

            result.AddRange(BitConverter.GetBytes(this.WindowSize));

            return result.ToArray();
        }
    }
}
