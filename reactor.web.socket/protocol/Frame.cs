/*--------------------------------------------------------------------------

Reactor.Web.Sockets

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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Reactor.Web.Socket.Protocol
{
    internal class Frame : IEnumerable<byte>
    {
        internal static readonly byte[] EmptyUnmaskPingData;

        public Fin         Fin           { get; private set; }

        public Rsv         Rsv1          { get; private set; }

        public Rsv         Rsv2          { get; private set; }

        public Rsv         Rsv3          { get; private set; }

        public Opcode      Opcode        { get; private set; }

        public Mask        Mask          { get; private set; }

        public byte        PayloadLen    { get; private set; }

        public byte[]      ExtPayloadLen { get; private set; }

        public byte[]      MaskingKey    { get; private set; }

        public Payload     Payload       { get; private set; }

        static Frame()
        {
            EmptyUnmaskPingData = CreatePingFrame(Mask.Unmask).ToByteArray();
        }

        private Frame()
        {
        }

        public Frame(Opcode opcode, Payload payload) : this(opcode, Mask.Mask, payload)
        {
        }

        public Frame(Opcode opcode, Mask mask, Payload payload) : this(Fin.Final, opcode, mask, payload)
        {
        }

        public Frame(Fin fin, Opcode opcode, Mask mask, Payload payload) : this(fin, opcode, mask, payload, false)
        {

        }

        public Frame(Fin fin, Opcode opcode, Mask mask, Payload payload, bool compressed)
        {
            this.Fin    = fin;
            
            this.Rsv1   = isData(opcode) && compressed ? Rsv.On : Rsv.Off;
            
            this.Rsv2   = Rsv.Off;
            
            this.Rsv3   = Rsv.Off;
            
            this.Opcode = opcode;
            
            this.Mask   = mask;

            //---------------------------------
            // PayloadLen
            //---------------------------------

            var dataLen    = payload.Length;

            var payloadLen = dataLen < 126 ? (byte)dataLen
                            
                                    : dataLen < 0x010000 ? (byte)126 : (byte)127;

            this.PayloadLen = payloadLen;

            //---------------------------------
            // ExtPayloadLen
            //---------------------------------

            this.ExtPayloadLen = payloadLen < 126

                          ? new byte[] { }

                          : payloadLen == 126

                            ? Util.ToByteArrayInternally(((ushort)dataLen), ByteOrder.Big)

                            : Util.ToByteArrayInternally(dataLen, ByteOrder.Big);

            //---------------------------------
            // MaskingKey 
            //---------------------------------

            var masking = mask == Mask.Mask;

            var maskingKey = masking

                           ? CreateMaskingKey()

                           : new byte[] { };

            this.MaskingKey = maskingKey;

            //---------------------------------
            // PayloadData 
            //---------------------------------

            if (masking)
            {
                payload.Mask(maskingKey);
            }

            this.Payload = payload;
        }

        #region Properties

        internal bool IsBinary
        {
            get
            {
                return this.Opcode == Opcode.BINARY;
            }
        }

        internal bool IsClose
        {
            get
            {
                return this.Opcode == Opcode.CLOSE;
            }
        }

        internal bool IsCompressed
        {
            get
            {
                return this.Rsv1 == Rsv.On;
            }
        }

        internal bool IsContinuation
        {
            get
            {
                return this.Opcode == Opcode.CONT;
            }
        }

        internal bool IsControl
        {
            get
            {
                return this.Opcode == Opcode.CLOSE || this.Opcode == Opcode.PING || this.Opcode == Opcode.PONG;
            }
        }

        internal bool IsData
        {
            get
            {
                return this.Opcode == Opcode.BINARY || this.Opcode == Opcode.TEXT;
            }
        }

        internal bool IsFinal
        {
            get
            {
                return this.Fin == Fin.Final;
            }
        }

        internal bool IsFragmented
        {
            get
            {
                return this.Fin == Fin.More || this.Opcode == Opcode.CONT;
            }
        }

        internal bool IsMasked
        {
            get
            {
                return this.Mask == Mask.Mask;
            }
        }

        internal bool IsPerMessageCompressed
        {
            get
            {
                return (this.Opcode == Opcode.BINARY || this.Opcode == Opcode.TEXT) && this.Rsv1 == Rsv.On;
            }
        }

        internal bool IsPing
        {
            get
            {
                return this.Opcode == Opcode.PING;
            }
        }

        internal bool IsPong
        {
            get
            {
                return this.Opcode == Opcode.PONG;
            }
        }

        internal bool IsText
        {
            get
            {
                return this.Opcode == Opcode.TEXT;
            }
        }

        internal ulong FrameLength
        {
            get
            {
                return 2 + (ulong)(this.ExtPayloadLen.Length + this.MaskingKey.Length) + Payload.Length;
            }
        }

        #endregion

        private static byte[] CreateMaskingKey()
        {
            var key = new byte[4];

            var rand = new Random();

            rand.NextBytes(key);

            return key;
        }

        private static string Dump(Frame frame)
        {
            var len = frame.FrameLength;

            var count = (long)(len / 4);
            
            var rem = (int)(len % 4);

            int countDigit;

            string countFmt;
            
            if (count < 10000)
            {
                countDigit = 4;

                countFmt = "{0,4}";
            }
            else if (count < 0x010000)
            {
                countDigit = 4;

                countFmt = "{0,4:X}";
            }
            else if (count < 0x0100000000)
            {
                countDigit = 8;

                countFmt = "{0,8:X}";
            }
            else
            {
                countDigit = 16;

                countFmt = "{0,16:X}";
            }


            var spFmt = String.Format("{{0,{0}}}", countDigit);
            
            var headerFmt = String.Format(
      @"{0} 01234567 89ABCDEF 01234567 89ABCDEF
{0}+--------+--------+--------+--------+\n", spFmt);

            var footerFmt = String.Format("{0}+--------+--------+--------+--------+", spFmt);

            var buffer = new StringBuilder(64);
            
            Reactor.Func<Reactor.Action<string, string, string, string>> linePrinter = () =>
            {
                long lineCount = 0;

                var lineFmt    = String.Format("{0}|{{1,8}} {{2,8}} {{3,8}} {{4,8}}|\n", countFmt);

                return (arg1, arg2, arg3, arg4) =>
                {
                    buffer.AppendFormat(lineFmt, ++lineCount, arg1, arg2, arg3, arg4);
                };
            };

            var printLine = linePrinter();

            buffer.AppendFormat(headerFmt, String.Empty);

            var frameAsBytes = frame.ToByteArray();
            
            int i, j;
            
            for (i = 0; i <= count; i++)
            {
                j = i * 4;

                if (i < count)
                {
                    printLine(
                      Convert.ToString(frameAsBytes[j], 2).PadLeft(8, '0'),

                      Convert.ToString(frameAsBytes[j + 1], 2).PadLeft(8, '0'),

                      Convert.ToString(frameAsBytes[j + 2], 2).PadLeft(8, '0'),

                      Convert.ToString(frameAsBytes[j + 3], 2).PadLeft(8, '0'));
                }
                else if (rem > 0)
                {
                    printLine(

                      Convert.ToString(frameAsBytes[j], 2).PadLeft(8, '0'),

                      rem >= 2 ? Convert.ToString(frameAsBytes[j + 1], 2).PadLeft(8, '0') : String.Empty,

                      rem == 3 ? Convert.ToString(frameAsBytes[j + 2], 2).PadLeft(8, '0') : String.Empty,

                      String.Empty);
                }
            }

            buffer.AppendFormat(footerFmt, String.Empty);

            return buffer.ToString();
        }



        private static Frame Parse(byte[] header, Stream stream, bool unmask)
        {
            //-----------------------
            // Header
            //-----------------------

            // FIN
            var fin    = (header[0] & 0x80) == 0x80 ? Fin.Final : Fin.More;

            // RSV1
            var rsv1   = (header[0] & 0x40) == 0x40 ? Rsv.On : Rsv.Off;

            // RSV2
            var rsv2   = (header[0] & 0x20) == 0x20 ? Rsv.On : Rsv.Off;

            // RSV3
            var rsv3   = (header[0] & 0x10) == 0x10 ? Rsv.On : Rsv.Off;

            // Opcode
            var opcode = (Opcode)(header[0] & 0x0f);

            // MASK
            var mask   = (header[1] & 0x80) == 0x80 ? Mask.Mask : Mask.Unmask;

            // Payload len
            var payloadLen = (byte)(header[1] & 0x7f);

            // Check if correct frame.
            var incorrect = isControl(opcode) && fin == Fin.More

                          ? "A control frame is fragmented."

                          : !isData(opcode) && rsv1 == Rsv.On

                            ? "A non data frame is compressed."

                            : null;

            if (incorrect != null)
            {
                throw new WebSocketException(CloseStatusCode.IncorrectData, incorrect);
            }

            // Check if consistent frame.
            if (isControl(opcode) && payloadLen > 125)
            {
                throw new WebSocketException(CloseStatusCode.InconsistentData, "The payload data length of a control frame is greater than 125 bytes.");
            }

            var frame = new Frame
            {
                Fin        = fin,

                Rsv1       = rsv1,

                Rsv2       = rsv2,

                Rsv3       = rsv3,

                Opcode     = opcode,

                Mask       = mask,

                PayloadLen = payloadLen
            };

            //-----------------------
            // Extended Payload Length
            //-----------------------

            var extLen = payloadLen < 126

                       ? 0

                       : payloadLen == 126

                         ? 2

                         : 8;

            var extPayloadLen = extLen > 0 ? Util.ReadBytes(stream, extLen) : new byte[] { };

            if (extLen > 0 && extPayloadLen.Length != extLen)
            {
                throw new WebSocketException("The 'Extended Payload Length' of a frame cannot be read from the data source.");
            }

            frame.ExtPayloadLen = extPayloadLen;

            //-----------------------------
            // Masking Key 
            //-----------------------------

            var masked = mask == Mask.Mask;

            var maskingKey = masked ? Util.ReadBytes(stream, 4) : new byte[] { };

            if (masked && maskingKey.Length != 4)
            {
                throw new WebSocketException("The 'Masking Key' of a frame cannot be read from the data source.");
            }

            frame.MaskingKey = maskingKey;
            
            //-----------------------------
            // Payload Data 
            //-----------------------------

            ulong dataLen = payloadLen < 126

                          ? payloadLen

                          : payloadLen == 126

                            ? Util.ToUInt16(extPayloadLen, ByteOrder.Big)

                            : Util.ToUInt64(extPayloadLen, ByteOrder.Big);

            byte[] data = null;

            if (dataLen > 0)
            {
                // Check if allowable payload data length.
                if (payloadLen > 126 && dataLen > Payload.MaxLength)
                {
                    throw new WebSocketException(CloseStatusCode.TooBig, "The 'Payload Data' length is greater than the allowable length.");
                }

                data = payloadLen > 126

                     ? Util.ReadBytes(stream, (long)dataLen, 1024)

                     : Util.ReadBytes(stream, (int)dataLen);

                if (data.LongLength != (long)dataLen)
                {
                    throw new WebSocketException("The 'Payload Data' of a frame cannot be read from the data source.");
                }
            }
            else
            {
                data = new byte[] { };
            }

            var payload = new Payload(data, masked);

            if (masked && unmask)
            {
                payload.Mask(maskingKey);

                frame.Mask = Mask.Unmask;

                //frame.MaskingKey = new byte[] { }; // why do this?
            }

            frame.Payload = payload;

            return frame;
        }

        private static string Print(Frame frame)
        {
            //-------------------------------
            // Opcode 
            //-------------------------------

            var opcode = frame.Opcode.ToString();

            //-------------------------------
            // Payload Len
            //-------------------------------

            var payloadLen = frame.PayloadLen;


            //-------------------------------
            // Extended Payload Len
            //-------------------------------

            var ext = frame.ExtPayloadLen;

            var size = ext.Length;
            
            var extLen = size == 2

                       ? Util.ToUInt16(ext, ByteOrder.Big).ToString()

                       : size == 8

                         ? Util.ToUInt64(ext, ByteOrder.Big).ToString()

                         : String.Empty;

            //-------------------------------
            // Masking Key
            //-------------------------------

            var masked = frame.IsMasked;

            var key = masked

                    ? BitConverter.ToString(frame.MaskingKey)

                    : String.Empty;

            //-------------------------------
            // Payload Data 
            //-------------------------------

            var data = payloadLen == 0

                     ? String.Empty

                     : size > 0

                       ? String.Format("A {0} data with {1} bytes.", opcode.ToLower(), extLen)

                       : masked || frame.IsFragmented || frame.IsBinary || frame.IsClose

                         ? BitConverter.ToString(frame.Payload.ToByteArray())

                         : Encoding.UTF8.GetString(frame.Payload.ApplicationData);

            var format =
      @"                 FIN: {0}
                RSV1: {1}
                RSV2: {2}
                RSV3: {3}
              Opcode: {4}
                MASK: {5}
         Payload Len: {6}
Extended Payload Len: {7}
         Masking Key: {8}
        Payload Data: {9}";

            return String.Format(format, frame.Fin, frame.Rsv1, frame.Rsv2, frame.Rsv3, opcode, frame.Mask, payloadLen, extLen, key, data);
        }

        #region Statics

        public static Frame CreateCloseFrame(Mask mask, Payload payload)
        {
            return new Frame(Opcode.CLOSE, mask, payload);
        }

        public static Frame CreatePongFrame(Mask mask, Payload payload)
        {
            return new Frame(Opcode.PONG, mask, payload);
        }

        public static Frame CreateCloseFrame(Mask mask, byte[] data)
        {
            return new Frame(Opcode.CLOSE, mask, new Payload(data));
        }

        public static Frame CreateCloseFrame(Mask mask, CloseStatusCode code, string reason)
        {
            return new Frame(Opcode.CLOSE, mask, new Payload(Util.Append((ushort)code, reason)));
        }

        public static Frame CreateFrame(Fin fin, Opcode opcode, Mask mask, byte[] data, bool compressed)
        {
            return new Frame(fin, opcode, mask, new Payload(data), compressed);
        }

        public static Frame CreatePingFrame(Mask mask)
        {
            return new Frame(Opcode.PING, mask, new Payload());
        }

        public static Frame CreatePingFrame(Mask mask, byte[] data)
        {
            return new Frame(Opcode.PING, mask, new Payload(data));
        }

        public static Frame Parse(byte[] src, bool unmask)
        {
            using (var stream = new MemoryStream(src))
            {
                return Parse(stream, unmask);
            }
        }

        public static Frame Parse(Stream stream, bool unmask)
        {
            var header = Util.ReadBytes(stream, 2);
            
            if (header.Length != 2)
            {
                throw new WebSocketException("The header part of a frame cannot be read from the data source.");
            }

            return Parse(header, stream, unmask);
        }

        #endregion

        #region Helpers

        public void Print(bool dumped)
        {
            Console.WriteLine(dumped ? Dump(this) : Print(this));
        }

        public string PrintToString(bool dumped)
        {
            return dumped ? Dump(this) : Print(this);
        }

        public byte[] ToByteArray()
        {
            using (var buffer = new MemoryStream())
            {
                int header = (int)Fin;

                header = (header << 1) + (int)Rsv1;

                header = (header << 1) + (int)Rsv2;

                header = (header << 1) + (int)Rsv3;

                header = (header << 4) + (int)Opcode;

                header = (header << 1) + (int)Mask;

                header = (header << 7) + (int)PayloadLen;

                buffer.Write(Util.ToByteArrayInternally((ushort)header, ByteOrder.Big), 0, 2);

                if (this.PayloadLen > 125)
                {
                    buffer.Write(this.ExtPayloadLen, 0, ExtPayloadLen.Length);
                }

                if (this.Mask == Mask.Mask)
                {
                    buffer.Write(this.MaskingKey, 0, MaskingKey.Length);
                }

                if (this.PayloadLen > 0)
                {
                    var data = this.Payload.ToByteArray();

                    if (this.PayloadLen < 127)
                    {
                        buffer.Write(data, 0, data.Length);
                    }
                    else
                    {
                        Util.WriteBytes(buffer, data);
                    }
                }

                buffer.Close();

                return buffer.ToArray();
            }
        }

        private static bool isBinary(Opcode opcode)
        {
            return opcode == Opcode.BINARY;
        }

        private static bool isClose(Opcode opcode)
        {
            return opcode == Opcode.CLOSE;
        }

        private static bool isContinuation(Opcode opcode)
        {
            return opcode == Opcode.CONT;
        }

        private static bool isControl(Opcode opcode)
        {
            return opcode == Opcode.CLOSE || opcode == Opcode.PING || opcode == Opcode.PONG;
        }

        private static bool isData(Opcode opcode)
        {
            return opcode == Opcode.TEXT || opcode == Opcode.BINARY;
        }

        private static bool isFinal(Fin fin)
        {
            return fin == Fin.Final;
        }

        private static bool isMasked(Mask mask)
        {
            return mask == Mask.Mask;
        }

        private static bool isPing(Opcode opcode)
        {
            return opcode == Opcode.PING;
        }

        private static bool isPong(Opcode opcode)
        {
            return opcode == Opcode.PONG;
        }

        private static bool isText(Opcode opcode)
        {
            return opcode == Opcode.TEXT;
        }

        #endregion

        #region IEnumerable<byte>

        public IEnumerator<byte> GetEnumerator()
        {
            foreach (byte b in ToByteArray())
            {
                yield return b;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}