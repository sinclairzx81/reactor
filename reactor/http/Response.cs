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

namespace Reactor.Http
{
    public class Response : IReadable
    {
        private HttpWebResponse   HttpWebResponse        { get; set; }

        private ReadStream        ReadStream             { get; set; }

        internal Response(HttpWebResponse HttpWebResponse)
        {
            this.HttpWebResponse        = HttpWebResponse;

            this.ReadStream             = new ReadStream(this.HttpWebResponse.GetResponseStream(), true);
        }

        #region HttpWebResponse

        public string CharacterSet 
        {
            get
            {
                return this.HttpWebResponse.CharacterSet;
            }
        }

        public string ContentEncoding
        {
            get
            {
                return this.HttpWebResponse.ContentEncoding;
            }
        }

        public long ContentLength
        {
            get
            {
                return this.HttpWebResponse.ContentLength;
            }
        }

        public string ContentType
        {
            get
            {
                return this.HttpWebResponse.ContentType;
            }
        }

        public CookieCollection Cookies
        {
            get
            {
                return this.HttpWebResponse.Cookies;
            }
            set
            {
                this.HttpWebResponse.Cookies = value;
            }
        }

        public WebHeaderCollection Headers
        {
            get
            {
                return this.HttpWebResponse.Headers;
            }
        }

        public bool IsMutuallyAuthenticated
        {
            get
            {
                return this.HttpWebResponse.IsMutuallyAuthenticated;
            }
        }


        public DateTime LastModified
        {
            get
            {
                return this.HttpWebResponse.LastModified;
            }
        }

        public string Method
        {
            get
            {
                return this.HttpWebResponse.Method;
            }
        }


        public Version ProtocolVersion
        {
            get
            {
                return this.HttpWebResponse.ProtocolVersion;
            }
        }

        public Uri ResponseUri
        {
            get
            {
                return this.HttpWebResponse.ResponseUri;
            }
        }

        public string Server
        {
            get
            {
                return this.HttpWebResponse.Server;
            }
        }


        public HttpStatusCode StatusCode
        {
            get
            {
                return this.HttpWebResponse.StatusCode;
            }
        }

        public string StatusDescription
        {
            get
            {
                return this.HttpWebResponse.StatusDescription;
            }
        }
        #endregion

        #region IReadStream

        public event Action<Exception> OnError 
        {
            add 
            {
                this.ReadStream.OnError += value;
            }
            remove 
            {
                this.ReadStream.OnError -= value;
            }
        }

        public event Action<Buffer> OnData
        {
            add
            {
                this.ReadStream.OnData += value;
            }
            remove
            {
                this.ReadStream.OnData -= value;
            }
        }

        public event Action OnEnd
        {
            add
            {
                this.ReadStream.OnEnd += value;
            }
            remove
            {
                this.ReadStream.OnEnd -= value;
            }
        }

        public event Action OnClose
        {
            add
            {
                this.ReadStream.OnClose += value;
            }
            remove
            {
                this.ReadStream.OnClose -= value;
            }
        }

        public IReadable Pipe(IWriteable writeable)
        {
            return this.ReadStream.Pipe(writeable);
        }

        public void Resume()
        {
            this.ReadStream.Resume();
        }

        public void Pause()
        {
            this.ReadStream.Pause();
        }

        #endregion
    }
}
