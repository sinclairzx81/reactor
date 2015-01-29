﻿/*--------------------------------------------------------------------------

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
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Reactor.Http
{
    public class ServerResponse : IWriteable<Reactor.Buffer>
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

        internal class FlushCommand : Command
        {
            public FlushCommand(Action<Exception> callback)
            {
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

        private HttpContext                    context;

        private Reactor.Net.HttpListenerResponse   httplistenerresponse;

        private Stream                         stream;

        private Queue<Command>                 commands;

        private bool                           writing;

        private bool                           ended;

        internal ServerResponse(HttpContext context, Reactor.Net.HttpListenerResponse httplistenerresponse)
        {
            this.context              = context;

            this.httplistenerresponse = httplistenerresponse;

            this.stream               = this.httplistenerresponse.OutputStream;

            this.commands             = new Queue<Command>();

            this.ended                = false;

            this.writing              = false;
        }

        #region HttpListenerResponse
        
        public Encoding ContentEncoding 
        {
            get
            {
                return this.httplistenerresponse.ContentEncoding;
            }
            set
            {
                this.httplistenerresponse.ContentEncoding = value;
            }
        }

        public long ContentLength
        {
            get
            {
                return this.httplistenerresponse.ContentLength64;
            }
            set
            {
                this.httplistenerresponse.ContentLength64 = value;
            }
        }

        public string ContentType
        {
            get
            {
                return this.httplistenerresponse.ContentType;
            }
            set
            {
                this.httplistenerresponse.ContentType = value;
            }
        }

        public Reactor.Net.CookieCollection Cookies
        {
            get
            {
                return this.httplistenerresponse.Cookies;
            }
            set
            {
                this.httplistenerresponse.Cookies = value;
            }
        }

        public Reactor.Net.WebHeaderCollection Headers
        {
            get
            {
                return this.httplistenerresponse.Headers;
            }
            set
            {
                this.httplistenerresponse.Headers = value;
            }
        }

        public bool KeepAlive
        {
            get
            {
                return this.httplistenerresponse.KeepAlive;
            }
            set
            {
                this.httplistenerresponse.KeepAlive = value;
            }
        }

        public Version ProtocolVersion
        {
            get
            {
                return this.httplistenerresponse.ProtocolVersion;
            }
            set
            {
                this.httplistenerresponse.ProtocolVersion = value;
            }
        }

        public string RedirectLocation
        {
            get
            {
                return this.httplistenerresponse.RedirectLocation;
            }
            set
            {
                this.httplistenerresponse.RedirectLocation = value;
            }
        }

        //public bool SendChunked
        //{
        //    get
        //    {
        //        return this.HttpListenerResponse.SendChunked;
        //    }
        //    set
        //    {
        //        this.HttpListenerResponse.SendChunked = value;
        //    }
        //}

        public int StatusCode
        {
            get
            {
                return this.httplistenerresponse.StatusCode;
            }
            set
            {
                this.httplistenerresponse.StatusCode = value;
            }
        }

        public string StatusDescription
        {
            get
            {
                return this.httplistenerresponse.StatusDescription;
            }
            set
            {
                this.httplistenerresponse.StatusDescription = value;
            }
        }

        public void AddHeader(string name, string value)
        {
            this.httplistenerresponse.AddHeader(name, value);
        }

        public void AppendCookie(Reactor.Net.Cookie cookie)
        {
            this.httplistenerresponse.AppendCookie(cookie);
        }

        public void AppendHeader(string name, string value)
        {
            this.httplistenerresponse.AppendHeader(name, value);
        }

        public void Redirect(string url)
        {
            this.httplistenerresponse.Redirect(url);
        }

        public void SetCookie(Reactor.Net.Cookie cookie)
        {
            this.httplistenerresponse.SetCookie(cookie);
        }

        #endregion

        #region IWriteable

        public void Write(Buffer buffer, Action<Exception> callback)
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

        public void Write(Buffer buffer)
        {
            this.Write(buffer, exception => { });
        }

        public void Flush(Action<Exception> callback)
        {
            this.commands.Enqueue(new FlushCommand(callback));

            if (!this.writing)
            {
                this.writing = true;

                if (!this.ended)
                {
                    this.Write();
                }
            }
        }

        public void Flush()
        {
            this.Flush(exception => { });
        }

        public void End(Action<Exception> callback)
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

        public void End()
        {
            this.End(exception => { });
        }

        public event Action<Exception> OnError;

        #endregion

        private void Write()
        {
            //----------------------------------
            // command: write
            //----------------------------------

            var command = this.commands.Dequeue();

            //----------------------------------
            // command: write
            //----------------------------------

            if (command is WriteCommand)
            {
                var write = command as WriteCommand;

                IO.Write(this.stream, write.Buffer.ToArray(), (exception) =>
                {
                    command.Callback(exception);

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
            // command: flush
            //----------------------------------

            if (command is FlushCommand)
            {
                try
                {
                    this.stream.Flush();

                    command.Callback(null);
                }
                catch (Exception exception)
                {
                    command.Callback(exception);

                    if (this.OnError != null)
                    {
                        this.OnError(exception);
                    }

                    this.ended = true;
                }
                if (this.commands.Count > 0)
                {
                    this.Write();

                    return;
                }

                this.writing = false;
            }

            //----------------------------------
            // command: end
            //----------------------------------

            if (command is EndCommand)
            {
                var end = command as EndCommand;

                try
                {
                    this.stream.Dispose();

                    end.Callback(null);
                }
                catch (Exception exception)
                {
                    end.Callback(exception);

                    if (this.OnError != null)
                    {
                        this.OnError(exception);
                    }
                }

                this.writing = false;

                this.ended   = true;
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
