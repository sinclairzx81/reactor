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
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Reactor.Web.Socket.Protocol
{

    internal class Payload : IEnumerable<byte>
    {
        public const ulong MaxLength = long.MaxValue;

        internal bool IsMasked              { get; private set; }

        public byte[] ExtensionData         { get; private set; }

        public byte[] ApplicationData       { get; private set; }

        public Payload() : this(new byte[] { })
        {

        }

        public Payload(byte[] appData) : this(new byte[] { }, appData)
        {
        }

        public Payload(string appData) : this(Encoding.UTF8.GetBytes(appData))
        {

        }

        public Payload(byte[] appData, bool masked) : this(new byte[] { }, appData, 0, appData.Length, masked)
        {

        }

        public Payload(byte[] extData, byte[] appData) : this(extData, appData, 0, appData.Length, false)
        {

        }

        public Payload(byte[] extData, byte[] appData, int appDataStart, int appDataLength, bool masked)
        {
            if ((ulong)extData.LongLength + (ulong)appData.LongLength > MaxLength)
            {
                throw new ArgumentOutOfRangeException("The length of 'extData' plus 'appData' must be less than MaxLength.");
            }

            this.ExtensionData         = extData;
            
            this.ApplicationData       = appData;

            this.IsMasked              = masked;
        }

        public ulong  Length
        {
            get { return (ulong)(ExtensionData.LongLength + ApplicationData.LongLength); }
        }

        public void Mask(byte[] maskingKey)
        {
            if (ExtensionData.LongLength > 0)
            {
                Mask(ExtensionData, maskingKey);
            }

            if (ApplicationData.LongLength > 0)
            {
                Mask(ApplicationData, maskingKey);
            }

            IsMasked = !IsMasked;
        }

        public byte[] ToByteArray()
        {
            return ExtensionData.LongLength > 0 ? new List<byte>(this).ToArray() : ApplicationData;
        }

        public override string ToString()
        {
            return BitConverter.ToString(ToByteArray());
        }

        private static void Mask(byte[] src, byte[] key)
        {
            for (long i = 0; i < src.LongLength; i++)
            {
                src[i] = (byte)(src[i] ^ key[i % 4]);
            }
        }

        #region IEnumerator<byte>

        public IEnumerator<byte> GetEnumerator()
        {
            foreach (byte b in ExtensionData)
            {
                yield return b;
            }

            foreach (byte b in ApplicationData)
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
