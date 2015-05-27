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
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;

namespace Reactor.Http.Protocol {

    /// <summary>
    /// Provides functionality to read a HTTP header from a 
    /// Reactor.IReadable.
    /// </summary>
    public static class RequestHeader {

        #region Line Reader

        /// <summary>
        /// Specialized line reader functionality to read
        /// lines from a http request. 
        /// </summary>
        internal static class LineReader {

            #region Result

            /// <summary>
            /// Encapsulates the result from a Reactor.Http.Protocol.LineReader.
            /// </summary>
            internal class Result {
                /// <summary>
                /// The lines read from http request.
                /// </summary>
                public List<string>       Lines      {  get; set; }
                /// <summary>
                /// Any unconsumed data from the request.
                /// </summary>
                public Reactor.Buffer     Unconsumed {  get; set; }
            }

            #endregion

            #region Interface

            /// <summary>
            /// Reads lines from this buffer using http content semantics. Responds 
            /// with a result containing the lines read, and a buffer of unconsumed data. 
            /// </summary>
            /// <param name="buffer">The buffer to read.</param>
            /// <returns></returns>
            public static Reactor.Async.Future<Result> Read(Reactor.Buffer buffer) {
                var data = buffer.ToArray();
                return Reactor.Fibers.Fiber.Create<Result>(() => {
                    var lines   = new List<string>();
                    var builder = new StringBuilder();
                    var index   = 0;
                    var length  = 0;
                    var fin     = false;
                    /* here, we enumerate the buffer array,
                     * keeping track of the bytes read as 
                     * we go. We continue until either the 
                     * array has been scanned, or until
                     * we detect we are at the end of 
                     * the header. */
                    while (index < data.Length && !fin) {
                        var b = data[index];
                        switch (b) {
                            /* CR: ignore */
                            case 13: break; 
                            /* LF: on line feed, we push the 
                             * current builder buffer on the 
                             * lines list. additional, we
                             * make our check here as to
                             * whether we have finished by
                             * interpreting a line of length
                             * 0 as a indication we have 
                             * reached the end of this 
                             * header */
                            case 10:
                                var line       = builder.ToString();
                                builder.Length = 0;
                                length         = 0;
                                if (line.Length > 0) 
                                    lines.Add(line);
                                else 
                                    fin = true;
                                break;
                            /* the default is to just push the 
                             * character on the builder and 
                             * continue on. */
                            default: 
                                builder.Append((char)b); 
                                length ++;
                                break;
                        }
                        index++;
                    }
                    /* any unconsumed data not read from the
                     * input buffer needs to be unshifted back
                     * on the stream buffer so it can be 
                     * consumed by the body readers later on.
                     * here we create that buffer and write
                     * the unconsumed bytes to it. */
                    var unconsumed = Reactor.Buffer.Create(data, index, data.Length - index);

                    /* complete. */
                    return new Result{
                        Lines      = lines,
                        Unconsumed = unconsumed
                    };
                });
            }

            #endregion
        }

        #endregion

        #region Result

        /// <summary>
        /// Encapsulates the result of a header read result.
        /// </summary>
        public class Result {
            public Reactor.Http.Headers     Headers          { get; set; }
            public Reactor.Http.QueryString Query            { get; set; }
            public Reactor.Http.Cookies     Cookies          { get; set; }
            public System.String            RawUrl           { get; set; }
            public System.String            Method           { get; set; }
            public System.Version           Version          { get; set; }
            public System.Uri               Url              { get; set; }
            public System.Int64             ContentLength    { get; set; }
            public System.String            UserHostName     { get; set; }
            public System.String            TransferEncoding { get; set; }
            public System.Text.Encoding     ContentEncoding  { get; set; }
            internal Result() {
                this.Headers          = new Headers();
                this.Query            = new QueryString();
                this.Cookies          = new Cookies();
                this.RawUrl           = string.Empty;
                this.Method           = string.Empty;
                this.Version          = new Version(0, 0);
                this.ContentLength    = 0;
                this.UserHostName     = string.Empty;
                this.TransferEncoding = string.Empty;
                this.ContentEncoding  = System.Text.Encoding.Default;
            }
        }

        #endregion

        #region Pipeline

        /// <summary>
        /// Reads the HTTP request line.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="line"></param>
        private static void ReadRequestLine (Result result, string line) {
            /* validate that we have 3 
             * components of the request line. */
            var split = line.Split (new char[] { ' ' }, 3);
            if (split.Length != 3) {
                throw new Exception("Invalid request line (parts).");
            }
            /* next, we parse and validate
             * the http verb being read. */
            var method = split[0];
            foreach (char c in method) {
                int ic = (int)c;
                if ((ic >= 'A' && ic <= 'Z') ||
                    (ic > 32 && c < 127 && c != '(' && c != ')' && c != '<' &&
                      c != '<' && c != '>' && c != '@' && c != ',' && c != ';' &&
                      c != ':' && c != '\\' && c != '"' && c != '/' && c != '[' &&
                      c != ']' && c != '?' && c != '=' && c != '{' && c != '}')) continue; 
                throw new Exception("invalid verb");
            }
            /* next, we parse the rawurl */
            var rawurl = split[1];
            if (split[2].Length != 8 || !split[2].StartsWith("HTTP/")) {
                throw new Exception("Invalid request line (version).");
            } 
            /* next, the version */
            Version version = null;
            try {
                version = new Version(split[2].Substring(5));
                if (version.Major < 1) 
                    throw new Exception("invalid version");
            }
            catch {
                throw new Exception("Invalid request line (version).");
            }

            result.Method = method;
            result.RawUrl = rawurl;
            result.Version = version;            
        }

        /// <summary>
        /// Reads a HTTP request line.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="line"></param>
        private static void ReadHeaderLine (Result result, string line) {
            int colon = line.IndexOf(':');
            if (colon == -1 || colon == 0) {
                throw new Exception("Invalid Header.");
            }
            string name  = line.Substring(0, colon).Trim();
            string value = line.Substring(colon + 1).Trim();
            result.Headers.Add(name, value);
        }

        /// <summary>
        /// Reads and populates common properties of this header object.
        /// </summary>
        /// <param name="result"></param>
        private static void ReadCommonProperties (Result result) {
            foreach (var header in result.Headers) {
                var lower = header.Key.ToLower(CultureInfo.InvariantCulture);
                switch (lower) {
                    case "host":
                        result.UserHostName = result.Headers[lower];
                        break;
                    case "transfer-encoding":
                        result.TransferEncoding = result.Headers[lower];
                        break;
                    case "content-length":
                        long contentLength = 0;
                        if (!long.TryParse(header.Value.Trim(), out contentLength)) {
                            throw new Exception("invalid content length");
                        }
                        if (contentLength < 0) {
                            throw new Exception("invalid content length");
                        }
                        result.ContentLength = contentLength;
                        break;
                }
            }
        }

        /// <summary>
        /// Reads and populates the 'Url' property of this header object.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="endpoint"></param>
        private static void ReadUrl (Result result, EndPoint endpoint) {
            var host = result.UserHostName;
            if (result.Version > HttpVersion.Version10 && (host.Length == 0))  
                throw new Exception("Invalid host name");
            string path;
            Uri raw_uri = null;
            if (Uri.TryCreate(result.RawUrl, UriKind.Absolute, out raw_uri)) 
                path = raw_uri.PathAndQuery;
            else 
                path = result.RawUrl;

            if (host.Length == 0)
                host = endpoint.ToString();
       
            if (raw_uri != null) 
                host = raw_uri.Host;
 
            int colon = host.IndexOf(':');
            if (colon >= 0)
                host = host.Substring(0, colon);

            var local = endpoint as IPEndPoint;
            string base_uri = String.Format("{0}://{1}:{2}", "http", host, local.Port);
            Uri url = null;
            if (!Uri.TryCreate(base_uri + path, UriKind.Absolute, out url))  
                throw new Exception("Invalid url: " + base_uri + path);
            
            result.Url = url;
        }

        /// <summary>
        /// Reads and populates the 'Query' property of this header object.
        /// </summary>
        /// <param name="result"></param>
        private static void ReadQueryString (Result result) {
            var query = result.Url.Query;
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
                        result.Query.Add(null, Reactor.Http.Utility.UrlDecode(kv));
                    }
                    else {
                        string key = Reactor.Http.Utility.UrlDecode(kv.Substring(0, pos));
                        string val = Reactor.Http.Utility.UrlDecode(kv.Substring(pos + 1));
                        result.Query.Add(key, val);
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// Reads http header lines and parses them into a Header Object.
        /// </summary>
        /// <param name="lines">The lines to parse.</param>
        /// <param name="local_endpoint">The local endpoint (typically obtained from socket.LocalEndPoint)</param>
        /// <returns></returns>
        private static Reactor.Async.Future<Result> ReadHeader (List<string> lines, EndPoint local_endpoint) {
            return Reactor.Fibers.Fiber.Create<Result>(() => {
                var result = new Result();
                ReadRequestLine(result, lines[0]);
                for (int i = 1; i < lines.Count; i++)
			        ReadHeaderLine(result, lines[i]);
                ReadCommonProperties (result);
                ReadUrl        (result, local_endpoint);
                ReadQueryString(result);
                return result;
            });
        }

        #endregion

        #region Interface

        /// <summary>
        /// Reads a http header object from this readable. This method will begin reading
        /// from this readable and consume data until it reaches the end of the http header.
        /// Once read, this function leaves this readable in a 'paused' state. Callers are
        /// expected to manually resume this readable once this method has completed.
        /// </summary>
        /// <param name="readable">The readable to read from.</param>
        /// <param name="local_endpoint">The local endpoint (typically obtained from socket.LocalEndPoint)</param>
        /// <returns></returns>
        public static Reactor.Async.Future<Result> Read (Reactor.IReadable readable, EndPoint local_endpoint) {
            return new Reactor.Async.Future<Result>((resolve, reject) => {
                /* we begin by reading a single
                 * chunk from this readable. we
                 * make a assumption that we will
                 * receive the entire header in this
                 * first read */
                readable.OnceRead(data => {
                    /* next, we pause the readable. We do
                     * this prevent additional data being
                     * read from the underlying stream, and
                     * to leave the readable in a state
                     * where a caller can resume() at a
                     * later time. */
                    readable.Pause();

                    /* next, we pass the data obtained in this
                     * read to the protocol line reader.
                     * This will consume as many bytes as
                     * it can and return a result containing
                     * any lines it read from the buffer, as well
                     * as any 'unconsumed' data not read. */
                    LineReader.Read(data).Then(result => {

                        /* we immediately unshift any unread
                         * data. This allows the caller
                         * to re-read this data once we 
                         * have completed reading this header. */
                        readable.Unshift(result.Unconsumed);

                        /* next, we pass the lines read to the 
                         * pipeline above. */
                        ReadHeader(result.Lines, local_endpoint)
                            .Then(resolve)
                            .Error(reject);

                    }).Error(reject);
                });
            });
        }
        
        /// <summary>
        /// Reads a http header object from this readable. This method will begin reading
        /// from this readable and consume data until it reaches the end of the http header.
        /// Once read, this function leaves this readable in a 'paused' state. Callers are
        /// expected to manually resume this readable once this method has completed.
        /// </summary>
        /// <param name="readable">The readable to read from.</param>
        /// <returns></returns>
        public static Reactor.Async.Future<Result> Read (Reactor.IReadable readable) {
            return Read(readable, new IPEndPoint(IPAddress.Any, 0));
        }

        #endregion
    }
}