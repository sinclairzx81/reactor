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
    public class ReadStream : IReadable
    {
        private FileStream           FileStream             { get; set; }

        private Reactor.ReadStream  _ReadStream             { get; set; }

        #region Constructor

        public ReadStream(string Filename) 
        {
            this.FileStream   = System.IO.File.Open(Filename, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read);

            this._ReadStream  = new Reactor.ReadStream(this.FileStream);
        }

        #endregion

        #region IReadStream

        public event Action<Exception> OnError
        {
            add
            {
                this._ReadStream.OnError += value;
            }
            remove
            {
                this._ReadStream.OnError -= value;
            }            
        }

        public event Action<Buffer> OnData
        {
            add
            {
                this._ReadStream.OnData += value;
            }
            remove
            {
                this._ReadStream.OnData -= value;
            }
        }

        public event Action OnEnd
        {
            add
            {
                this._ReadStream.OnEnd += value;
            }
            remove
            {
                this._ReadStream.OnEnd -= value;
            }
        }

        public event Action  OnClose
        {
            add
            {
                this._ReadStream.OnClose += value;
            }
            remove
            {
                this._ReadStream.OnClose -= value;
            }
        }

        public IReadable Pipe(IWriteable writestream)
        {
            return this._ReadStream.Pipe(writestream);
        }

        public void Pause()
        {
            this._ReadStream.Pause();
        }

        public void Resume()
        {
            this._ReadStream.Resume();
        }

        #endregion

        #region Statics

        public static ReadStream Create(string Filename)
        {
            return new ReadStream(Filename);
        }

        #endregion
    }
}
