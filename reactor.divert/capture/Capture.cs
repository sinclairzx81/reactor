/*--------------------------------------------------------------------------

Reactor.Divert

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
using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;

namespace Reactor.Divert
{
    public class Capture
    {
        public event Reactor.Action<System.Exception>  OnError;

        private const uint                     buffersize = 65536;

        private string                         filter;

        private IntPtr                         handle;

        private Thread                         thread;

        private IntPtr                         readbuffer;

        private IntPtr                         writebuffer;

        private WINDIVERT_ADDRESS              addr;

        private bool                           started;

        private bool                           synchronized;

        private Reactor.Action<Packet, Action<Packet>> callback;

        public Capture(string filter, Reactor.Action<Packet, Reactor.Action<Packet>> callback)
        {
            this.filter       = filter;

            this.handle       = IntPtr.Zero;

            this.started      = false;

            this.synchronized = false;

            this.readbuffer  = Marshal.AllocHGlobal((int)buffersize); // clean these up

            this.writebuffer = Marshal.AllocHGlobal((int)buffersize);

            this.addr        = default(WINDIVERT_ADDRESS);

            this.callback    = callback;

            this.OnError += error => {

                this.started = false;
            };
        }

        public Capture Start ()
        {
            this.handle = WinDivert.WinDivertOpen(this.filter, WINDIVERT_LAYER.WINDIVERT_LAYER_NETWORK, 0, 0);

            if (handle == new IntPtr(-1))
            {              
                var exception = new Win32Exception(Marshal.GetLastWin32Error());

                throw exception;
            }

            this.started      = true;

            this.thread       = new Thread(this.Runtime);

            this.thread.Start();

            return this;
        }

        public Capture Stop  ()
        {
            this.started = false;

            return this;
        }

        private void Runtime()
        {
            this.started = true;

            while (this.started)
            {
                //--------------------------------------------------
                // read packet from kernel
                //--------------------------------------------------

                uint read = 0;

                if (!WinDivert.WinDivertRecv(this.handle, this.readbuffer, buffersize, out addr, out read)) {

                    var exception = new Win32Exception(Marshal.GetLastWin32Error());

                    this.OnError(exception);

                    this.started = false;

                    return;
                }

                //--------------------------------------------------
                // packet headers
                //--------------------------------------------------

                IntPtr ppIpHdr;

                IntPtr ppIpv6Hdr;

                IntPtr ppIcmpHdr;

                IntPtr ppIcmpv6Hdr;

                IntPtr ppTcpHdr;

                IntPtr ppUdpHdr;

                IntPtr ppData;

                uint   pDataLen;

                WinDivert.WinDivertHelperParsePacket(this.readbuffer, read, out ppIpHdr,
                
                                                                            out ppIpv6Hdr,

                                                                            out ppIcmpHdr,

                                                                            out ppIcmpv6Hdr,

                                                                            out ppTcpHdr,

                                                                            out ppUdpHdr,

                                                                            out ppData,

                                                                            out pDataLen);

                //----------------------------------------------------
                // parse packet source and destination (best attempt)
                //----------------------------------------------------

                PacketType type     = PacketType.Unknown;

                IPAddress  src_addr = IPAddress.None;
                
                IPAddress  dst_addr = IPAddress.None;

                int        src_port = 0;

                int        dst_port = 0;

                #region Parser

                if (ppIpHdr != IntPtr.Zero)
                {
                    try
                    {
                        var iphdr = (WINDIVERT_IPHDR)Marshal.PtrToStructure(ppIpHdr, typeof(WINDIVERT_IPHDR));

                        type      = PacketType.IP;

                        src_addr  = new IPAddress(new byte[] { iphdr.SrcAddr0, iphdr.SrcAddr1, iphdr.SrcAddr2, iphdr.SrcAddr3 });

                        dst_addr  = new IPAddress(new byte[] { iphdr.DstAddr0, iphdr.DstAddr1, iphdr.DstAddr2, iphdr.DstAddr3 });
                    }
                    catch
                    {

                    }
                }

                if (ppIpv6Hdr != IntPtr.Zero)
                {
                    try
                    {
                        var ipv6hdr = (PWINDIVERT_IPV6HDR)Marshal.PtrToStructure(ppIpv6Hdr, typeof(PWINDIVERT_IPV6HDR));
                    }
                    catch
                    {

                    }
                }

                if (ppIcmpHdr != IntPtr.Zero)
                {
                    try
                    {
                        var imcphdr = (PWINDIVERT_ICMPHDR)Marshal.PtrToStructure(ppIcmpHdr, typeof(PWINDIVERT_ICMPHDR));
                    }
                    catch
                    {

                    }
                }

                if (ppIcmpv6Hdr != IntPtr.Zero)
                {
                    try
                    {
                        var icmpv6hdr = (PWINDIVERT_ICMPV6HDR)Marshal.PtrToStructure(ppIcmpv6Hdr, typeof(PWINDIVERT_ICMPV6HDR));
                    }
                    catch
                    {

                    }
                }

                if (ppTcpHdr != IntPtr.Zero)
                {
                    try
                    {
                        var tcphdr = (PWINDIVERT_TCPHDR)Marshal.PtrToStructure(ppTcpHdr, typeof(PWINDIVERT_TCPHDR));

                        type       = PacketType.TCP;

                        src_port   = Capture.LittleEndian(tcphdr.SrcPort);

                        dst_port   = Capture.LittleEndian(tcphdr.DstPort);
                    }
                    catch
                    {

                    }
                }

                if (ppUdpHdr != IntPtr.Zero)
                {
                    try
                    {
                        var udphdr = (WINDIVERT_UDPHDR)Marshal.PtrToStructure(ppUdpHdr, typeof(WINDIVERT_UDPHDR));

                        type       = PacketType.UDP;

                        src_port   = Capture.LittleEndian(udphdr.SrcPort);

                        dst_port   = Capture.LittleEndian(udphdr.DstPort);                       
                    }
                    catch
                    {

                    }
                }

                #endregion

                //-------------------------------------------------
                // copy from readbuffer to buffer
                //-------------------------------------------------

                var data = new byte[read];

                Marshal.Copy(this.readbuffer, data, 0, (int)read);

                //-------------------------------------------------
                // process packet
                //-------------------------------------------------

                Reactor.Loop.Post(() =>
                {
                    this.callback(new Packet {

                        Type        = type,

                        Data        = data,

                        Source      = new IPEndPoint(src_addr, src_port),

                        Destination = new IPEndPoint(dst_addr, dst_port)

                    }, packet =>
                    {
                        //-------------------------------------------------
                        // copy packet buffer to writebuffer
                        //-------------------------------------------------

                        Marshal.Copy(packet.Data, 0, this.writebuffer, packet.Data.Length);

                        uint send = 0;

                        if (!WinDivert.WinDivertSend(this.handle, this.writebuffer, (uint)packet.Data.Length, ref addr, out send))
                        {
                            var exception = new Win32Exception(Marshal.GetLastWin32Error());

                            this.OnError(exception);

                            this.started = false;

                            return;
                        }                
                    });
                });
            }

            WinDivert.WinDivertClose(this.handle);
        }

        #region Statics

        public static Capture Create(string filter, Action<Packet, Action<Packet>> callback)
        {
            return new Capture(filter, callback);
        }

        public static Capture Create(Reactor.Divert.PacketType type, Action<Packet, Action<Packet>> callback)
        {
            var filter = "(inbound or outbound)";

            switch(type)
            {
                case PacketType.IP:

                    filter = "(inbound or outbound) and ip";

                    break;

                case PacketType.TCP:

                    filter = "(inbound or outbound) and tcp";

                    break;

                case PacketType.UDP:

                    filter = "(inbound or outbound) and udp";

                    break;
            }

            return new Capture(filter, callback);
        }

        public static Capture Create(Action<Packet, Action<Packet>> callback)
        {
            var filter = "(inbound or outbound)";

            return new Capture(filter, callback);
        }

        #endregion

        #region Endian

        private static System.UInt16 LittleEndian(System.UInt16 input)
        {
            var b = BitConverter.GetBytes(input);

            Array.Reverse(b, 0, b.Length);

            return BitConverter.ToUInt16(b, 0);
        }

        private static System.UInt32 LittleEndian(System.UInt32 input)
        {
            var b = BitConverter.GetBytes(input);

            Array.Reverse(b, 0, b.Length);

            return BitConverter.ToUInt32(b, 0);
        }

        #endregion
    }
}
