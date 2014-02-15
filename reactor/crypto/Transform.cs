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
using System.Security.Cryptography;
using System.Text;

namespace Reactor.Crypto
{
    public class Transform : IReadable, IWriteable
    {
        private ICryptoTransform ICryptoTransform { get; set; }

        public Transform(ICryptoTransform ICryptoTransform)
        {
            this.ICryptoTransform = ICryptoTransform;
        }

        #region IWriteable

        public void Write(Buffer buffer)
        {
            var _buffer = buffer.ToArray();

            this.TransformData(_buffer, 0, _buffer.Length);
        }

        public void Write(string data)
        {
            var buffer = Encoding.UTF8.GetBytes(data);

            this.TransformData(buffer, 0, buffer.Length);
        }

        public void Write(string format, object arg0)
        {
            var buffer = Encoding.UTF8.GetBytes(string.Format(format, arg0));

            this.TransformData(buffer, 0, buffer.Length);
        }

        public void Write(string format, params object[] args)
        {
            var buffer = Encoding.UTF8.GetBytes(string.Format(format, args));

            this.TransformData(buffer, 0, buffer.Length);
        }

        public void Write(string format, object arg0, object arg1)
        {
            var buffer = Encoding.UTF8.GetBytes(string.Format(format, arg0, arg1));

            this.TransformData(buffer, 0, buffer.Length);
        }

        public void Write(string format, object arg0, object arg1, object arg2)
        {
            var buffer = Encoding.UTF8.GetBytes(string.Format(format, arg0, arg1, arg2));

            this.TransformData(buffer, 0, buffer.Length);
        }

        public void Write(byte data)
        {
            var buffer = new byte[] { data };

            this.TransformData(buffer, 0, buffer.Length);
        }

        public void Write(byte[] buffer)
        {
            this.TransformData(buffer, 0, buffer.Length);
        }

        public void Write(byte[] buffer, int index, int count)
        {
            this.TransformData(buffer, index, count);
        }

        public void Write(bool value)
        {
            var buffer = BitConverter.GetBytes(value);

            this.TransformData(buffer, 0, buffer.Length);
        }

        public void Write(short value)
        {
            var buffer = BitConverter.GetBytes(value);

            this.TransformData(buffer, 0, buffer.Length);
        }

        public void Write(ushort value)
        {
            var buffer = BitConverter.GetBytes(value);

            this.TransformData(buffer, 0, buffer.Length);
        }

        public void Write(int value)
        {
            var buffer = BitConverter.GetBytes(value);

            this.TransformData(buffer, 0, buffer.Length);
        }

        public void Write(uint value)
        {
            var buffer = BitConverter.GetBytes(value);

            this.TransformData(buffer, 0, buffer.Length);
        }

        public void Write(long value)
        {
            var buffer = BitConverter.GetBytes(value);

            this.TransformData(buffer, 0, buffer.Length);
        }

        public void Write(ulong value)
        {
            var buffer = BitConverter.GetBytes(value);

            this.TransformData(buffer, 0, buffer.Length);
        }

        public void Write(float value)
        {
            var buffer = BitConverter.GetBytes(value);

            this.TransformData(buffer, 0, buffer.Length);
        }

        public void Write(double value)
        {
            var buffer = BitConverter.GetBytes(value);

            this.TransformData(buffer, 0, buffer.Length);
        }

        public void End()
        {
            Loop.Post(() => {

                if(this.OnEnd != null)
                {
                    this.OnEnd();
                }

                if (this.OnClose != null)
                {
                    this.OnClose();
                }
            });
        }

        public event Action<Exception> OnError;

        #endregion

        #region IReadable

        public event Action<Buffer> OnData;

        public event Action         OnEnd;

        public event Action         OnClose;

        public IReadable Pipe(IWriteable writeable)
        {
            this.OnData += (data) => writeable.Write(data);

            this.OnEnd  += ()     => writeable.End();

            if(writeable is IReadable)
            {
                return writeable as IReadable;
            }

            return null;
        }

        public void Pause()
        {
           
        }

        public void Resume()
        {

        }

        #endregion

        #region Transform

        private void TransformData(byte[] data, int index, int count)
        {
            try
            {
                Console.WriteLine("method not implemented");

                // todo: split block sizes

                byte [] block = new byte[65536];
                
                if(data.Length < block.Length) {

                    var temp = new byte[65536];

                    System.Buffer.BlockCopy(data, 0, temp, 0, data.Length);

                    data = temp;
                }

                int read = this.ICryptoTransform.TransformBlock(data, 0, data.Length, block, 0);

                if(read > 0) {

                    if (this.OnData != null) {

                        this.OnData(Reactor.Buffer.Create(block, 0, read));
                    }
                }
            }
            catch(Exception exception)
            {
                Console.WriteLine(exception);

                Loop.Post(() => {

                    if (this.OnError != null) {

                        this.OnError(exception);
                    }
                });
            }
        }

        #endregion

        #region Statics

        public static Transform Create(ICryptoTransform ICryptoTransform)
        {
            return new Transform(ICryptoTransform);
        }

        #endregion
    }
}
