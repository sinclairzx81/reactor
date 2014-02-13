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
using System.Threading;

namespace Reactor
{


    /// <summary>
    /// Reactor WriteStream. Provides asynchronous 
    /// write operations on a System.IO.Stream.
    /// </summary>
    internal class WriteStream : IWriteable
    {
        private object Lock { get; set; }

        private Stream                 Stream                 { get; set; }

        private Buffer                 Buffer                 { get; set; }

        private bool                   Writing                { get; set; }

        public  bool                   Ended                  { get; set; }

        private Action                 OnWriteComplete        { get; set; }

        #region Constructor

        public WriteStream(Stream Stream)
        {
            this.Lock = new object();

            this.Buffer                 = new Buffer();

            this.Writing                = false;
                
            this.Ended                  = false;

            this.OnWriteComplete        = null;

            this.Stream                 = Stream;
        }

        #endregion

        #region IWriteable

        public event Action<Exception> OnError;

        public void Write(Buffer buffer)
        {
            lock(this.Lock)
            {
            this.Buffer.Write(buffer);

            if(!this.Writing)
            {
                this.Writing = true;

                this.WriteToStream();
            }
            }
        }

        public void Write(byte[] data)
        {
            this.Buffer.Write(data);

            if (!this.Writing)
            {
                this.Writing = true;

                this.WriteToStream();
            }
        }

        public void Write(string data)
        {
            this.Buffer.Write(data);

            if (!this.Writing)
            {
                this.Writing = true;

                this.WriteToStream();
            }
        }

        public void Write(string format, object arg0)
        {
            this.Buffer.Write(format, arg0);

            if (!this.Writing)
            {
                this.Writing = true;

                this.WriteToStream();
            }
        }

        public void Write(string format, params object[] args)
        {
            this.Buffer.Write(format, args);

            if (!this.Writing)
            {
                this.Writing = true;

                this.WriteToStream();
            }
        }

        public void Write(string format, object arg0, object arg1)
        {
            this.Buffer.Write(format, arg0, arg1);

            if (!this.Writing)
            {
                this.Writing = true;

                this.WriteToStream();
            }
        }

        public void Write(string format, object arg0, object arg1, object arg2)
        {
            this.Buffer.Write(format, arg0, arg1, arg2);

            if (!this.Writing)
            {
                this.Writing = true;

                this.WriteToStream();
            }
        }

        public void Write(byte data)
        {
            this.Buffer.Write(data);

            if (!this.Writing)
            {
                this.Writing = true;

                this.WriteToStream();
            }
        }

        public void Write(byte[] buffer, int index, int count)
        {
            this.Buffer.Write(buffer, index, count);

            if (!this.Writing)
            {
                this.Writing = true;

                this.WriteToStream();
            }
        }

        public void Write(bool value)
        {
            this.Buffer.Write(value);

            if (!this.Writing)
            {
                this.Writing = true;

                this.WriteToStream();
            }
        }

        public void Write(short value)
        {
            this.Buffer.Write(value);

            if (!this.Writing)
            {
                this.Writing = true;

                this.WriteToStream();
            }
        }

        public void Write(ushort value)
        {
            this.Buffer.Write(value);

            if (!this.Writing)
            {
                this.Writing = true;

                this.WriteToStream();
            }
        }

        public void Write(int value)
        {
            this.Buffer.Write(value);

            if (!this.Writing)
            {
                this.Writing = true;

                this.WriteToStream();
            }
        }

        public void Write(uint value)
        {
            this.Buffer.Write(value);

            if (!this.Writing)
            {
                this.Writing = true;

                this.WriteToStream();
            }
        }

        public void Write(long value)
        {
            this.Buffer.Write(value);

            if (!this.Writing)
            {
                this.Writing = true;

                this.WriteToStream();
            }
        }

        public void Write(ulong value)
        {
            this.Buffer.Write(value);

            if (!this.Writing)
            {
                this.Writing = true;

                this.WriteToStream();
            }
        }

        public void Write(float value)
        {
            this.Buffer.Write(value);

            if (!this.Writing)
            {
                this.Writing = true;

                this.WriteToStream();
            }

        }

        public void Write(double value)
        {
            this.Buffer.Write(value);

            if (!this.Writing)
            {
                this.Writing = true;

                this.WriteToStream();
            }
        }



        #endregion

        public void End()
        {
            this.End(() => { });
        }

        internal void End(Action WriteComplete)
        {
            lock(this.Lock)
            {
            if (this.Ended) {

                return;
            }

            this.Ended = true;

            if (!this.Writing)
            {
                WriteComplete();

                this.Release();

                return;
            }

            this.OnWriteComplete = () => {

                WriteComplete();

                this.Release();
            };
            }
        }

        #region WriteToStream

        private void WriteToStream()
        {
            lock(this.Lock)
            {
                    if (this.Buffer.Length == 0)
                    {
                        this.Writing = false;

                        if (this.Ended) {

                            if (this.OnWriteComplete != null) {

                                this.OnWriteComplete();
                            }
                        }

                        return;
                    }
           

                try
                {
                    byte[] buffer = this.Buffer.ToArray();

                    this.Buffer.SetLength(0);

                    this.Buffer.Seek(0);

                    this.Stream.BeginWrite(buffer, 0, buffer.Length, (result) =>
                    {
                        try
                        {
                            this.Stream.EndWrite(result);

                            this.WriteToStream();
                        }
                        catch (Exception exception)
                        {
                            Loop.Post(() => {

                                if (this.OnError != null) {

                                    this.OnError(exception);
                                }
                            });

                            this.Release();
                        }

                    }, null);
                }
                catch (Exception exception)
                {
                    Loop.Post(() => {

                        if (this.OnError != null) {

                            this.OnError(exception);
                        }
                    });
                }
            }
        }

        #endregion

        #region Privates

        private void Release()
        {
            try
            {
                this.Stream.Close();
            }
            catch
            {
             
            }
        }

        #endregion
    }
}
