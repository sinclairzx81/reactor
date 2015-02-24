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

namespace Reactor.Fusion.Protocol
{
    internal enum PacketType : byte
    {
        Invalid      = 0,
        
        Syn          = 1,
        
        SynAck       = 2,
        
        Ack          = 3,
        
        PayloadSyn   = 4,
        
        PayloadAck   = 5,
        
        FinSyn       = 6,
        
        FinAck       = 7
    }

    internal static class Parser
    {
        public static object Deserialize(byte[] data, out PacketType packetType)
        {
            try
            {
                packetType = (PacketType)data[0];

                switch (packetType)
                {
                    case PacketType.Syn:

                        return new Syn(BitConverter.ToUInt32(data, 1));
                    
                    case PacketType.SynAck:

                        return new SynAck(BitConverter.ToUInt32(data, 1), BitConverter.ToUInt32(data, 5));
                    
                    case PacketType.Ack:

                        return new Ack(BitConverter.ToUInt32(data, 1), BitConverter.ToUInt32(data, 5));
                    
                    case PacketType.PayloadSyn:

                        var sequenceNumber = BitConverter.ToUInt32(data, 1);

                        var buffer = new byte[data.Length - 5];
                        
                        System.Buffer.BlockCopy(data, 5, buffer, 0, buffer.Length);
                        
                        return new DataSyn(sequenceNumber, buffer);

                    case PacketType.PayloadAck:

                        return new DataAck(BitConverter.ToUInt32(data, 1), BitConverter.ToUInt16(data, 5));

                    case PacketType.FinSyn:

                        return new FinSyn(0, 0);

                    case PacketType.FinAck:

                        return new FinAck(0, 0);
                }
            }
            catch
            {

            }

            packetType = PacketType.Invalid;

            return null;
        }
    }
}
