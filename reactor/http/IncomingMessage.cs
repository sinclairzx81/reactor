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
using System.Globalization;
using System.Net;
using System.Text;

namespace Reactor.Http {

    /// <summary>
    /// Reactor HTTP Incoming Message
    /// </summary>
    public class IncomingMessage : Reactor.IReadable {
        private Reactor.Tcp.Socket                  socket;
        private Reactor.Async.Event                 onreadable;
        private Reactor.Async.Event<Reactor.Buffer> onread;
        private Reactor.Async.Event<Exception>      onerror;
        private Reactor.Async.Event                 onend;
        private Reactor.Http.Headers                headers;
        private Reactor.Http.Query                  query;
        private Version                             version;
        private string                              method;
        private string                              raw_url;
        private Uri                                 url;
        private long                                contentLength;
        private Encoding                            contentEncoding;
        private string[]                            acceptTypes;
        private string[]                            userLanguages;
        
        private int                                 received;
        private bool                                ended;

        #region Constructors

        internal IncomingMessage(Reactor.Tcp.Socket socket) {
            this.socket          = socket;
            this.onreadable      = new Reactor.Async.Event();
            this.onread          = new Reactor.Async.Event<Reactor.Buffer>();
            this.onerror         = new Reactor.Async.Event<Exception>();
            this.onend           = new Reactor.Async.Event();
            this.headers         = new Headers();
            this.query           = new Query();
            this.contentEncoding = Encoding.Default;
            this.received        = 0;
            this.ended           = false;
        }

        #endregion

        #region Properties

        /// <summary>
        /// HTTP Headers.
        /// </summary>
        public Headers Headers {
            get {  return this.headers; }
        }

        /// <summary>
        /// QueryString parameters.
        /// </summary>
        public Query Query {
            get { return this.query; }
        }

        /// <summary>
        /// The HTTP Protocol version.
        /// </summary>
        public Version Version {
            get {  return this.version; }
        }

        /// <summary>
        /// The HTTP Verb.
        /// </summary>
        public string Method {
            get { return this.method; }
        }

        /// <summary>
        /// The raw url read from the request line.
        /// </summary>
        public string RawUrl {
            get {  return this.raw_url; }
        }

        /// <summary>
        /// The parsed url.
        /// </summary>
        public Uri Url {
            get { return this.url; }
        }

        /// <summary>
        /// The Content-Length header.
        /// </summary>
        public long ContentLength {
            get { return this.contentLength; }
        }

        /// <summary>
        /// The Content-Encoding header.
        /// </summary>
        public Encoding ContentEncoding {
            get {  return this.contentEncoding; }
        }

        /// <summary>
        /// The Accept-Types header.
        /// </summary>
        public string[] AcceptTypes     {
            get {  return this.acceptTypes; }
        }

        /// <summary>
        /// The User-Languages header.
        /// </summary>
        public string[] UserLanguages   {
            get {  return this.userLanguages; }
        }

        /// <summary>
        /// The local endpoint.
        /// </summary>
        public EndPoint LocalEndPoint   {
            get { return this.socket.LocalEndPoint; }
        }

        /// <summary>
        /// The remote endpoint.
        /// </summary>
        public EndPoint RemoteEndPoint  {
            get { return this.socket.RemoteEndPoint; }
        }

        /// <summary>
        /// The HTTP referer header.
        /// </summary>
        public string   Referer {
            get { return this.headers["referer"] ?? string.Empty; }
        }

        /// <summary>
        /// The Content-Type header.
        /// </summary>
        public string ContentType {
            get { return this.headers["content-type"] ?? string.Empty; }
        }
        
        /// <summary>
        /// The Host header.
        /// </summary>
        public string UserHostName {
            get { return this.headers["host"] ?? string.Empty; }
        }

        /// <summary>
        /// The User-Agent header.
        /// </summary>
        public string UserAgent {
            get { return headers["user-agent"] ?? string.Empty; }
        }

        #endregion

        #region Events

        

        public Reactor.Buffer Read () {
            return this.socket.Read();
        }

        public Reactor.Buffer Read (int count) {
            return this.socket.Read(count);
        }


        public void OnReadable (Reactor.Action callback) {
            this.onreadable.On(callback);

        }
        public void OnRead (Reactor.Action<Reactor.Buffer> callback) {
            this.onread.On(callback);
            this.socket.Resume();
        }
        public void RemoveRead (Reactor.Action<Reactor.Buffer> callback) {
            this.onread.Remove(callback);
        }

        public void OnError (Reactor.Action<Exception> callback) {
            this.onerror.On(callback);
        }

        public void OnEnd (Reactor.Action callback) {
            this.onend.On(callback);
        }

        public void RemoveReadable (Reactor.Action callback) {
            this.onreadable.On(callback);
        }



        public void RemoveError (Reactor.Action<Exception> callback) {
            this.onerror.Remove(callback);
        }

        public void RemoveEnd (Reactor.Action callback) {
            this.onend.Remove(callback);
        }

        #endregion

        #region Methods

        public void Unshift (Reactor.Buffer buffer) {
            this.socket.Unshift(buffer);
        }

        public void Pause () {
            this.socket.Pause();
        }

        public void Resume () {
            this.socket.Resume();
        }

        public Reactor.IReadable Pipe(Reactor.IWritable writeable) {
            return this.socket.Pipe(writeable);
        }

        #endregion

        #region Buffer

        /// <summary>
        /// Unshifts this data to the stream.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        public void Unshift (byte[] buffer, int index, int count) {
            this.Unshift(Reactor.Buffer.Create(buffer, 0, count));
        }

        /// <summary>
        /// Unshifts this data to the stream.
        /// </summary>
        /// <param name="buffer"></param>
        public void Unshift (byte[] buffer) {
            this.Unshift(Reactor.Buffer.Create(buffer));
        }

        /// <summary>
        /// Unshifts this data to the stream.
        /// </summary>
        /// <param name="data"></param>
        public void Unshift (char data) {
            this.Unshift(data.ToString());
        }

        /// <summary>
        /// Unshifts this data to the stream.
        /// </summary>
        /// <param name="data"></param>
        public void Unshift (string data) {
            this.Unshift(System.Text.Encoding.UTF8.GetBytes(data));
        }

        /// <summary>
        /// Unshifts this data to the stream.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void Unshift (string format, params object[] args) {
            format = string.Format(format, args);
            this.Unshift(System.Text.Encoding.UTF8.GetBytes(format));
        }

        /// <summary>
        /// Unshifts this data to the stream.
        /// </summary>
        /// <param name="data"></param>
        public void Unshift (byte data) {
            this.Unshift(new byte[1] { data });
        }

        /// <summary>
        /// Unshifts a System.Boolean value to the stream.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (bool value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts a System.Int16 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (short value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts a System.UInt16 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (ushort value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts a System.Int32 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (int value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts a System.UInt32 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (uint value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts a System.Int64 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (long value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts a System.UInt64 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (ulong value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts a System.Single value to the stream.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (float value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts a System.Double value to the stream.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (double value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        #endregion

        #region Internal

        /// <summary>
        /// Reads HTTP Protocol.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private Reactor.Async.Future<Reactor.Buffer> ReadProtocol (Reactor.Buffer buffer) {
            return Reactor.Fibers.Fiber.Create<Reactor.Buffer>(() => {
                Reactor.Buffer unconsumed = null;
                Exception parse_error = null;
                var parser = Reactor.Http.Parsers.HttpParser.Create(buffer);
                parser.OnMethod    (method  => { this.method  = method;  });
                parser.OnRawUrl    (raw_url => { this.raw_url = raw_url; });
                parser.OnVersion   (version => { this.version = version; });
                parser.OnHeader    (header  => { this.headers.Add_Internal(header.Key, header.Value); });
                parser.OnError     (error   => { parse_error  = error; });
                parser.OnEnd       (()      => {
                    // this needs fixing...
                    unconsumed = parser.Unconsumed();
                });
                parser.Begin();
                if(parse_error != null) throw parse_error;
                return unconsumed;
            });
        }

        /// <summary>
        /// Parsers Headers
        /// </summary>
        /// <returns></returns>
        private Reactor.Async.Future ReadHeaders () {
            return Reactor.Fibers.Fiber.Create(() => {
                foreach (var header in this.headers) {
                    var lower = header.Key.ToLower(CultureInfo.InvariantCulture);
                    switch (lower) {
                        case "accept-language":
                            var languages = header.Value.Split(',');
                            for (int i = 0; i < languages.Length; i++) {
                                languages[i] = languages[i].Trim();
                            }
                            this.userLanguages = languages;
                            break;

                        case "accept":
                            var accept_types = header.Value.Split(',');
                            for (int i = 0; i < accept_types.Length; i++) {
                                accept_types[i] = accept_types[i].Trim();
                            }
                            this.acceptTypes = accept_types;
                            break;

                        case "content-length":
                            long contentLength = 0;
                            if (!long.TryParse(header.Value.Trim(), out contentLength)) {
                                throw new Exception("invalid content length");
                            }
                            if (contentLength < 0) {
                                throw new Exception("invalid content length");
                            }
                            this.contentLength = contentLength;
                            break;
                    }
                }
            });
        }

        /// <summary>
        /// Parsers the QueryString
        /// </summary>
        /// <returns></returns>
        private Reactor.Async.Future ReadQueryString () {
            return Reactor.Fibers.Fiber.Create(() => {
                var query = this.url.Query;
                if (query == null || query.Length == 0) {
                    return;
                }
                if (query[0] == '?') {
                    query = query.Substring(1);
                }
                string[] components = query.Split('&');
                foreach (string kv in components) {
                    try {
                        int pos = kv.IndexOf('=');
                        if (pos == -1) {
                            this.query.Add(null, Reactor.Http.Utility.UrlDecode(kv));
                        }
                        else {
                            string key = Reactor.Http.Utility.UrlDecode(kv.Substring(0, pos));
                            string val = Reactor.Http.Utility.UrlDecode(kv.Substring(pos + 1));
                            this.query.Add(key, val);
                        }
                    }
                    catch { }
                }
            });
        }

        /// <summary>
        /// Parsers the URL.
        /// </summary>
        /// <returns></returns>
        private Reactor.Async.Future ReadUrl () {
            return Reactor.Fibers.Fiber.Create(() => {
                var host = this.UserHostName;
                if (version > HttpVersion.Version10 && (host.Length == 0)) {
                    throw new Exception("Invalid host name");
                }
                string path;
                Uri raw_uri = null;
                if (Uri.TryCreate(this.raw_url, UriKind.Absolute, out raw_uri)) {
                    path = raw_uri.PathAndQuery;
                }
                else {
                    path = raw_url;
                }
                if (host.Length == 0) {
                    host = this.socket.LocalEndPoint.ToString();
                }
                if (raw_uri != null) {
                    host = raw_uri.Host;
                }
                int colon = host.IndexOf(':');
                if (colon >= 0) {
                    host = host.Substring(0, colon);
                }

                var local = this.socket.LocalEndPoint as IPEndPoint;
                string base_uri = String.Format("{0}://{1}:{2}", "http", host, local.Port);
                Uri url = null;
                if (!Uri.TryCreate(base_uri + path, UriKind.Absolute, out url)) {
                    throw new Exception("Invalid url: " + base_uri + path);
                }
                this.url = url;
            });
        }

        /// <summary>
        /// Begins reading the http request from the raw socket.
        /// </summary>
        internal Reactor.Async.Future BeginRequest () {
            return new Reactor.Async.Future((resolve, reject) => {
                Reactor.Action<Reactor.Buffer> onread = null;
                onread = buffer => {
                    this.socket.Pause();
                    this.socket.RemoveRead(onread);
                    this.ReadProtocol(buffer).Then(unconsumed => {
                        this.ReadHeaders().Then(() => {
                            this.ReadUrl().Then(() => {
                                this.ReadQueryString().Then(() => {
                                    /* once we have processed the request
                                     * we need to reset the socket. The
                                     * following sets received count 
                                     * to zero, unshifts any unconsumed
                                     * data from the buffer, and binds 
                                     * the socket to local listeners. 
                                     * 
                                     * At this point, the socket is in
                                     * a paused state. ideally, we need
                                     * the socket in a pending state so
                                     * the caller can 'resume' processing
                                     * in a typical fashion.
                                     * */
                                    this.received = 0;
                                    this.socket.Unshift (unconsumed);
                                    this.socket.OnRead  (this._Read);
                                    this.socket.OnError (this._Error);
                                    this.socket.OnEnd   (this._End);
                                    resolve();
                                }).Error(reject);
                            }).Error(reject);
                        }).Error(reject);
                    }).Error(reject);
                }; this.socket.OnRead(onread);
            });
        }

        #endregion

        /// <summary>
        /// Handles errors on this stream.
        /// </summary>
        /// <param name="error"></param>
        private void _Error (Exception error) {
            if(!this.ended) {
                this.onerror.Emit(error);
                this._End();
            }
        }

        /// <summary>
        /// Ends this stream.
        /// </summary>
        private void _End () {
            if (!this.ended) {
                this.ended = true;
                this.onend.Emit();
                this.onreadable.Dispose();
                this.onread.Dispose();
                this.onerror.Dispose();
                this.onend.Dispose();
            }
        }

        /// <summary>
        /// Receives data on this stream.
        /// </summary>
        /// <param name="buffer"></param>
        private void _Read (Reactor.Buffer buffer) {
            this.received += buffer.Length;
            Console.WriteLine("_read: {0} - {1} - {2} - {3}", 
                this.received, this.contentLength, 
                this.received < this.contentLength ? "less" : "greater",
                this.contentLength - this.received);
            //Console.Write("[");    
            //Console.Write(buffer);
            //Console.Write("]");    
            if (!this.ended) {
                //-------------------------------------
                // content-length:
                //-------------------------------------
                if (this.ContentLength > 0) {
                    this.onread.Emit(buffer);
                    if (this.received >= this.ContentLength) {
                        var overflow = this.received - this.contentLength;
                        if (overflow > 0) {
                            // TODO: prevent overflow.
                        }
                        this._End();
                    }
                }
                //-------------------------------------
                // transfer-encoding: chunked
                //-------------------------------------
                // TODO
            }
        }
    }
}