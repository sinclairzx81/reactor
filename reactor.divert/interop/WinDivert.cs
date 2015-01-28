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

using System.Runtime.InteropServices;

namespace Reactor.Divert
{
    //----------------------------------------------------------------------------
    // https://github.com/basil00/Divert/blob/master/include/windivert.h#L58
    //----------------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal struct WINDIVERT_ADDRESS
    {
        internal System.UInt32 IfIdx;

        internal System.UInt32 SubIfIdx;

        internal System.Byte Direction;
    }

    //----------------------------------------------------------------------------
    // https://github.com/basil00/Divert/blob/master/include/windivert.h#L71
    //----------------------------------------------------------------------------

    internal enum WINDIVERT_LAYER
    {
        WINDIVERT_LAYER_NETWORK = 0,

        WINDIVERT_LAYER_NETWORK_FORWARD = 1
    }

    //----------------------------------------------------------------------------
    // https://github.com/basil00/Divert/blob/master/include/windivert.h#L87
    //----------------------------------------------------------------------------

    internal enum PWINDIVERT_PARAM
    {
        WINDIVERT_PARAM_QUEUE_LEN = 0,

        WINDIVERT_PARAM_QUEUE_TIME = 1
    }

    //----------------------------------------------------------------------------
    // https://github.com/basil00/Divert/blob/master/include/windivert.h#L178
    //----------------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal struct WINDIVERT_IPHDR
    {
        internal System.Byte HdrLength_Version; // 4:4

        internal System.Byte TOS;

        internal System.UInt16 Length;

        internal System.UInt16 Id;

        internal System.UInt16 FragOff0;

        internal System.Byte TTL;

        internal System.Byte Protocol;

        internal System.UInt16 Checksum;

        internal System.Byte SrcAddr0;

        internal System.Byte SrcAddr1;

        internal System.Byte SrcAddr2;

        internal System.Byte SrcAddr3;

        internal System.Byte DstAddr0;

        internal System.Byte DstAddr1;

        internal System.Byte DstAddr2;

        internal System.Byte DstAddr3;
    }

    //----------------------------------------------------------------------------
    // https://github.com/basil00/Divert/blob/master/include/windivert.h#L231
    //----------------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal struct PWINDIVERT_IPV6HDR
    {
        internal System.Byte TrafficClass0_Version;    // 4:4

        internal System.Byte FlowLabel0_TrafficClass1; // 4:4

        internal System.UInt16 FlowLabel1;

        internal System.UInt16 Length;

        internal System.Byte NextHdr;

        internal System.Byte HopLimit;

        internal System.Byte SrcAddr0;

        internal System.Byte SrcAddr1;

        internal System.Byte SrcAddr2;

        internal System.Byte SrcAddr3;

        internal System.Byte DstAddr0;

        internal System.Byte DstAddr1;

        internal System.Byte DstAddr2;

        internal System.Byte DstAddr3;
    }

    //----------------------------------------------------------------------------
    // https://github.com/basil00/Divert/blob/master/include/windivert.h#L265
    //----------------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal struct PWINDIVERT_ICMPHDR
    {
        internal System.Byte Type;

        internal System.Byte Code;

        internal System.UInt16 Checksum;

        internal System.UInt16 Body;
    }

    //----------------------------------------------------------------------------
    // https://github.com/basil00/Divert/blob/master/include/windivert.h#L273
    //----------------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal struct PWINDIVERT_ICMPV6HDR
    {
        internal System.Byte Type;

        internal System.Byte Code;

        internal System.UInt16 Checksum;

        internal System.UInt32 Body;
    }

    //----------------------------------------------------------------------------
    // https://github.com/basil00/Divert/blob/master/include/windivert.h#L281
    //----------------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal struct PWINDIVERT_TCPHDR
    {
        internal System.UInt16 SrcPort;

        internal System.UInt16 DstPort;

        internal System.UInt32 SeqNum;

        internal System.UInt32 AckNum;

        //internal System.UInt16 Reserved1; // 4

        //internal System.UInt16 HdrLength; // 4

        internal System.Byte Reserved1_HdrLength;

        //internal System.UInt16 Fin; // 1

        //internal System.UInt16 Syn; // 1

        //internal System.UInt16 Rst; // 1

        //internal System.UInt16 Psh; // 1

        //internal System.UInt16 Ack; // 1

        //internal System.UInt16 Urg; // 1 

        //internal System.UInt16 Reserved2; // 2

        internal System.Byte F_S_R_P_A_U_R2;

        internal System.UInt16 Window;

        internal System.UInt16 Checksum;

        internal System.UInt16 UrgPtr;
    }

    //----------------------------------------------------------------------------
    // https://github.com/basil00/Divert/blob/master/include/windivert.h#L301
    //----------------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal struct WINDIVERT_UDPHDR
    {
        internal System.UInt16 SrcPort;

        internal System.UInt16 DstPort;

        internal System.UInt16 Length;

        internal System.UInt16 Checksum;
    }


    internal static class WinDivert
    {


        //----------------------------------------------------------------------------
        // https://github.com/basil00/Divert/blob/master/include/windivert.h#L99
        //----------------------------------------------------------------------------

        [DllImport("WinDivert.dll", EntryPoint = "WinDivertOpen",
                                    SetLastError = true,
                                    CharSet = CharSet.Auto,
                                    ExactSpelling = true,
                                    CallingConvention = CallingConvention.Cdecl)]

        internal static extern System.IntPtr WinDivertOpen([In] [MarshalAs(UnmanagedType.LPStr)] System.String filter,
                                                         [In]  WINDIVERT_LAYER layer,
                                                         [In]  System.Int16 priority,
                                                         [In]  System.UInt64 flags);

        //----------------------------------------------------------------------------
        // https://github.com/basil00/Divert/blob/master/include/windivert.h#L108
        //----------------------------------------------------------------------------

        [DllImport("WinDivert.dll", EntryPoint = "DivertRecv",
                                             SetLastError = true,
                                             CharSet = CharSet.Unicode,
                                             ExactSpelling = true,
                                             CallingConvention = CallingConvention.Cdecl)]

        [return: MarshalAsAttribute(UnmanagedType.Bool)]
        internal static extern bool WinDivertRecv([In]      System.IntPtr handle,
                                                [Out]     System.IntPtr pPacket,
                                                [In]      System.UInt32 packetLen,
                                                [Out] out WINDIVERT_ADDRESS pAddr,
                                                [Out] out System.UInt32 recvLen);

        //----------------------------------------------------------------------------
        // https://github.com/basil00/Divert/blob/master/include/windivert.h#L130
        //----------------------------------------------------------------------------

        [DllImport("WinDivert.dll", EntryPoint = "WinDivertSend",
                                    SetLastError = true,
                                    CharSet = CharSet.Auto,
                                    ExactSpelling = true,
                                    CallingConvention = CallingConvention.Cdecl)]

        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool WinDivertSend([In]      System.IntPtr handle,
                                                [In]      System.IntPtr pPacket,
                                                [In]      System.UInt32 packetLen,
                                                [In]  ref WINDIVERT_ADDRESS pAddr,
                                                [Out] out System.UInt32 sendLen);

        //----------------------------------------------------------------------------
        // https://github.com/basil00/Divert/blob/master/include/windivert.h#L152
        //----------------------------------------------------------------------------

        [DllImport("WinDivert.dll", EntryPoint = "WinDivertClose",
                                    SetLastError = true,
                                    CharSet = CharSet.Auto,
                                    ExactSpelling = true,
                                    CallingConvention = CallingConvention.Cdecl)]

        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool WinDivertClose([In] System.IntPtr handle);


        //----------------------------------------------------------------------------
        // https://github.com/basil00/Divert/blob/master/include/windivert.h#L321
        ////----------------------------------------------------------------------------

        [DllImport("WinDivert.dll", EntryPoint = "WinDivertHelperParsePacket",
                                    SetLastError = true,
                                    CharSet = CharSet.Auto,
                                    ExactSpelling = true,
                                    CallingConvention = CallingConvention.Cdecl)]

        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool WinDivertHelperParsePacket([In]  System.IntPtr pPacket,
                                                             [In]  System.UInt32 packetLen,
                                                             [Out] out System.IntPtr ppIpHdr,
                                                             [Out] out System.IntPtr ppIpv6Hdr,
                                                             [Out] out System.IntPtr ppIcmpHdr,
                                                             [Out] out System.IntPtr ppIcmpv6Hdr,
                                                             [Out] out System.IntPtr ppTcpHdr,
                                                             [Out] out System.IntPtr ppUdpHdr,
                                                             [Out] out System.IntPtr ppData,
                                                             [Out] out System.UInt32 pDataLen);
    }
}