/*--------------------------------------------------------------------------

Reactor.Web.Sockets

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
using System.IO;

namespace Reactor.Web.Sockets.Protocol
{
    internal static class Util
    {
        public static T[] SubArray<T>(T[] array, int start, int length)
        {
            if (array == null || array.Length == 0)
            {
                return new T[0];
            }
            if (start < 0 || length <= 0)
            {
                return new T[0];
            }

            if (start + length > array.Length)
            {
                return new T[0];
            }

            if (start == 0 && array.Length == length)
            {
                return array;
            }

            T[] result = new T[length];

            Array.Copy(array, start, result, 0, length);

            return result;            
        }

        internal static T[] Reverse<T>(T[] array)
        {
            var len = array.Length;

            T[] result = new T[len];

            var end = len - 1;

            for (var i = 0; i <= end; i++)
            {
                result[i] = array[end - i];
            }

            return result;
        }

        private static bool IsHostOrder(ByteOrder order)
        {
            return !(BitConverter.IsLittleEndian ^ (order == ByteOrder.Little));
        }

        public static byte[] ToHostOrder(byte[] src, ByteOrder srcOrder)
        {
            if (src == null)
            {
                throw new ArgumentNullException("src");
            }



            return src.Length > 1 && !Util.IsHostOrder(srcOrder) ? Util.Reverse(src) : src;
        }        

        public static byte[] ToByteArrayInternally(ushort value, ByteOrder order)
        {
            var bytes = BitConverter.GetBytes(value);
            
            if (!IsHostOrder(order))
            {
                Array.Reverse(bytes);
            }

            return bytes;
        }

        public static byte[] ToByteArrayInternally(ulong value, ByteOrder order)
        {
            var bytes = BitConverter.GetBytes(value);
            
            if (!IsHostOrder(order))
            {
                Array.Reverse(bytes);
            }

            return bytes;
        }

        public static string GetMessage(CloseStatusCode code)
        {
            return code == CloseStatusCode.ProtocolError

                   ? "A WebSocket protocol error has occurred."

                   : code == CloseStatusCode.IncorrectData

                     ? "An incorrect data has been received."

                     : code == CloseStatusCode.Abnormal

                       ? "An exception has occurred."

                       : code == CloseStatusCode.InconsistentData

                         ? "An inconsistent data has been received."

                         : code == CloseStatusCode.PolicyViolation

                           ? "A policy violation has occurred."

                           : code == CloseStatusCode.TooBig

                             ? "A too big data has been received."

                             : code == CloseStatusCode.IgnoreExtension

                               ? "WebSocket client did not receive expected extension(s)."

                               : code == CloseStatusCode.ServerError

                                 ? "WebSocket server got an internal error."

                                 : code == CloseStatusCode.TlsHandshakeFailure

                                   ? "An error has occurred while handshaking."

                                   : String.Empty;        
        }

        public static byte[] ReadBytes(Stream stream, int length)
        {
            return Util.readBytes(stream, new byte[length], 0, length);
        }

        public static byte[] ReadBytes(Stream stream, long length, int bufferLength)
        {
            using (var result = new MemoryStream())
            {
                var count = length / bufferLength;

                var rem = (int)(length % bufferLength);

                var buffer = new byte[bufferLength];
                
                var end = false;
                
                for (long i = 0; i < count; i++)
                {
                    if (!Util.readBytes(stream, buffer, 0, bufferLength, result))
                    {
                        end = true;

                        break;
                    }
                }

                if (!end && rem > 0)
                {
                    Util.readBytes(stream, new byte[rem], 0, rem, result);
                }

                result.Close();

                return result.ToArray();
            }
        }

        private static byte[] readBytes(Stream stream, byte[] buffer, int offset, int length)
        {
            var len = stream.Read(buffer, offset, length);
            
            if (len < 1)
            {
                return Util.SubArray(buffer, 0, offset);
            }

            var tmp = 0;

            while (len < length)
            {
                tmp = stream.Read(buffer, offset + len, length - len);
                
                if (tmp < 1)
                {
                    break;
                }

                len += tmp;
            }

            return len < length ? Util.SubArray(buffer, 0, offset + len) : buffer;
        }

        private static bool readBytes(Stream stream, byte[] buffer, int offset, int length, Stream dest)
        {
            var bytes = Util.readBytes(stream, buffer, offset, length);

            var len = bytes.Length;

            dest.Write(bytes, 0, len);

            return len == offset + length;
        }

        public static void WriteBytes(Stream stream, byte[] value)
        {
            using (var src = new MemoryStream(value))
            {
                Util.CopyTo(src, stream);
            }
        }

        internal static void CopyTo(Stream src, Stream dest)
        {
            Util.CopyTo(src, dest, false);
        }

        internal static void CopyTo(Stream src, Stream dest, bool setDefaultPosition)
        {
            var readLen = 0;
            
            var bufferLen = 256;
            
            var buffer = new byte[bufferLen];
            
            while ((readLen = src.Read(buffer, 0, bufferLen)) > 0)
            {
                dest.Write(buffer, 0, readLen);
            }

            if (setDefaultPosition)
            {
                dest.Position = 0;
            }
        }

        internal static ushort ToUInt16(byte[] src, ByteOrder srcOrder)
        {
            return BitConverter.ToUInt16(Util.ToHostOrder(src, srcOrder), 0);
        }

        internal static ulong ToUInt64(byte[] src, ByteOrder srcOrder)
        {
            return BitConverter.ToUInt64(Util.ToHostOrder(src, srcOrder), 0);
        }

        internal static void ReadBytesAsync(Stream stream, int length, Reactor.Action<byte[]> completed, Reactor.Action<Exception> error)
        {
            var buffer = new byte[length];

            stream.BeginRead(buffer, 0, length, result => {

                  try
                  {
                      var len = stream.EndRead(result);
                      
                      var bytes = len < 1
                                ? new byte[0]
                                : len < length
                                  ? Util.readBytes(stream, buffer, len, length - len)
                                  : buffer;

                      if (completed != null)
                      {
                          completed(bytes);
                      }
                  }
                  catch (Exception ex)
                  {
                      {
                          if (error != null)
                          {
                              error(ex);
                          }
                      }
                  }
              }, null);
        }

        internal static byte[] Append(ushort code, string reason)
        {
            using (var buffer = new MemoryStream())
            {
                var tmp = Util.ToByteArrayInternally(code, ByteOrder.Big);

                buffer.Write(tmp, 0, 2);

                if (reason != null && reason.Length > 0)
                {
                    tmp = System.Text.Encoding.UTF8.GetBytes(reason);

                    buffer.Write(tmp, 0, tmp.Length);
                }

                buffer.Close();

                return buffer.ToArray();
            }
        }

        internal static bool IsReserved(ushort code)
        {
            return code == (ushort)CloseStatusCode.Undefined ||
                   
                   code == (ushort)CloseStatusCode.NoStatusCode ||

                   code == (ushort)CloseStatusCode.Abnormal ||

                   code == (ushort)CloseStatusCode.TlsHandshakeFailure;
        }

        internal static bool IsReserved(CloseStatusCode code)
        {
            return code == CloseStatusCode.Undefined ||

                   code == CloseStatusCode.NoStatusCode ||

                   code == CloseStatusCode.Abnormal ||

                   code == CloseStatusCode.TlsHandshakeFailure;
        }


        //--------------------------------------
        // read data
        //--------------------------------------

        internal static byte[] ReadBytes(byte [] data, int offset, int count)
        {
            var buffer = new byte[count];

            System.Buffer.BlockCopy(data, offset, buffer, 0, count);

            return buffer;
        }


    }
}
