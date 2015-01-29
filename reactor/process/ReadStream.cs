/*--------------------------------------------------------------------------

Reactor

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
using System.Net;

namespace Reactor.Process
{
    public class ReadStream : IReadable<Reactor.Buffer>
    {
        private Stream stream;

        private bool reading;

        private long received;

        private bool paused;

        private bool closed;

        internal ReadStream(Stream stream)
        {
            this.stream   = stream;

            this.reading  = false;

            this.paused   = true;

            this.closed   = false;

            this.received = 0;
        }

        #region IReadStream

        public event Action<Exception> OnError;

        private event Action<Buffer> ondata;

        public event Action<Buffer> OnData
        {
            add
            {
                this.ondata += value;

                this.Resume();
            }
            remove
            {
                this.ondata -= value;
            }
        }

        public event Action OnEnd;

        public IReadable<Reactor.Buffer> Pipe(IWriteable<Reactor.Buffer> writeable)
        {
            this.OnData += data =>
            {
                this.Pause();

                writeable.Write(data, (exception0) =>
                {
                    if (exception0 != null)
                    {
                        if (this.OnError != null)
                        {
                            this.OnError(exception0);
                        }

                        return;
                    }

                    writeable.Flush((exception1) =>
                    {
                        if (exception1 != null)
                        {
                            if (this.OnError != null)
                            {
                                this.OnError(exception1);
                            }

                            return;
                        }

                        this.Resume();
                    });
                });
            };

            this.OnEnd += () =>
            {
                writeable.End();
            };

            if (writeable is IReadable<Reactor.Buffer>)
            {
                return writeable as IReadable<Reactor.Buffer>;
            }

            return null;
        }

        public void Pause()
        {
            this.paused = true;
        }

        public void Resume()
        {
            this.paused = false;

            if (!this.reading)
            {
                if (!this.closed)
                {
                    this.Read();
                }
            }
        }

        public void Close()
        {
            this.closed = true;
        }

        #endregion

        private byte[] readbuffer = new byte[Reactor.Settings.DefaultReadBufferSize];

        private void Read()
        {
            this.reading = true;

            IO.Read(this.stream, this.readbuffer, (exception, read) =>
            {
                //----------------------------------------------
                // exception
                //----------------------------------------------
                if (exception != null)
                {
                    if (this.OnError != null)
                    {
                        this.OnError(exception);
                    }

                    if (this.OnEnd != null)
                    {
                        this.OnEnd();
                    }

                    try
                    {
                        this.stream.Dispose();
                    }
                    catch (Exception _exception)
                    {
                        if (this.OnError != null)
                        {
                            this.OnError(_exception);
                        }
                    }

                    this.reading = false;

                    this.closed = true;

                    return;
                }

                //----------------------------------------------
                // end of stream
                //----------------------------------------------
                if (read == 0)
                {
                    if (this.OnEnd != null)
                    {
                        this.OnEnd();
                    }

                    try
                    {
                        this.stream.Dispose();
                    }
                    catch (Exception _exception)
                    {
                        if (this.OnError != null)
                        {
                            this.OnError(_exception);
                        }
                    }

                    this.reading = false;

                    this.closed = true;

                    return;
                }

                //----------------------------------------------
                // increment received.
                //----------------------------------------------

                this.received = this.received + read;

                //----------------------------------------------
                // standard
                //----------------------------------------------
                if (this.ondata != null)
                {
                    this.ondata(new Buffer(this.readbuffer, 0, read));
                }

                //----------------------------------------------
                // continue
                //----------------------------------------------

                if (!this.paused)
                {
                    this.Read();
                }
                else
                {
                    this.reading = false;
                }
            });
        }
    }
}
