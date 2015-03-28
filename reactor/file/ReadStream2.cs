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
    public class ReadStream2 : Reactor.IReadable2<Reactor.Buffer>
    {
        //--------------------------------------------
        // state
        //--------------------------------------------

        private Reactor.Channel<Reactor.Buffer> reads;

        private Reactor.Channel<System.Exception> errors;

        private Reactor.Channel ends;

        private FileStream      stream;

        private bool            paused;

        public ReadStream2(string path)
        {
            this.stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Read);

            this.reads  = new Channel<Reactor.Buffer>();

            this.errors = new Channel<System.Exception>();

            this.ends   = new Channel();

            this.paused = true;
        }

        #region IReadable2

        public void Read(Reactor.Action<Reactor.Buffer> callback)
        {
            this.reads.Recv(callback);

            this.Resume();
        }

        public void Error(Reactor.Action<Exception> callback)
        {
            this.errors.Recv(callback);
        }

        public void End(Reactor.Action callback)
        {
            this.ends.Recv(callback);
        }

        public void Pause()
        {
            this.paused = true;
        }

        public void Resume()
        {
            if (this.paused)
            {
                this.paused = false;

                this._Read();
            }
        }

        public IReadable2<Reactor.Buffer> Pipe(IWriteable2<Reactor.Buffer> writeable)
        {
            //------------------------------
            // error
            //------------------------------

            Reactor.Action<Exception> error_handler = error =>
            {

                try
                {
                    this.errors.Send(error);

                    this.ends.Send();

                    this.stream.Close();

                    this.stream.Dispose();
                }
                catch (Exception error2) { this.errors.Send(error2); }
            };


            //------------------------------
            // end
            //------------------------------

            Reactor.Action end_handler = () => {

                try
                {
                    this.stream.Close();

                    this.stream.Dispose();
                }
                catch (Exception error) { this.errors.Send(error); }
            };

            this.Read(data =>
            {
                this.Pause();

                writeable.Write(data).Error(error_handler);

                writeable.Flush().Then(this.Resume).Error(error_handler);
            });

            this.End(end_handler);

            return this;
        }

        #endregion

        #region _Read

        private byte[] read_buffer = new byte[65536];

        private void _Read()
        {
            Reactor.IO.Read(this.stream, this.read_buffer).Then(read => {

                if (read == 0) { this.ends.Send(); return; }

                var buffer = Reactor.Buffer.Create(this.read_buffer, 0, read);

                this.reads.Send(buffer);

                if (!this.paused) this._Read();

            }).Error(error => {

                this.errors.Send(error);

                this.ends.Send();
            });
        }

        #endregion

        public static ReadStream2 Create(string path)
        {
            return new ReadStream2(path);
        }
    }
}
