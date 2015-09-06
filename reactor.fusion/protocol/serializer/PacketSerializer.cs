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

namespace Reactor.Fusion.Protocol {

    /// <summary>
    /// Provides packet serialization services.
    /// </summary>
    public class PacketSerializer : IPacketSerializer {
        /// <summary>
        /// Serializers this packet to bytes.
        /// </summary>
        /// <param name="packet">The packet to serialize.</param>
        /// <returns>The serialized packet or null if error.</returns>
        public byte[] Serialize(Packet packet) {
            var buffer = Reactor.Buffer.Create();
            switch (packet.type) {
                case PacketType.Ack:
                    var ack = (Ack)packet;
                    buffer.Write(new byte[] { (byte)ack.type });
                    buffer.Write(ack.ack);
                    break;
                case PacketType.Data:
                    var data = (Data)packet;
                    buffer.Write(new byte[] { (byte)data.type });
                    buffer.Write(data.seq);
                    buffer.Write(data.data);
                    break;
                case PacketType.Fin:
                    var fin = (Fin)packet;
                    buffer.Write(new byte[] { (byte)fin.type });
                    buffer.Write(fin.seq);
                    break;
                case PacketType.Ping:
                    var ping = (Ping)packet;
                    buffer.Write(new byte[] { (byte)ping.type });
                    break;
                case PacketType.Syn:
                    var syn = (Syn)packet;
                    buffer.Write(new byte[] { (byte)syn.type });
                    buffer.Write(syn.seq);
                    break;
                case PacketType.SynAck:
                    var synack = (SynAck)packet;
                    buffer.Write(new byte[] { (byte)synack.type });
                    buffer.Write(synack.seq);
                    buffer.Write(synack.ack);
                    break;
            }
            var ret = buffer.ToArray();
            buffer.Dispose();
            return ret;
        }

        /// <summary>
        /// Deserialized these bytes into a packet.
        /// </summary>
        /// <param name="bytes">The bytes to deserialize.</param>
        /// <param name="packetType">the Output packetType</param>
        /// <returns>A Packet on success or null if error.</returns>
        public Packet Deserialize(byte[] bytes, out PacketType packetType) {
            Packet packet = null;
            packetType = PacketType.Unknown;
            using (var buffer = Reactor.Buffer.Create(bytes)) {
                try {
                    packetType = (PacketType)buffer.ReadByte();
                    switch (packetType) {
                        case PacketType.Ack: 
                            packet = new Ack(buffer.ReadUInt32());
                            break;
                        case PacketType.Data:
                            packet =  new Data(buffer.ReadUInt32(), buffer.Read(buffer.Length));
                            break;
                        case PacketType.Fin:
                            packet =  new Fin(buffer.ReadUInt32());
                            break;
                        case PacketType.Ping:
                            packet =  new Ping();
                            break;
                        case PacketType.Syn:
                            packet =  new Syn(buffer.ReadUInt32());
                            break;
                        case PacketType.SynAck:
                            packet =  new SynAck(buffer.ReadUInt32(), buffer.ReadUInt32());
                            break;
                    }
                }
                catch { packetType = PacketType.Unknown; }
            }
            return packet;
        }
    }
}
