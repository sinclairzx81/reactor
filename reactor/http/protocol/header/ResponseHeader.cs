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
    public static class ResponseHeader {

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
                buffer.Dispose();
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
            public Reactor.Http.Headers     Headers           { get; set; }
            public System.Version           Version           { get; set; }
            public System.Int32             StatusCode        { get; set; }
            public System.String            StatusDescription { get; set; }
            public System.Int64             ContentLength     { get; set; }
            public System.String            TransferEncoding  { get; set; }
            public System.Text.Encoding     ContentEncoding   { get; set; }
            internal Result() {
                this.Headers          = new Headers();
                this.Version          = new Version(0, 0);
                this.ContentLength    = 0;
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
            var split = line.Trim().Split(' ');
            if (split.Length < 2) {
                throw new Exception("invalid response line.");
            }
            /* the following code is resposible 
             * for parsing the HTTP/x.x component
             * of the response line. 
             */
            if (!split[0].StartsWith("HTTP/")) {
                throw new Exception("invalid response line. (version)");
            }
            Version version = null;
            try {
                version = new Version(split[0].Substring(5));
                if (version.Major < 1) {
                    throw new Exception("invalid version.");
                }
            }
            catch {
                throw new Exception("invalid response line. (version)");
            }

            /* the following is responsible
             * for parsing the status code.
             */
            var statuscode = 0;
            if (!int.TryParse(split[1], out statuscode)) {
                throw new Exception("invalid status code.");
            } 

            /* status description is optional
             * below we simply check for a 
             * third component of the response
             * line and treat it as the status
             * description.
             */
            string statusDescription = string.Empty;
            if (split.Length == 3) {
                statusDescription = split[2];
            }

            result.StatusDescription = statusDescription;
            result.StatusCode        = statuscode;
            result.Version           = version;
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
                readable.OnceRead(buffer => {
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
                    LineReader.Read(buffer).Then(result => {

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
        /// Reads a http response header object from this readable. This method will begin reading
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