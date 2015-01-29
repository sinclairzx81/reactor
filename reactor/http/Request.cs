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
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Security.Cryptography.X509Certificates;

namespace Reactor.Http
{
    public class Request : IWriteable<Reactor.Buffer>
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

        internal class GetRequestStreamCommand : Command 
        {
            public GetRequestStreamCommand(Action<Exception> callback)
            {
                this.Callback = callback;
            }
        }

        internal class GetResponseCommand : Command
        {
            public GetResponseCommand(Action<Exception> callback)
            {
                this.Callback = callback;
            }
        }

        #endregion

        private HttpWebRequest   httpwebrequest;

        private System.IO.Stream stream;

        private Queue<Command>   commands;

        private bool             writing;

        private bool             ended;

        private Action<Response> onresponse;

        private bool             requestedStream;

        #region Constructor

        public Request(string Url, Action<Response> OnResponse)
        {
            this.onresponse            = OnResponse;

            this.httpwebrequest        = (HttpWebRequest)WebRequest.Create(Url);

            this.stream                = null;

            this.commands              = new Queue<Command>();

            this.ended                 = false;

            this.writing               = false;

            this.requestedStream       = false;
        }

        #endregion

        #region HttpWebRequest

        public string Accept
        {
            get
            {
                return this.httpwebrequest.Accept;
            }
            set
            {
                this.httpwebrequest.Accept = value;
            }
        }

        public Uri Address
        {
            get
            {
                return this.httpwebrequest.Address;
            }
        }

        public bool AllowAutoRedirect
        {
            get
            {
                return this.httpwebrequest.AllowAutoRedirect;
            }
            set
            {
                this.httpwebrequest.AllowAutoRedirect = value;
            }
        }

        public bool AllowWriteStreamBuffering
        {
            get
            {
                return this.httpwebrequest.AllowWriteStreamBuffering;
            }
            set
            {
                this.httpwebrequest.AllowWriteStreamBuffering = value;
            }
        }

        public DecompressionMethods AutomaticDecompression
        {
            get
            {
                return this.httpwebrequest.AutomaticDecompression;
            }
            set
            {
                this.httpwebrequest.AutomaticDecompression = value;
            }
        }

        public X509CertificateCollection ClientCertificates
        {
            get
            {
                return this.httpwebrequest.ClientCertificates;
            }
            set
            {
                this.httpwebrequest.ClientCertificates = value;
            }
        }

        public string Connection
        {
            get
            {
                return this.httpwebrequest.Connection;
            }
            set
            {
                this.httpwebrequest.Connection = value;
            }
        }

        public string ConnectionGroupName
        {
            get
            {
                return this.httpwebrequest.ConnectionGroupName;
            }
            set
            {
                this.httpwebrequest.ConnectionGroupName = value;
            }
        }

        public long ContentLength
        {
            get
            {
                return this.httpwebrequest.ContentLength;
            }
            set
            {
                this.httpwebrequest.ContentLength = value;
            }
        }

        public string ContentType
        {
            get
            {
                return this.httpwebrequest.ContentType;
            }
            set
            {
                this.httpwebrequest.ContentType = value;
            }
        }

        public HttpContinueDelegate ContinueDelegate
        {
            get
            {
                return this.httpwebrequest.ContinueDelegate;
            }
            set
            {
                this.httpwebrequest.ContinueDelegate = value;
            }
        }

        public CookieContainer CookieContainer
        {
            get
            {
                return this.httpwebrequest.CookieContainer;
            }
            set
            {
                this.httpwebrequest.CookieContainer = value;
            }
        }

        public ICredentials Credentials
        {
            get
            {
                return this.httpwebrequest.Credentials;
            }
            set
            {
                this.httpwebrequest.Credentials = value;
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
                return this.httpwebrequest.Expect;
            }
            set
            {
                this.httpwebrequest.Expect = value;
            }
        }

        public WebHeaderCollection Headers
        {
            get
            {
                return this.httpwebrequest.Headers;
            }
            set
            {
                this.httpwebrequest.Headers = value;
            }
        }

        public DateTime IfModifiedSince
        {
            get
            {
                return this.httpwebrequest.IfModifiedSince;
            }
            set
            {
                this.httpwebrequest.IfModifiedSince = value;
            }
        }

        public bool KeepAlive
        {
            get
            {
                return this.httpwebrequest.KeepAlive;
            }
            set
            {
                this.httpwebrequest.KeepAlive = value;
            }
        }

        public int MaximumAutomaticRedirections
        {
            get
            {
                return this.httpwebrequest.MaximumAutomaticRedirections;
            }
            set
            {
                this.httpwebrequest.MaximumAutomaticRedirections = value;
            }
        }

        public int MaximumResponseHeadersLength
        {
            get
            {
                return this.httpwebrequest.MaximumResponseHeadersLength;
            }
            set
            {
                this.httpwebrequest.MaximumResponseHeadersLength = value;
            }
        }

        public string MediaType
        {
            get
            {
                return this.httpwebrequest.MediaType;
            }
            set
            {
                this.httpwebrequest.MediaType = value;
            }
        }

        public string Method
        {
            get
            {
                return this.httpwebrequest.Method;
            }
            set
            {
                this.httpwebrequest.Method = value;
            }
        }

        public bool Pipelined
        {
            get
            {
                return this.httpwebrequest.Pipelined;
            }
            set
            {
                this.httpwebrequest.Pipelined = value;
            }
        }

        public bool PreAuthenticate
        {
            get
            {
                return this.httpwebrequest.PreAuthenticate;
            }
            set
            {
                this.httpwebrequest.PreAuthenticate = value;
            }
        }

        public Version ProtocolVersion
        {
            get
            {
                return this.httpwebrequest.ProtocolVersion;
            }
            set
            {
                this.httpwebrequest.ProtocolVersion = value;
            }
        }

        public IWebProxy Proxy
        {
            get
            {
                return this.httpwebrequest.Proxy;
            }
            set
            {
                this.httpwebrequest.Proxy = value;
            }
        }

        public int ReadWriteTimeout
        {
            get
            {
                return this.httpwebrequest.ReadWriteTimeout;
            }
            set
            {
                this.httpwebrequest.ReadWriteTimeout = value;
            }
        }

        public string Referer
        {
            get
            {
                return this.httpwebrequest.Referer;
            }
            set
            {
                this.httpwebrequest.Referer = value;
            }
        }

        public Uri RequestUri
        {
            get
            {
                return this.httpwebrequest.RequestUri;
            }
        }

        /// <summary>
        /// Send Chunked: Warning: possible issues using this. Mono HttpListener OnEnd not fired when sending chunked from this client.
        /// </summary>
        public bool SendChunked
        {
            get
            {
                return this.httpwebrequest.SendChunked;
            }
            set
            {
                this.httpwebrequest.SendChunked = value;
            }
        }

        public ServicePoint ServicePoint
        {
            get
            {
                return this.httpwebrequest.ServicePoint;
            }
        }

        public int Timeout
        {
            get
            {
                return this.httpwebrequest.Timeout;
            }
            set
            {
                this.httpwebrequest.Timeout = value;
            }
        }

        public string TransferEncoding
        {
            get
            {
                return this.httpwebrequest.TransferEncoding;
            }
            set
            {
                this.httpwebrequest.TransferEncoding = value;
            }
        }

        public bool UnsafeAuthenticatedConnectionSharing
        {
            get
            {
                return this.httpwebrequest.UnsafeAuthenticatedConnectionSharing;
            }
            set
            {
                this.httpwebrequest.UnsafeAuthenticatedConnectionSharing = value;
            }
        }

        public bool UseDefaultCredentials
        {
            get
            {
                return this.httpwebrequest.UseDefaultCredentials;
            }
            set
            {
                this.httpwebrequest.UseDefaultCredentials = value;
            }
        }

        public string UserAgent
        {
            get
            {
                return this.httpwebrequest.UserAgent;
            }
            set
            {
                this.httpwebrequest.UserAgent = value;
            }
        }

        #endregion

        #region IWriteable

        public void Write(Buffer buffer, Action<Exception> callback)
        {
            if(!this.requestedStream)
            {
                this.requestedStream = true;

                this.commands.Enqueue(new GetRequestStreamCommand((exception) => {
                    
                    if (!this.writing)
                    {   
                        this.writing = true;

                        if (!this.ended)
                        {
                            this.Write();
                        }
                    }                    

                }));
            }

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
            if(this.requestedStream)
            {
                this.commands.Enqueue(new EndCommand(callback));
            }
            
            this.commands.Enqueue(new GetResponseCommand(callback));

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
            // command: get request stream
            //----------------------------------

            if(command is GetRequestStreamCommand)
            {
                var getrequeststream = command as GetRequestStreamCommand;

                IO.GetRequestStream(this.httpwebrequest, (exception, stream) =>
                {
                    getrequeststream.Callback(exception);

                    if (exception != null)
                    {
                        if (this.OnError != null)
                        {
                            this.OnError(exception);
                        }

                        return;
                    }

                    this.stream = stream;

                    if (this.commands.Count > 0)
                    {
                        this.Write();

                        return;
                    }
                });
            }

            //----------------------------------
            // command: write
            //----------------------------------

            if (command is WriteCommand)
            {
                var write = command as WriteCommand;
                
                IO.Write(this.stream, write.Buffer.ToArray(), (exception) =>
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
                    if (this.stream != null)
                    {
                        this.stream.Close();
                    }

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

                this.ended  = true;

                if (this.commands.Count > 0)
                {
                    this.Write();

                    return;
                }
            }

            //----------------------------------
            // command: get response
            //----------------------------------

            if (command is GetResponseCommand)
            {
                var getresponse = command as GetResponseCommand;

                // fix - resolve uninitialized
                // content length in instances
                // where the client is POSTing,
                // but isn't sending data.
                try
                {
                    if (this.httpwebrequest.Method != "GET")
                    {
                        if (this.httpwebrequest.ContentLength == -1)
                        {
                            this.httpwebrequest.ContentLength = 0;
                        }
                    }
                }
                catch {}
             

                IO.GetResponse(this.httpwebrequest, (exception, response) =>
                {
                    getresponse.Callback(exception);

                    if (exception != null)
                    {
                        if (this.OnError != null)
                        {
                            this.OnError(exception);
                        }

                        this.ended = true;

                        return;
                    }

                    if(this.onresponse != null)
                    {
                        this.onresponse(new Reactor.Http.Response(response));
                    }
                });
            }
        }

        #region Statics

        public static Request Create(string Url, Action<Response> OnResponse)
        {
            return new Request(Url, OnResponse);
        }

        public static void Get(string Url, string contentType, IDictionary<string, string> headers, Action<Exception, Buffer> callback)
        {
            var request = Reactor.Http.Request.Create(Url, (response) =>
            {
                var buffer_ = Reactor.Buffer.Create();

                response.OnData  += buffer_.Write;

                response.OnEnd   += () => callback(null, buffer_);

                response.OnError += (e) => callback(e, null);
            });

            request.OnError += (e) => callback(e, null);

            foreach (var pair in headers)
            {
                request.Headers[pair.Key] = pair.Value;
            }

            request.ContentType = contentType;

            request.End();
        }

        public static void Get(string Url, string contentType, Action<Exception, Buffer> callback)
        {
            Get(Url, contentType, new Dictionary<string, string>(), callback);
        }

        public static void Get(string Url, Action<Exception, Buffer> callback)
        {
            Get(Url, "application/octet-stream", new Dictionary<string, string>(), callback);
        }

        public static void Post(string Url, string contentType, IDictionary<string, string> headers, Buffer buffer, Action<Exception, Buffer> callback)
        {
            var request = Reactor.Http.Request.Create(Url, (response) =>
            {
                var buffer_ = Reactor.Buffer.Create();

                response.OnData += buffer_.Write;

                response.OnEnd += () => callback(null, buffer_);

                response.OnError += (e) => callback(e, null);
            });

            request.OnError += (e) => callback(e, null);

            foreach (var pair in headers)
            {
                request.Headers[pair.Key] = pair.Value;
            }

            request.Method = "POST";

            request.ContentType = contentType;

            request.ContentLength = buffer.Length;

            request.Write(buffer);

            request.End();
        }

        public static void Post(string Url, string contentType, Reactor.Buffer buffer, Action<Exception, Buffer> callback)
        {
            Post(Url, contentType, new Dictionary<string, string>(), buffer, callback);
        }

        public static void Post(string Url, Reactor.Buffer buffer, Action<Exception, Buffer> callback)
        {
            Post(Url, "application/octet-stream", new Dictionary<string, string>(), buffer, callback);
        }

        public static void Put(string Url, string contentType, IDictionary<string, string> headers, Buffer buffer, Action<Exception, Buffer> callback)
        {
            var request = Reactor.Http.Request.Create(Url, (response) =>
            {
                var buffer_ = Reactor.Buffer.Create();

                response.OnData += buffer_.Write;

                response.OnEnd += () => callback(null, buffer_);

                response.OnError += (e) => callback(e, null);
            });

            request.OnError += (e) => callback(e, null);

            foreach (var pair in headers)
            {
                request.Headers[pair.Key] = pair.Value;
            }

            request.Method = "PUT";

            request.ContentType = contentType;

            request.ContentLength = buffer.Length;

            request.Write(buffer);

            request.End();
        }

        public static void Put(string Url, string contentType, Reactor.Buffer buffer, Action<Exception, Buffer> callback)
        {
            Put(Url, contentType, new Dictionary<string, string>(), buffer, callback);
        }

        public static void Put(string Url, Reactor.Buffer buffer, Action<Exception, Buffer> callback)
        {
            Put(Url, "application/octet-stream", new Dictionary<string, string>(), buffer, callback);
        }


        public static void Delete(string Url, string contentType, IDictionary<string, string> headers, Buffer buffer, Action<Exception, Buffer> callback)
        {
            var request = Reactor.Http.Request.Create(Url, (response) =>
            {
                var buffer_ = Reactor.Buffer.Create();

                response.OnData += buffer_.Write;

                response.OnEnd += () => callback(null, buffer_);

                response.OnError += (e) => callback(e, null);
            });

            request.OnError += (e) => callback(e, null);

            foreach (var pair in headers)
            {
                request.Headers[pair.Key] = pair.Value;
            }

            request.Method = "DELETE";

            request.ContentType = contentType;

            request.ContentLength = buffer.Length;

            request.Write(buffer);

            request.End();
        }

        public static void Delete(string Url, string contentType, Reactor.Buffer buffer, Action<Exception, Buffer> callback)
        {
            Delete(Url, contentType, new Dictionary<string, string>(), buffer, callback);
        }

        public static void Delete(string Url, Reactor.Buffer buffer, Action<Exception, Buffer> callback)
        {
            Delete(Url, "application/octet-stream", new Dictionary<string, string>(), buffer, callback);
        }

        #endregion

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
