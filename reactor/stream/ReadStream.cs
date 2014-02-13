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
    /// Reactor ReadStream. Provides evented read 
    /// operations on a System.IO.Stream.
    /// </summary>
    internal class ReadStream : IReadable
    {
        private Stream                 Stream       { get; set; }

        private bool                   Paused       { get; set; }

        private bool                   WaitOnRead   { get; set; }

        /// <summary>
        /// Creates a new ReadStream
        /// </summary>
        /// <param name="Stream">The stream to read from.</param>
        public ReadStream(Stream Stream) : this(Stream, false) {
        
        }

        /// <summary>
        /// Creates a new ReadStream
        /// </summary>
        /// <param name="Stream">The stream to read from.</param>
        /// <param name="WaitOnRead">Wait On Data</param>
        public ReadStream(Stream Stream, bool WaitOnRead)
        {
            this.Stream     = Stream;

            this.Paused     = true;

            this.WaitOnRead = WaitOnRead;
        }

        #region IReadStream

        public  event Action<Exception> OnError;

        private event Action<Buffer>    ondata;

        public  event Action<Buffer>    OnData
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

        public  event Action OnEnd;

        public  event Action OnClose;

        public IReadable Pipe(IWriteable writeable)
        {
            this.OnData += (data) =>  writeable.Write(data);
            
            this.OnEnd  += ()     =>  writeable.End();

            this.Resume();

            if (writeable is IReadable)
            {
                return writeable as IReadable;
            }

            return null;
        }

        public void Resume()
        {
            if (this.Paused)
            {
                this.Paused = false;

                this.ReadFromStream();
            }
        }

        public void Pause()
        {
            this.Paused = true;
        }

        #endregion

        #region ReadFromStream

        private void ReadFromStream()
        {
            try
            {
                var buffer = new byte[65536];

                var handle = this.Stream.BeginRead(buffer, 0, buffer.Length, (result) =>
                {
                    try
                    {
                        int read = this.Stream.EndRead(result);

                        if (read > 0)
                        {
                            Loop.Post(() =>
                            {
                                if (this.ondata != null)
                                {
                                    var b = new Buffer(buffer, 0, read);

                                    b.Seek(0);

                                    this.ondata(b);
                                }

                                if (!this.Paused)
                                {
                                    this.ReadFromStream();
                                }
                            });

                            return;
                        }

                        Loop.Post(() =>
                        {
                            if (this.OnEnd != null)
                            {
                                this.OnEnd();
                            }

                            if (this.OnClose != null)
                            {
                                this.OnClose();
                            }
                        });
                    }
                    catch (Exception exception)
                    {
                        Loop.Post(() =>
                        {
                            if (this.OnError != null)
                            {
                                this.OnError(exception);
                            }

                            if (this.OnEnd != null)
                            {
                                this.OnEnd();
                            }

                            if (this.OnClose != null)
                            {
                                this.OnClose();
                            }
                        });
                    }

                }, null);

                if (this.WaitOnRead)
                {
                    handle.AsyncWaitHandle.WaitOne();
                }
            }
            catch (Exception exception)
            {
                Loop.Post(() =>
                {
                    if (this.OnError != null)
                    {
                        this.OnError(exception);
                    }

                    if (this.OnEnd != null)
                    {
                        this.OnEnd();
                    }

                    if (this.OnClose != null)
                    {
                        this.OnClose();
                    }
                });
            }
        }

        #endregion
    }
}
