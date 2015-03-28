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
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Reactor.Http
{
    public class ServerRequest : IReadable<Reactor.Buffer>
    {
        private Reactor.Http.Context             context;

        private Reactor.Net.HttpListenerRequest  request;

        private Stream               stream;

        private bool                 reading;

        private long                 received;

        private bool                 paused;

        private bool                 closed;

        internal ServerRequest(Reactor.Http.Context context, Reactor.Net.HttpListenerRequest request)
        {
            this.context                = context;

            this.request                = request;

            this.stream                 = this.request.InputStream;

            this.received               = 0;

            this.paused                 = true;

            this.closed                 = false;

            this.reading                = false;
        }

        #region HttpListenerRequest

        public string[] AcceptTypes
        {
            get
            {
                return this.request.AcceptTypes;
            }
        }

        public int ClientCertificateError
        {
            get
            {
                return this.request.ClientCertificateError;
            }
        }

        public Encoding ContentEncoding
        {
            get
            {
                return this.request.ContentEncoding;
            }
        }

        public long ContentLength
        {
            get
            {
                return this.request.ContentLength64;
            }
        }

        public string ContentType
        {
            get
            {
                return this.request.ContentType;
            }
        }

        public Net.CookieCollection Cookies
        {
            get
            {
                return this.request.Cookies;
            }
        }

        public bool HasEntityBody
        {
            get
            {
                return this.request.HasEntityBody;
            }
        }

        public NameValueCollection Headers
        {
            get
            {
                return this.request.Headers;
            }
        }

        public string Method
        {
            get
            {
                return this.request.HttpMethod;
            }
        }

        public bool IsAuthenticated
        {
            get
            {
                return this.request.IsAuthenticated;
            }
        }

        public bool IsLocal
        {
            get
            {
                return this.request.IsLocal;
            }
        }

        public bool IsSecureConnection
        {
            get
            {
                return this.request.IsSecureConnection;
            }
        }

        public bool KeepAlive
        {
            get
            {
                return this.request.KeepAlive;
            }
        }

        public IPEndPoint LocalEndPoint
        {
            get
            {
                return this.request.LocalEndPoint;
            }
        }

        public Version ProtocolVersion
        {
            get
            {
                return this.request.ProtocolVersion;
            }
        }

        public NameValueCollection QueryString
        {
            get
            {
                return this.request.QueryString;
            }
        }

        public string RawUrl
        {
            get
            {
                return this.request.RawUrl;
            }
        }

        public IPEndPoint RemoteEndPoint
        {
            get
            {
                return this.request.RemoteEndPoint;
            }
        }

        public Guid RequestTraceIdentifier
        {
            get
            {
                return this.request.RequestTraceIdentifier;
            }
        }

        public Uri Url
        {
            get
            {
                return this.request.Url;
            }
        }

        public Uri UrlReferrer
        {
            get
            {
                return this.request.UrlReferrer;
            }
        }

        public string UserAgent
        {
            get
            {
                return this.request.UserAgent;
            }
        }

        public string UserHostAddress
        {
            get
            {
                return this.request.UserHostAddress;
            }
        }

        public string UserHostName
        {
            get
            {
                return this.request.UserHostName;
            }
        }

        public string[] UserLanguages
        {
            get
            {
                return this.request.UserLanguages;
            }
        }

        #endregion

        #region Certificate

        public void Certificate(Action<Exception, X509Certificate2> OnCertificate)
        {
            //this.HttpListenerRequest.GetClientCertificate(OnCertificate);
        }

        #endregion

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

                    this.closed  = true;

                    return;
                }

                //----------------------------------------------
                // end of stream
                //----------------------------------------------
                if (read == 0 || read == -1)
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
