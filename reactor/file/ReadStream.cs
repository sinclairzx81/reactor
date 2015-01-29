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

namespace Reactor.File
{
    public class ReadStream : IReadable<Reactor.Buffer>
    {
        private FileStream stream;

        private bool       reading;

        private bool       closed;

        private long       index;

        private long       count;

        private long       received;

        private bool       paused; 

        #region Constructor

        public ReadStream(string filename, long index, long count, System.IO.FileMode mode, System.IO.FileShare share)
        {
            this.stream = System.IO.File.Open(filename, mode, FileAccess.Read, share);

            this.reading    = false;

            this.index      = (index > this.stream.Length) ? this.stream.Length : index;

            this.count      = (count > this.stream.Length) ? this.stream.Length : count;

            this.received   = 0;
            
            this.closed     = false;

            this.paused     = true;

            this.stream.Seek(this.index, SeekOrigin.Begin);
        }

        #endregion

        #region Properties

        public long Length
        {
            get
            {
                return this.stream.Length;
            }
        }

        #endregion

        #region IReadStream

        public event Action<Exception> OnError;

        private event Action<Buffer>   ondata;

        public event Action<Buffer>    OnData
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
            
            if(!this.reading)
            {
                if(!this.closed)
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

            IO.Read(this.stream, this.readbuffer, (exception, read) => {

                //----------------------------------------------
                // exception
                //----------------------------------------------
                if(exception != null)
                {
                    if(this.OnError != null)
                    {
                        this.OnError(exception);
                    }

                    if(this.OnEnd != null)
                    {
                        this.OnEnd();
                    }
                    
                    try
                    {
                        this.stream.Dispose();
                    }
                    catch(Exception _exception)
                    {
                        if (this.OnError != null)
                        {
                            this.OnError(_exception);
                        }                        
                    }

                    this.reading = false;

                    this.closed  = true;

                    return;
                }

                //----------------------------------------------
                // end of stream
                //----------------------------------------------
                if (read == 0)
                {
                    if(this.OnEnd != null)
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

                    this.closed  = true;

                    return;
                }

                //----------------------------------------------
                // increment received.
                //----------------------------------------------

                this.received = this.received + read;

                //----------------------------------------------
                // received expected
                //----------------------------------------------

                if (this.received == this.count)
                {
                    if(this.ondata != null)
                    {
                        this.ondata(new Buffer(this.readbuffer, 0, read));
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
                // received overflow
                //----------------------------------------------

                if (this.received > this.count)
                {
                    var overflow = this.received - this.count;

                    var range    = (int)(read - overflow);

                    if (this.ondata != null)
                    {
                        this.ondata(new Buffer(this.readbuffer, 0, range));
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
                    
                    this.closed  = true;

                    return;
                }

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

                if(!this.paused)
                {
                    this.Read();
                }
                else
                {
                    this.reading = false;
                }
            });
        }

        #region Statics

        public static ReadStream Create(string filename, FileMode mode, FileShare share)
        {
            return new ReadStream(filename, 0, long.MaxValue, mode, share);
        }

        public static ReadStream Create(string filename, long index, FileMode mode, FileShare share)
        {
            return new ReadStream(filename, index, long.MaxValue, mode, share);
        }

        public static ReadStream Create(string filename, long index, long count, FileMode mode, FileShare share)
        {
            return new ReadStream(filename, index, count, mode, share);
        }

        public static ReadStream Create(string filename, FileMode mode)
        {
            return new ReadStream(filename, 0, long.MaxValue, mode, FileShare.Read);
        }

        public static ReadStream Create(string filename, long index, FileMode mode)
        {
            return new ReadStream(filename, index, long.MaxValue, mode, FileShare.Read);
        }

        public static ReadStream Create(string filename, long index, long count, FileMode mode)
        {
            return new ReadStream(filename, index, count, mode, FileShare.Read);
        }

        public static ReadStream Create(string filename)
        {
            return new ReadStream(filename, 0, long.MaxValue, FileMode.OpenOrCreate, FileShare.Read);
        }

        public static ReadStream Create(string filename, long index)
        {
            return new ReadStream(filename, index, long.MaxValue, FileMode.OpenOrCreate, FileShare.Read);
        }

        public static ReadStream Create(string filename, long index, long count)
        {
            return new ReadStream(filename, index, count, FileMode.OpenOrCreate, FileShare.Read);
        }

        #endregion
    }
}