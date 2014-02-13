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
using System.Net;
using System.Text;

namespace Reactor.Http
{
    public class ServerResponse : IWriteable
    {
        private HttpListenerResponse   HttpListenerResponse   { get; set; }

        private WriteStream            WriteStream            { get; set; }

        internal ServerResponse(HttpListenerResponse HttpListenerResponse)
        {
            this.HttpListenerResponse   = HttpListenerResponse;

            this.WriteStream            = new WriteStream(this.HttpListenerResponse.OutputStream);
        }

        #region HttpListenerResponse
        
        public Encoding ContentEncoding 
        {
            get
            {
                return this.HttpListenerResponse.ContentEncoding;
            }
            set
            {
                this.HttpListenerResponse.ContentEncoding = value;
            }
        }

        public long ContentLength
        {
            get
            {
                return this.HttpListenerResponse.ContentLength64;
            }
            set
            {
                this.HttpListenerResponse.ContentLength64 = value;
            }
        }

        public string ContentType
        {
            get
            {
                return this.HttpListenerResponse.ContentType;
            }
            set
            {
                this.HttpListenerResponse.ContentType = value;
            }
        }

        public CookieCollection Cookies
        {
            get
            {
                return this.HttpListenerResponse.Cookies;
            }
            set
            {
                this.HttpListenerResponse.Cookies = value;
            }
        }

        public WebHeaderCollection Headers
        {
            get
            {
                return this.HttpListenerResponse.Headers;
            }
            set
            {
                this.HttpListenerResponse.Headers = value;
            }
        }

        public bool KeepAlive
        {
            get
            {
                return this.HttpListenerResponse.KeepAlive;
            }
            set
            {
                this.HttpListenerResponse.KeepAlive = value;
            }
        }

        public Version ProtocolVersion
        {
            get
            {
                return this.HttpListenerResponse.ProtocolVersion;
            }
            set
            {
                this.HttpListenerResponse.ProtocolVersion = value;
            }
        }

        public string RedirectLocation
        {
            get
            {
                return this.HttpListenerResponse.RedirectLocation;
            }
            set
            {
                this.HttpListenerResponse.RedirectLocation = value;
            }
        }

        public bool SendChunked
        {
            get
            {
                return this.HttpListenerResponse.SendChunked;
            }
            set
            {
                this.HttpListenerResponse.SendChunked = value;
            }
        }

        public int StatusCode
        {
            get
            {
                return this.HttpListenerResponse.StatusCode;
            }
            set
            {
                this.HttpListenerResponse.StatusCode = value;
            }
        }

        public string StatusDescription
        {
            get
            {
                return this.HttpListenerResponse.StatusDescription;
            }
            set
            {
                this.HttpListenerResponse.StatusDescription = value;
            }
        }

        public void AddHeader(string name, string value)
        {
            this.HttpListenerResponse.AddHeader(name, value);
        }

        public void AppendCookie(Cookie cookie)
        {
            this.HttpListenerResponse.AppendCookie(cookie);
        }

        public void AppendHeader(string name, string value)
        {
            this.HttpListenerResponse.AppendHeader(name, value);
        }

        public void Redirect(string url)
        {
            this.HttpListenerResponse.Redirect(url);
        }

        public void SetCookie(Cookie cookie)
        {
            this.HttpListenerResponse.SetCookie(cookie);
        }

        #endregion

        #region IWriteable

        public void Write(byte[] data)
        {
            this.WriteStream.Write(data);
        }

        public void Write(Buffer buffer)
        {
            this.WriteStream.Write(buffer);
        }

        public void Write(string data)
        {
            this.WriteStream.Write(data);
        }

        public void Write(string format, object arg0)
        {
            this.WriteStream.Write(format, arg0);
        }

        public void Write(string format, params object[] args)
        {
            this.WriteStream.Write(format, args);
        }


        public void Write(string format, object arg0, object arg1)
        {
            this.WriteStream.Write(format, arg0, arg1);
        }

        public void Write(string format, object arg0, object arg1, object arg2)
        {
            this.WriteStream.Write(format, arg0, arg1, arg2);
        }

        public void Write(byte data)
        {
            this.WriteStream.Write(data);
        }

        public void Write(byte[] buffer, int index, int count)
        {
            this.WriteStream.Write(buffer, index, count);
        }

        public void Write(bool value)
        {
            this.WriteStream.Write(value);
        }

        public void Write(short value)
        {
            this.WriteStream.Write(value);
        }

        public void Write(ushort value)
        {
            this.WriteStream.Write(value);
        }

        public void Write(int value)
        {
            this.WriteStream.Write(value);
        }

        public void Write(uint value)
        {
            this.WriteStream.Write(value);
        }

        public void Write(long value)
        {
            this.WriteStream.Write(value);
        }

        public void Write(ulong value)
        {
            this.WriteStream.Write(value);
        }

        public void Write(float value)
        {
            this.WriteStream.Write(value);
        }

        public void Write(double value)
        {
            this.WriteStream.Write(value);
        }

        public void End()
        {
            this.WriteStream.End();
        }

        public event Action<Exception> OnError
        {
            add
            {
                this.WriteStream.OnError += value;
            }
            remove
            {
                this.WriteStream.OnError -= value;
            }
        }

        #endregion
    }
}
