/*--------------------------------------------------------------------------

Reactor

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

namespace Reactor.Http.Protocol {

    public class ChunkedBodyReader : Reactor.IReadable {

        private Reactor.IReadable readable;

        public ChunkedBodyReader(Reactor.IReadable readable) {
            
            this.readable = readable;
        }

        public void OnReadable(Action callback)
        {
            throw new NotImplementedException();
        }

        public void OnceReadable(Action callback)
        {
            throw new NotImplementedException();
        }

        public void RemoveReadable(Action callback)
        {
            throw new NotImplementedException();
        }

        public void OnRead(Action<Buffer> callback)
        {
            throw new NotImplementedException();
        }

        public void OnceRead(Action<Buffer> callback)
        {
            throw new NotImplementedException();
        }

        public void RemoveRead(Action<Buffer> callback)
        {
            throw new NotImplementedException();
        }

        public void OnError(Action<Exception> callback)
        {
            throw new NotImplementedException();
        }

        public void RemoveError(Action<Exception> callback)
        {
            throw new NotImplementedException();
        }

        public void OnEnd(Action callback)
        {
            throw new NotImplementedException();
        }

        public void RemoveEnd(Action callback)
        {
            throw new NotImplementedException();
        }

        public Buffer Read(int count)
        {
            throw new NotImplementedException();
        }

        public Buffer Read()
        {
            throw new NotImplementedException();
        }

        public void Unshift(Buffer buffer)
        {
            throw new NotImplementedException();
        }

        public void Pause()
        {
            throw new NotImplementedException();
        }

        public void Resume()
        {
            throw new NotImplementedException();
        }

        public IReadable Pipe(IWritable writeable)
        {
            throw new NotImplementedException();
        }
    }
}
