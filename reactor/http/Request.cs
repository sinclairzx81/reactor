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
using System.Net;
using System.Net.Cache;
using System.Security.Cryptography.X509Certificates;

namespace Reactor.Http
{
    public class Request : IWriteable
    {
        private HttpWebRequest         HttpWebRequest         { get; set; }

        private Reactor.Buffer         WriteBuffer            { get; set; }

        private WriteStream            WriteStream            { get; set; }

        private Action<Response>       OnResponse             { get; set; }

        #region Constructor

        public Request(string Url, Action<Response> OnResponse)
        {
            this.OnResponse             = OnResponse;

            this.WriteBuffer            = Reactor.Buffer.Create();

            this.HttpWebRequest         = (HttpWebRequest)WebRequest.Create(Url);
        }

        #endregion

        #region HttpWebRequest

        public string Accept
        {
            get
            {
                return this.HttpWebRequest.Accept;
            }
            set
            {
                this.HttpWebRequest.Accept = value;
            }
        }

        public Uri Address
        {
            get
            {
                return this.HttpWebRequest.Address;
            }
        }

        public bool AllowAutoRedirect
        {
            get
            {
                return this.HttpWebRequest.AllowAutoRedirect;
            }
            set
            {
                this.HttpWebRequest.AllowAutoRedirect = value;
            }
        }

        public bool AllowWriteStreamBuffering
        {
            get
            {
                return this.HttpWebRequest.AllowWriteStreamBuffering;
            }
            set
            {
                this.HttpWebRequest.AllowWriteStreamBuffering = value;
            }
        }

        public DecompressionMethods AutomaticDecompression
        {
            get
            {
                return this.HttpWebRequest.AutomaticDecompression;
            }
            set
            {
                this.HttpWebRequest.AutomaticDecompression = value;
            }
        }

        public X509CertificateCollection ClientCertificates
        {
            get
            {
                return this.HttpWebRequest.ClientCertificates;
            }
            set
            {
                this.HttpWebRequest.ClientCertificates = value;
            }
        }

        public string Connection
        {
            get
            {
                return this.HttpWebRequest.Connection;
            }
            set
            {
                this.HttpWebRequest.Connection = value;
            }
        }

        public string ConnectionGroupName
        {
            get
            {
                return this.HttpWebRequest.ConnectionGroupName;
            }
            set
            {
                this.HttpWebRequest.ConnectionGroupName = value;
            }
        }

        public long ContentLength
        {
            get
            {
                return this.HttpWebRequest.ContentLength;
            }
            set
            {
                this.HttpWebRequest.ContentLength = value;
            }
        }

        public string ContentType
        {
            get
            {
                return this.HttpWebRequest.ContentType;
            }
            set
            {
                this.HttpWebRequest.ContentType = value;
            }
        }

        public HttpContinueDelegate ContinueDelegate
        {
            get
            {
                return this.HttpWebRequest.ContinueDelegate;
            }
            set
            {
                this.HttpWebRequest.ContinueDelegate = value;
            }
        }

        public CookieContainer CookieContainer
        {
            get
            {
                return this.HttpWebRequest.CookieContainer;
            }
            set
            {
                this.HttpWebRequest.CookieContainer = value;
            }
        }

        public ICredentials Credentials
        {
            get
            {
                return this.HttpWebRequest.Credentials;
            }
            set
            {
                this.HttpWebRequest.Credentials = value;
            }
        }

        public static RequestCachePolicy DefaultCachePolicy
        {
            get
            {
                return HttpWebRequest.DefaultCachePolicy;
            }
            set
            {
                HttpWebRequest.DefaultCachePolicy = value;
            }
        }

        public static int DefaultMaximumErrorResponseLength
        {
            get
            {
                return HttpWebRequest.DefaultMaximumErrorResponseLength;
            }
            set
            {
                HttpWebRequest.DefaultMaximumErrorResponseLength = value;
            }
        }

        public static int DefaultMaximumResponseHeadersLength
        {
            get
            {
                return HttpWebRequest.DefaultMaximumResponseHeadersLength;
            }
            set
            {
                HttpWebRequest.DefaultMaximumResponseHeadersLength = value;
            }
        }

        public string Expect
        {
            get
            {
                return this.HttpWebRequest.Expect;
            }
            set
            {
                this.HttpWebRequest.Expect = value;
            }
        }

        public WebHeaderCollection Headers
        {
            get
            {
                return this.HttpWebRequest.Headers;
            }
            set
            {
                this.HttpWebRequest.Headers = value;
            }
        }

        public DateTime IfModifiedSince
        {
            get
            {
                return this.HttpWebRequest.IfModifiedSince;
            }
            set
            {
                this.HttpWebRequest.IfModifiedSince = value;
            }
        }

        public bool KeepAlive
        {
            get
            {
                return this.HttpWebRequest.KeepAlive;
            }
            set
            {
                this.HttpWebRequest.KeepAlive = value;
            }
        }

        public int MaximumAutomaticRedirections
        {
            get
            {
                return this.HttpWebRequest.MaximumAutomaticRedirections;
            }
            set
            {
                this.HttpWebRequest.MaximumAutomaticRedirections = value;
            }
        }

        public int MaximumResponseHeadersLength
        {
            get
            {
                return this.HttpWebRequest.MaximumResponseHeadersLength;
            }
            set
            {
                this.HttpWebRequest.MaximumResponseHeadersLength = value;
            }
        }

        public string MediaType
        {
            get
            {
                return this.HttpWebRequest.MediaType;
            }
            set
            {
                this.HttpWebRequest.MediaType = value;
            }
        }

        public string Method
        {
            get
            {
                return this.HttpWebRequest.Method;
            }
            set
            {
                this.HttpWebRequest.Method = value;
            }
        }

        public bool Pipelined
        {
            get
            {
                return this.HttpWebRequest.Pipelined;
            }
            set
            {
                this.HttpWebRequest.Pipelined = value;
            }
        }

        public bool PreAuthenticate
        {
            get
            {
                return this.HttpWebRequest.PreAuthenticate;
            }
            set
            {
                this.HttpWebRequest.PreAuthenticate = value;
            }
        }

        public Version ProtocolVersion
        {
            get
            {
                return this.HttpWebRequest.ProtocolVersion;
            }
            set
            {
                this.HttpWebRequest.ProtocolVersion = value;
            }
        }

        public IWebProxy Proxy
        {
            get
            {
                return this.HttpWebRequest.Proxy;
            }
            set
            {
                this.HttpWebRequest.Proxy = value;
            }
        }

        public int ReadWriteTimeout
        {
            get
            {
                return this.HttpWebRequest.ReadWriteTimeout;
            }
            set
            {
                this.HttpWebRequest.ReadWriteTimeout = value;
            }
        }

        public string Referer
        {
            get
            {
                return this.HttpWebRequest.Referer;
            }
            set
            {
                this.HttpWebRequest.Referer = value;
            }
        }

        public Uri RequestUri
        {
            get
            {
                return this.HttpWebRequest.RequestUri;
            }
        }

        public bool SendChunked
        {
            get
            {
                return this.HttpWebRequest.SendChunked;
            }
            set
            {
                this.HttpWebRequest.SendChunked = value;
            }
        }

        public ServicePoint ServicePoint
        {
            get
            {
                return this.HttpWebRequest.ServicePoint;
            }
        }

        public int Timeout
        {
            get
            {
                return this.HttpWebRequest.Timeout;
            }
            set
            {
                this.HttpWebRequest.Timeout = value;
            }
        }

        public string TransferEncoding
        {
            get
            {
                return this.HttpWebRequest.TransferEncoding;
            }
            set
            {
                this.HttpWebRequest.TransferEncoding = value;
            }
        }

        public bool UnsafeAuthenticatedConnectionSharing
        {
            get
            {
                return this.HttpWebRequest.UnsafeAuthenticatedConnectionSharing;
            }
            set
            {
                this.HttpWebRequest.UnsafeAuthenticatedConnectionSharing = value;
            }
        }

        public bool UseDefaultCredentials
        {
            get
            {
                return this.HttpWebRequest.UseDefaultCredentials;
            }
            set
            {
                this.HttpWebRequest.UseDefaultCredentials = value;
            }
        }

        public string UserAgent
        {
            get
            {
                return this.HttpWebRequest.UserAgent;
            }
            set
            {
                this.HttpWebRequest.UserAgent = value;
            }
        }

        #endregion

        #region IWriteStream

        public event Action<Exception> OnError;

        public void Write(byte[] data)
        {
            this.WriteBuffer.Write(data);
        }

        public void Write(Buffer buffer)
        {
            this.WriteBuffer.Write(buffer);
        }

        public void Write(string data)
        {
            this.WriteBuffer.Write(data);
        }

        public void Write(string format, object arg0)
        {
            this.WriteBuffer.Write(format, arg0);
        }

        public void Write(string format, params object[] args)
        {
            this.WriteBuffer.Write(format, args);
        }

        public void Write(string format, object arg0, object arg1)
        {
            this.WriteBuffer.Write(format, arg0, arg1);
        }

        public void Write(string format, object arg0, object arg1, object arg2)
        {
            this.WriteBuffer.Write(format, arg0, arg1, arg2);
        }

        public void Write(byte data)
        {
            this.WriteBuffer.Write(data);
        }

        public void Write(byte[] buffer, int index, int count)
        {
            this.WriteBuffer.Write(buffer, index, count);
        }

        public void Write(bool value)
        {
            this.WriteBuffer.Write(value);
        }

        public void Write(short value)
        {
            this.WriteBuffer.Write(value);
        }

        public void Write(ushort value)
        {
            this.WriteBuffer.Write(value);
        }

        public void Write(int value)
        {
            this.WriteBuffer.Write(value);
        }

        public void Write(uint value)
        {
            this.WriteBuffer.Write(value);
        }

        public void Write(long value)
        {
            this.WriteBuffer.Write(value);
        }

        public void Write(ulong value)
        {
            this.WriteBuffer.Write(value);
        }

        public void Write(float value)
        {
            this.WriteBuffer.Write(value);
        }

        public void Write(double value)
        {
            this.WriteBuffer.Write(value);
        }

        #endregion

        public void End()
        {
            if(this.WriteBuffer.Length > 0) {

                this.GetRequestStream((stream) => {
                    
                    var writestream = new Reactor.WriteStream(stream);

                    writestream.Write(this.WriteBuffer);

                    writestream.End(() => {

                        this.GetResponse();
                    });
                });

                return;
            }

            this.GetResponse();
        }

        #region GetRequestStream

        private void GetRequestStream(Reactor.Action<Stream> callback)
        {

            this.HttpWebRequest.BeginGetRequestStream((Result) => {

                try {

                    var stream = this.HttpWebRequest.EndGetRequestStream(Result);

                    callback(stream);
                }
                catch (Exception exception)
                {
                    Loop.Post(() =>
                    {
                        if (this.OnError != null) {

                            this.OnError(exception);
                        }
                    });
                }

            }, null);
        }

        #endregion

        #region GetResponse

        private void GetResponse()
        {
            // fix - resolve uninitialized
            // content length in instances
            // where the client is POSTing,
            // but isn't sending data.
            if (this.HttpWebRequest.Method != "GET") {

                if (this.HttpWebRequest.ContentLength == -1) {

                    this.HttpWebRequest.ContentLength = 0;
                }
            }

            this.HttpWebRequest.BeginGetResponse((Result) => {

                try
                {
                    var response = new Response((HttpWebResponse)this.HttpWebRequest.EndGetResponse(Result));

                    Loop.Post(() =>
                    {
                        this.OnResponse(response);
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
                    });
                }
                  

            }, null);
  
        }

        #endregion

        #region Statics

        public static Request Create(string Url, Action<Response> OnResponse)
        {
            return new Request(Url, OnResponse);
        }

        #endregion
    }
}
