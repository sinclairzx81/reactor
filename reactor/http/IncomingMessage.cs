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

        #region States

        /// <summary>
        /// Readable state.
        /// </summary>
        internal enum State {
            /// <summary>
            /// The initial state of this stream. A stream
            /// in a pending state signals that the stream
            /// is waiting on the caller to issue a read request
            /// the the underlying resource, by attaching a
            /// OnRead, OnReadable, or calling Read().
            /// </summary>
            Pending,
            /// <summary>
            /// A stream in a reading state signals that the
            /// stream is currently requesting data from the
            /// underlying resource and is waiting on a 
            /// response.
            /// </summary>
            Reading,
            /// <summary>
            /// A stream in a paused state will bypass attempts
            /// to read on the underlying resource. A paused
            /// stream must be resumed by the caller.
            /// </summary>
            Paused,
            /// <summary>
            /// Indicates this stream has ended. Streams can end
            /// by way of reaching the end of the stream, or through
            /// error.
            /// </summary>
            Ended
        }

        /// <summary>
        /// Readable mode.
        /// </summary>
        internal enum Mode {
            /// <summary>
            /// This stream is using flowing semantics.
            /// </summary>
            Flowing,
            /// <summary>
            /// This stream is using non-flowing semantics.
            /// </summary>
            NonFlowing
        }

        #endregion

        private Reactor.Tcp.Socket                  socket;
        private Reactor.Async.Event                 onreadable;
        private Reactor.Async.Event<Reactor.Buffer> onread;
        private Reactor.Async.Event<Exception>      onerror;
        private Reactor.Async.Event                 onend;
        private Reactor.Buffer                      buffer;
        private State                               state;
        private Mode                                mode;
        private int                                 received;

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
        

        #region Constructors

        /// <summary>
        /// Creates a new Incoming Message. 
        /// </summary>
        /// <param name="socket"></param>
        internal IncomingMessage(Reactor.Tcp.Socket socket) {
            this.socket          = socket;
            this.onreadable      = Reactor.Async.Event.Create();
            this.onread          = Reactor.Async.Event.Create<Reactor.Buffer>();
            this.onerror         = Reactor.Async.Event.Create<Exception>();
            this.onend           = Reactor.Async.Event.Create();
            this.buffer          = Reactor.Buffer.Create();
            this.state           = State.Pending;
            this.mode            = Mode.NonFlowing;

            /* initialize with reasonable defaults */
            this.headers         = new Headers();
            this.query           = new Query();
            this.version         = null;
            this.method          = null;
            this.raw_url         = null;
            this.url             = null;
            this.contentLength   = 0;
            this.contentEncoding = Encoding.Default;
            this.acceptTypes     = null;
            this.userLanguages   = null;
            this.received        = 0;
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
        /// The Transfer-Encoding header.
        /// </summary>
        public string TransferEncoding {
            get { return this.headers["transfer-encoding"] ?? string.Empty; }
        }

        /// <summary>
        /// The HTTP referer header.
        /// </summary>
        public string Referer {
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

        /// <summary>
        /// Subscribes this action to the 'readable' event. When a chunk of 
        /// data can be read from the stream, it will emit a 'readable' event.
        /// Listening for a 'readable' event will cause some data to be read 
        /// into the internal buffer from the underlying resource. If a stream 
        /// happens to be in a 'paused' state, attaching a readable event will 
        /// transition into a pending state prior to reading from the resource.
        /// </summary>
        /// <param name="callback"></param>
        public void OnReadable (Reactor.Action callback) {
            this.onreadable.On(callback);
            this.mode = Mode.NonFlowing;
            if (this.state == State.Paused) {
                this.state = State.Pending;
            }
            this._Read(); 
        }

        /// <summary>
        /// Subscribes this action once to the 'readable' event. When a chunk of 
        /// data can be read from the stream, it will emit a 'readable' event.
        /// Listening for a 'readable' event will cause some data to be read 
        /// into the internal buffer from the underlying resource. If a stream 
        /// happens to be in a 'paused' state, attaching a readable event will 
        /// transition into a pending state prior to reading from the resource.
        /// </summary>
        /// <param name="callback"></param>
        public void OnceReadable(Reactor.Action callback) {
            this.onreadable.Once(callback);
            this.mode = Mode.NonFlowing;
            if (this.state == State.Paused) {
                this.state = State.Pending;
            }
            this._Read();
        }

        /// <summary>
        /// Unsubscribes this action from the 'readable' event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveReadable(Reactor.Action callback) {
			this.onreadable.Remove(callback);
        }

        /// <summary>
        /// Subscribes this action to the 'read' event. Attaching a data event 
        /// listener to a stream that has not been explicitly paused will 
        /// switch the stream into flowing mode and begin reading immediately. 
        /// Data will then be passed as soon as it is available.
        /// </summary>
        /// <param name="callback"></param>
        public void OnRead (Reactor.Action<Reactor.Buffer> callback) {
            this.onread.On(callback);
            if (this.state == State.Pending) {
                this.Resume();
            }
        }

        /// <summary>
        /// Subscribes this action once to the 'read' event. Attaching a data event 
        /// listener to a stream that has not been explicitly paused will 
        /// switch the stream into flowing mode and begin reading immediately. 
        /// Data will then be passed as soon as it is available.
        /// </summary>
        /// <param name="callback"></param>
        public void OnceRead(Reactor.Action<Reactor.Buffer> callback) {
            this.onread.Once(callback);
            if (this.state == State.Pending) {
                this.Resume();
            }
        }

        /// <summary>
        /// Unsubscribes this action from the 'read' event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveRead (Reactor.Action<Reactor.Buffer> callback) {
            this.onread.Remove(callback);
        }

        /// <summary>
        /// Subscribes this action to the 'error' event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnError (Reactor.Action<Exception> callback) {
            this.onerror.On(callback);
        }

        /// <summary>
        /// Unsubscribes this action from the 'error' event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveError (Reactor.Action<Exception> callback) {
            this.onerror.Remove(callback);
        }

        /// <summary>
        /// Subscribes this action to the 'end' event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnEnd (Reactor.Action callback) {
            this.onend.On(callback);
        }

        /// <summary>
        /// Unsubscribes this action from the 'end' event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveEnd (Reactor.Action callback) {
            this.onend.Remove(callback);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Will read this number of bytes out of the internal buffer. If there 
        /// is no data available, then it will return a zero length buffer. If 
        /// the internal buffer has been completely read, then this method will 
        /// issue a new read request on the underlying resource in non-flowing 
        /// mode. Any data read with a length > 0 will also be emitted as a 'read' 
        /// event.
        /// </summary>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns></returns>
        public Reactor.Buffer Read (int count) {
            var result = Reactor.Buffer.Create(this.buffer.Read(count));
            if (result.Length > 0) {
                this.onread.Emit(result);
            }
            if (this.buffer.Length == 0) {
                this.mode = Mode.NonFlowing;
                this._Read();
            }
            return result;
        }

        /// <summary>
        /// Will read all data out of the internal buffer. If no data is available 
        /// then it will return a zero length buffer. This method will then issue 
        /// a new read request on the underlying resource in non-flowing mode. Any 
        /// data read with a length > 0 will also be emitted as a 'read' event.
        /// </summary>
        public Reactor.Buffer Read () {
            return this.Read(this.buffer.Length);
        }

        /// <summary>
        /// Unshifts this buffer back to this stream.
        /// </summary>
        /// <param name="buffer">The buffer to unshift.</param>
        public void Unshift (Reactor.Buffer buffer) {
            this.buffer.Unshift(buffer);
        }

        /// <summary>
        /// Pauses this stream. This method will cause a 
        /// stream in flowing mode to stop emitting data events, 
        /// switching out of flowing mode. Any data that becomes 
        /// available will remain in the internal buffer.
        /// </summary>
        public void Pause() {
			this.mode  = Mode.NonFlowing;
            this.state = State.Paused;
        }

        /// <summary>
        /// This method will cause the readable stream to resume emitting data events.
        /// This method will switch the stream into flowing mode. If you do not want 
        /// to consume the data from a stream, but you do want to get to its end event, 
        /// you can call readable.resume() to open the flow of data.
        /// </summary>
        public void Resume() {
            this.mode  = Mode.Flowing;
            this.state = State.Pending;
            this._Read();
        }

        /// <summary>
        /// Pipes data to a writable stream.
        /// </summary>
        /// <param name="writable"></param>
        /// <returns></returns>
        public Reactor.IReadable Pipe (Reactor.IWritable writable) {
            this.OnRead(data => {
                this.Pause();
                writable.Write(data)
                        .Then(this.Resume)
                        .Error(this._Error);
            });
            this.OnEnd (() => writable.End());
            return this;
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
        /// Resolves a System.Net.Uri from the raw_url.
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
        /// Responsible for initializing the Incoming Message. This
        /// method will begin reading from the underlying socket and
        /// attempt to parse the http protocol header. On successful
        /// parse, the method will bind the sockets read, error and
        /// end listeners to 'this' machine. This is done as we need
        /// to add read semantics for chunked and content-length 
        /// bodies, but also to provide a layed abstraction between
        /// the caller and the socket.
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
                                     * following sets 'this' received count 
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
                                    this.buffer.Write(unconsumed);
                                    this.received = unconsumed.Length;
                                    this.socket.OnError(this._Error);
                                    this.socket.OnEnd(this._End);
                                    resolve();
                                }).Error(reject);
                            }).Error(reject);
                        }).Error(reject);
                    }).Error(reject);
                }; this.socket.OnRead(onread);
            });
        }

        #endregion

        #region Machine

        /// <summary>
        /// Begins reading from the underlying stream.
        /// </summary>
        private void _Read () {
            if (this.state == State.Pending) {
                this.state = State.Reading;
                /* any data resident in the buffer
                 * needs to emitted prior to issuing
                 * a request for more, normal operation
                 * would assume that the callers only 
                 * need to read if they have emptied 
                 * the buffer, however, this rule is
                 * broken in instances where the user
                 * may have unshifted data inbetween
                 * reads. The following overrides the
                 * default behaviour and calls to 
                 * _data() directly with a cloned
                 * buffer.
                 */
                if (this.buffer.Length > 0) {
                    var clone = this.buffer.Clone();
                    this.buffer.Clear();
                    this.onread.Emit(clone);
                    this._Data(this.buffer);
                }
                /* here, we handle the case where
                 * the caller is attempting to read a 
                 * request with a content-length of 0.
                 * in this instance, we defer emitting
                 * end till next loop to give the caller
                 * enough time to attach listeners.
                 */
                else if (this.received >= this.contentLength) {
                    Loop.Post(this._End);
                }
                /* here, we make a actual request on the
                 * underlying socket. This is a conceptually
                 * similar approach taken by other readable
                 * streams.
                 */
                else {
                    this.socket.OnceRead(data => {
                        this.socket.Pause();
                        this._Data(data);
                    }); this.socket.Resume();
                }
            }
        }

        /// <summary>
        /// Handles incoming data from the stream.
        /// </summary>
        /// <param name="buffer"></param>
        private void _Data (Reactor.Buffer buffer) {
            if (this.state == State.Reading) {
                this.state = State.Pending;

                bool ended = false;

                if (this.TransferEncoding != "chunked") {
                    /* non chunked content needs to be 
                     * compared against the content-length.
                     * here, we increment the received,
                     * and trim the buffer if necessary. 
                     */
                    var length = buffer.Length;
                    this.received = this.received + length;
                    if (this.received >= this.contentLength) {
                        var overflow = this.received - this.contentLength;
                        length = length - (int)overflow;
                        buffer = buffer.Slice(0, length);
                        ended  = true;
                    }
                }

                this.buffer.Write(buffer);
                switch (this.mode) {
                    case Mode.Flowing:
                        var clone = this.buffer.Clone();
                        this.buffer.Clear();
                        this.onread.Emit(clone);
                        if(ended)
                            this._End();
                        else
                            this._Read();
                        break;
                    case Mode.NonFlowing:
                        this.onreadable.Emit();
                        if(ended)
                            this._End();
                        break;
                }
            }
        }

        /// <summary>
        /// Handles stream errors.
        /// </summary>
        /// <param name="error"></param>
        private void _Error (Exception error) {
            if (this.state != State.Ended) { 
                this.onerror.Emit(error);
                this._End();
            }
        }

        /// <summary>
        /// Terminates the stream.
        /// </summary>
        public void _End    () {
            if (this.state != State.Ended) {
                this.state = State.Ended;
                this.onend.Emit();
            }
        }

        #endregion
    }
}