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
namespace Reactor.Fusion.Protocol {

    public enum PacketType: byte {
        Unknown = 0,
        Syn     = 1,
        SynAck  = 2,
        Ack     = 3,
        Data    = 4,
        Ping    = 5,
        PingAck = 6,
        Fin     = 7
    }

    public class Packet {
        public PacketType type;
        public Packet() {
            this.type = PacketType.Unknown;
        }
    }

    public class SequencedPacket: Packet, IComparable {
        public System.UInt32 seq;
        public SequencedPacket(System.UInt32 seq): base() {
            this.seq = seq;
        }
        public int CompareTo(object other) {
            if (other is SequencedPacket) {
                var packet = other as SequencedPacket;
                return this.seq.CompareTo(packet.seq);
            }
            return 0;
        }
    }
}