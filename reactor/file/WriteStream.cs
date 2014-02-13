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
using System.IO;

namespace Reactor.File
{
    public class WriteStream: IWriteable
    {
        private FileStream             FileStream             { get; set; }

        private Reactor.WriteStream   _WriteStream            { get; set; }

        #region Constructor

        public WriteStream(string Filename)
        {
            this.FileStream = System.IO.File.Open(Filename, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write);

            this._WriteStream = new Reactor.WriteStream(this.FileStream);
        }

        #endregion

        #region IWriteable

        public void Write(byte[] data)
        {
            this._WriteStream.Write(data);
        }

        public void Write(Buffer buffer)
        {
            this._WriteStream.Write(buffer);
        }

        public void Write(string data)
        {
            this._WriteStream.Write(data);
        }

        public void Write(string format, object arg0)
        {
            this._WriteStream.Write(format, arg0);
        }

        public void Write(string format, params object[] args)
        {
            this._WriteStream.Write(format, args);
        }

        public void Write(string format, object arg0, object arg1)
        {
            this._WriteStream.Write(format, arg0, arg1);
        }

        public void Write(string format, object arg0, object arg1, object arg2)
        {
            this._WriteStream.Write(format, arg0, arg1, arg2);
        }

        public void Write(byte data)
        {
            this._WriteStream.Write(data);
        }

        public void Write(byte[] buffer, int index, int count)
        {
            this._WriteStream.Write(buffer, index, count);
        }

        public void Write(bool value)
        {
            this._WriteStream.Write(value);
        }

        public void Write(short value)
        {
            this._WriteStream.Write(value);
        }

        public void Write(ushort value)
        {
            this._WriteStream.Write(value);
        }

        public void Write(int value)
        {
            this._WriteStream.Write(value);
        }

        public void Write(uint value)
        {
            this._WriteStream.Write(value);
        }

        public void Write(long value)
        {
            this._WriteStream.Write(value);
        }

        public void Write(ulong value)
        {
            this._WriteStream.Write(value);
        }

        public void Write(float value)
        {
            this._WriteStream.Write(value);
        }

        public void Write(double value)
        {
            this._WriteStream.Write(value);
        }

        public void End()
        {
            this._WriteStream.End();
        }

        public event Action<Exception> OnError 
        {
            add 
            {
                this._WriteStream.OnError += value;
            }

            remove
            {
                this._WriteStream.OnError -= value;
            }
        }

        #endregion

        #region Statics

        public static WriteStream Create(string Filename)
        {
            return new WriteStream(Filename);
        }

        #endregion
    }
}
