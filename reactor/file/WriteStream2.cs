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
using System.IO;

namespace Reactor.File
{
    public class WriteStream2 : IWriteable2<Reactor.Buffer>
    {
        private FileStream   stream;
        
        private Throttle     throttle;

        private Channel<Exception> errors;

        public WriteStream2(string path)
        {
            this.throttle   = new Throttle(1);

            this.stream     = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);

            this.errors     = new Channel<Exception>();
        }

        public void Error(Reactor.Action<Exception> callback)
        {
            this.errors.Recv(callback);
        }

        public Future Write(Reactor.Buffer buffer)
        {
            return this.throttle.Run(() => new Future((resolve, reject) => {

                Reactor.IO.Write(this.stream, buffer.ToArray())

                    .Then(resolve)

                    .Error(reject);
            }));
        }

        public Future Flush()
        {
            return this.throttle.Run(() => new Future((resolve, reject) => {

                try {

                    this.stream.Flush();

                    resolve();
                }
                catch (Exception error) {

                    this.errors.Send(error);

                    reject(error);
                }
            }));
        }

        public Future End()
        {
            return this.throttle.Run(() => new Future((resolve, reject) => {

                try {

                    this.stream.Close();

                    this.stream.Dispose();

                    resolve();
                }
                catch (Exception error) {

                    this.errors.Send(error);

                    reject(error);
                }
            }));
        }
    }
}
