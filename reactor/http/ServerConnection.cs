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

using Reactor.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace Reactor.Http
{
    public class ServerConnection : IDuplexable<Reactor.Buffer>
    {
        #region Command

        internal class Command
        {
            public Action<Exception> Callback { get; set; }
        }

        internal class WriteCommand : Command
        {
            public Buffer Buffer { get; set; }

            public WriteCommand(Buffer buffer, Action<Exception> callback)
            {
                this.Buffer = buffer;

                this.Callback = callback;
            }
        }

        internal class EndCommand : Command
        {
            public EndCommand(Action<Exception> callback)
            {
                this.Callback = callback;
            }
        }

        #endregion

        private   HttpContext    context;

        private   HttpConnection connection;

        private   Stream         stream;

        public ServerConnection(HttpContext context, HttpConnection connection)
        {
            this.context    = context;

            this.connection = connection;

            this.stream     = connection.Stream;

            //---------------------
            // readable
            //---------------------
            this.received = 0;
            
            this.closed   = false;

            this.paused   = true;

            //---------------------
            // writeable
            //---------------------
            this.commands = new Queue<Command>();

            this.closed   = false;

            this.writing  = false;

            this.ended    = false;
        }

        //---------------------------------
        // readable
        //---------------------------------

        private bool                       reading;

        private bool                       paused;

        private long                       received;

        //---------------------------------
        // writeable
        //---------------------------------

        private Queue<Command>             commands;

        private bool                       writing;

        private bool                       ended;

        //---------------------------------
        // shared
        //---------------------------------

        private bool                       closed;

        #region HttpConnection

        public bool IsClosed 
        {
            get
            {
                return this.connection.IsClosed;
            }
        }
        public bool IsSecure 
        {
            get
            {
                return this.connection.IsSecure;
            }
        }
        
        public IPEndPoint LocalEndPoint 
        { 
            get
            {
                return this.connection.LocalEndPoint;
            }
        }

        public Reactor.Net.ListenerPrefix Prefix 
        {
            get
            {
                return this.connection.Prefix;
            }
        }

        
        public IPEndPoint RemoteEndPoint 
        { 
            get
            {
                return this.RemoteEndPoint;
            }
        }
        
        public int Reuses 
        {
            get
            {
                return this.connection.Reuses;
            }
        }
        
        #endregion

        #region IReadStream

        public event  Action<Exception> OnError;

        private event Action<Buffer> ondata;

        public event  Action<Buffer> OnData
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

        public event  Action         OnEnd;

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

        #region IWriteable

        public void Write (Buffer buffer, Action<Exception> callback)
        {
            this.commands.Enqueue(new WriteCommand(buffer, callback));

            if (!this.writing)
            {
                this.writing = true;

                if (!this.ended)
                {
                    this.Write();
                }
            }
        }

        public void Write (Buffer buffer)
        {
            this.Write(buffer, exception => { });
        }

        public void Flush (Action<Exception> callback)
        {
            callback(null);
        }

        public void Flush ()
        {
            this.Flush(exception => { });
        }

        public void End   (Action<Exception> callback)
        {
            this.commands.Enqueue(new EndCommand(callback));

            if (!this.writing)
            {
                this.writing = true;

                if (!this.ended)
                {
                    this.Write();
                }
            }
        }

        public void End   ()
        {
            this.End(exception => { });
        }

        #endregion

        private byte [] readbuffer = new byte[Reactor.Settings.DefaultReadBufferSize];

        private void Read  ()
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
                        //--------------------------------
                        // special case close as we need 
                        // to clean up more than the 
                        // underlying stream.
                        //--------------------------------

                        this.connection.Close();

                        //this.stream.Close();
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
                        //--------------------------------
                        // special case close as we need 
                        // to clean up more than the 
                        // underlying stream.
                        //--------------------------------

                        this.connection.Close();

                        //this.stream.Dispose();
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

        private void Write ()
        {
            var command  = this.commands.Dequeue();

            //----------------------------------
            // command: write
            //----------------------------------

            if (command is WriteCommand)
            {
                var write = command as WriteCommand;

                IO.Write(this.stream, write.Buffer.ToArray(), exception =>
                {
                    write.Callback(exception);

                    if (exception != null)
                    {
                        if (this.OnError != null)
                        {
                            this.OnError(exception);
                        }

                        this.ended = true;

                        return;
                    }

                    if (this.commands.Count > 0)
                    {
                        this.Write();

                        return;
                    }

                    this.writing = false;
                });
            }

            //----------------------------------
            // command: end
            //----------------------------------

            if (command is EndCommand)
            {
                var end = command as EndCommand;

                this.writing = false;

                this.ended   = true;

                try
                {
                    //--------------------------------
                    // special case close as we need 
                    // to clean up more than the 
                    // underlying stream.
                    //--------------------------------

                    this.connection.Close();

                    //this.stream.Close();
                }
                catch(Exception exception)
                {
                    if (this.OnError != null)
                    {
                        this.OnError(exception);
                    }

                    if (this.OnEnd != null)
                    {
                        this.OnEnd();
                    }

                    end.Callback(exception);
                }
            }
        }

        #region IWritables

        public void Write(byte[] buffer)
        {
            this.Write(Reactor.Buffer.Create(buffer));
        }

        public void Write(byte[] buffer, int index, int count)
        {
            this.Write(Reactor.Buffer.Create(buffer, 0, count));
        }

        public void Write(string data)
        {
            var buffer = System.Text.Encoding.UTF8.GetBytes(data);

            this.Write(buffer);
        }

        public void Write(string format, object arg0)
        {
            format = string.Format(format, arg0);

            var buffer = System.Text.Encoding.UTF8.GetBytes(format);

            this.Write(buffer);
        }

        public void Write(string format, params object[] args)
        {
            format = string.Format(format, args);

            var buffer = System.Text.Encoding.UTF8.GetBytes(format);

            this.Write(buffer);
        }

        public void Write(string format, object arg0, object arg1)
        {
            format = string.Format(format, arg0, arg1);

            var buffer = System.Text.Encoding.UTF8.GetBytes(format);

            this.Write(buffer);
        }

        public void Write(string format, object arg0, object arg1, object arg2)
        {
            format = string.Format(format, arg0, arg1, arg2);

            var buffer = System.Text.Encoding.UTF8.GetBytes(format);

            this.Write(buffer);
        }

        public void Write(byte data)
        {
            this.Write(new byte[1] { data });
        }

        public void Write(bool value)
        {
            var buffer = BitConverter.GetBytes(value);

            this.Write(buffer);
        }

        public void Write(short value)
        {
            var buffer = BitConverter.GetBytes(value);

            this.Write(buffer);
        }

        public void Write(ushort value)
        {
            var buffer = BitConverter.GetBytes(value);

            this.Write(buffer);
        }

        public void Write(int value)
        {
            var buffer = BitConverter.GetBytes(value);

            this.Write(buffer);
        }

        public void Write(uint value)
        {
            var buffer = BitConverter.GetBytes(value);

            this.Write(buffer);
        }

        public void Write(long value)
        {
            var buffer = BitConverter.GetBytes(value);

            this.Write(buffer);
        }

        public void Write(ulong value)
        {
            var buffer = BitConverter.GetBytes(value);

            this.Write(buffer);
        }

        public void Write(float value)
        {
            var buffer = BitConverter.GetBytes(value);

            this.Write(buffer);
        }

        public void Write(double value)
        {
            var buffer = BitConverter.GetBytes(value);

            this.Write(buffer);
        }

        #endregion
    }
}
