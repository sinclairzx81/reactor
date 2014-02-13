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
    internal enum MessageType : byte
    {
        Invalid         = 0,

        HandshakeSyn    = 1,

        HandshakeSynAck = 2,

        HandshakeAck    = 3,

        PayloadSyn      = 4,

        PayloadAck      = 5,

        KeepAliveSyn    = 6,

        KeepAliveAck    = 7,
    }

    internal static class Deserializer
    {
        public static object Deserialize(byte[] data, out MessageType messageType)
        {
            try
            {
                messageType = (MessageType)data[0];

                switch (messageType)
                {
                    case MessageType.HandshakeSyn:

                        return new HandshakeSyn(BitConverter.ToUInt32(data, 1));

                    case MessageType.HandshakeSynAck:

                        return new HandshakeSynAck(BitConverter.ToUInt32(data, 1), BitConverter.ToUInt32(data, 5));

                    case MessageType.HandshakeAck:

                        return new HandshakeAck(BitConverter.ToUInt32(data, 1), BitConverter.ToUInt32(data, 5));

                    case MessageType.PayloadSyn:

                        var sequenceNumber = BitConverter.ToUInt32(data, 1);

                        var end            = data[5];

                        var buffer         = new byte[data.Length - 6];

                        System.Buffer.BlockCopy(data, 6, buffer, 0, buffer.Length);

                        return new PayloadSyn(sequenceNumber, buffer, end);

                    case MessageType.PayloadAck:

                        return new PayloadAck(BitConverter.ToUInt32(data, 1), BitConverter.ToUInt16(data, 5) , data[7]);

                    case MessageType.KeepAliveSyn:

                        return new KeepAliveSyn();

                    case MessageType.KeepAliveAck:

                        return new KeepAliveAck();
                }
            }
            catch
            {
                
            }

            messageType = MessageType.Invalid;

            return null;
     
        }
    }
}
