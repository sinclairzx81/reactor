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
using System.Collections.Specialized;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Reactor.Http
{
    public class ServerRequest : IReadable
    {
        private HttpListenerRequest  HttpListenerRequest   { get; set; }

        private ReadStream           ReadStream            { get; set; }

        internal ServerRequest(HttpListenerRequest HttpListenerRequest)
        {
            this.HttpListenerRequest    = HttpListenerRequest;

            this.ReadStream             = new ReadStream(this.HttpListenerRequest.InputStream, true);
        }

        #region HttpListenerRequest

        public string[] AcceptTypes
        {
            get
            {
                return this.HttpListenerRequest.AcceptTypes;
            }
        }

        public int ClientCertificateError
        {
            get
            {
                return this.HttpListenerRequest.ClientCertificateError;
            }
        }

        public Encoding ContentEncoding
        {
            get
            {
                return this.HttpListenerRequest.ContentEncoding;
            }
        }

        public long ContentLength
        {
            get
            {
                return this.HttpListenerRequest.ContentLength64;
            }
        }

        public string ContentType
        {
            get
            {
                return this.HttpListenerRequest.ContentType;
            }
        }

        public CookieCollection Cookies
        {
            get
            {
                return this.HttpListenerRequest.Cookies;
            }
        }

        public bool HasEntityBody
        {
            get
            {
                return this.HttpListenerRequest.HasEntityBody;
            }
        }

        public NameValueCollection Headers
        {
            get
            {
                return this.HttpListenerRequest.Headers;
            }
        }

        public string Method
        {
            get
            {
                return this.HttpListenerRequest.HttpMethod;
            }
        }

        public bool IsAuthenticated
        {
            get
            {
                return this.HttpListenerRequest.IsAuthenticated;
            }
        }

        public bool IsLocal
        {
            get
            {
                return this.HttpListenerRequest.IsLocal;
            }
        }

        public bool IsSecureConnection
        {
            get
            {
                return this.HttpListenerRequest.IsSecureConnection;
            }
        }

        public bool KeepAlive
        {
            get
            {
                return this.HttpListenerRequest.KeepAlive;
            }
        }

        public IPEndPoint LocalEndPoint
        {
            get
            {
                return this.HttpListenerRequest.LocalEndPoint;
            }
        }

        public Version ProtocolVersion
        {
            get
            {
                return this.HttpListenerRequest.ProtocolVersion;
            }
        }

        public NameValueCollection QueryString
        {
            get
            {
                return this.HttpListenerRequest.QueryString;
            }
        }

        public string RawUrl
        {
            get
            {
                return this.HttpListenerRequest.RawUrl;
            }
        }

        public IPEndPoint RemoteEndPoint
        {
            get
            {
                return this.HttpListenerRequest.RemoteEndPoint;
            }
        }

        public Guid RequestTraceIdentifier
        {
            get
            {
                return this.HttpListenerRequest.RequestTraceIdentifier;
            }
        }

        public Uri Url
        {
            get
            {
                return this.HttpListenerRequest.Url;
            }
        }

        public Uri UrlReferrer
        {
            get
            {
                return this.HttpListenerRequest.UrlReferrer;
            }
        }

        public string UserAgent
        {
            get
            {
                return this.HttpListenerRequest.UserAgent;
            }
        }

        public string UserHostAddress
        {
            get
            {
                return this.HttpListenerRequest.UserHostAddress;
            }
        }

        public string UserHostName
        {
            get
            {
                return this.HttpListenerRequest.UserHostName;
            }
        }

        public string[] UserLanguages
        {
            get
            {
                return this.HttpListenerRequest.UserLanguages;
            }
        }

        #endregion

        #region Certificate

        public void Certificate(Action<Exception, X509Certificate2> OnCertificate)
        {
            this.HttpListenerRequest.BeginGetClientCertificate((result) =>
            {
                try
                {
                    var certificate = this.HttpListenerRequest.EndGetClientCertificate(result);

                    Loop.Post(() =>
                    {
                        OnCertificate(null, certificate);
                    });
                }
                catch (Exception exception)
                {
                    Loop.Post(() =>
                    {
                        OnCertificate(exception, null);
                    });
                }

            }, null);
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
