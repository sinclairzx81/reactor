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

namespace Reactor.Http.Protocol {
    /// <summary>
    /// Reactor Http Response Parser. This class parses
    /// the contents of a Reactor.Buffer as a HTTP Response
    /// header. The buffer passes to be passed should be
    /// the first buffer received from a http client.
    /// </summary>
    internal class RequestReader : IDisposable {
        private Reactor.Http.Protocol.LineReader                  line_reader;
        private Reactor.Async.Event<string>                       on_request_line;
        private Reactor.Async.Event<string>                       on_method;
        private Reactor.Async.Event<Version>                      on_version;
        private Reactor.Async.Event<string>                       on_raw_url;
        private Reactor.Async.Event<KeyValuePair<string, string>> on_header;
        private Reactor.Async.Event<Exception>                    on_error;
        private Reactor.Async.Event                               on_end;

        private bool parsed_request_line;
        private bool ended;

        public RequestReader(Reactor.Buffer buffer) {
            this.line_reader         = new Reactor.Http.Protocol.LineReader(buffer);
            this.on_request_line     = new Reactor.Async.Event<string>();
            this.on_method           = new Reactor.Async.Event<string>();
            this.on_version          = new Reactor.Async.Event<Version>();
            this.on_raw_url          = new Reactor.Async.Event<string>();
            this.on_header           = new Reactor.Async.Event<KeyValuePair<string,string>>();
            this.on_error            = new Reactor.Async.Event<Exception>();
            this.on_end              = new Reactor.Async.Event();
            this.parsed_request_line = false;
            this.ended               = false;
        }

        #region Events

        /// <summary>
        /// Subscribes to the 'request_line' event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnRequestLine (Reactor.Action<string> callback) {
            this.on_request_line.On(callback);
        }

        /// <summary>
        /// Subscribes to the 'method' event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnMethod(Reactor.Action<string> callback) {
            this.on_method.On(callback);
        }

        /// <summary>
        /// Subscribes to the 'version' event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnVersion (Reactor.Action<Version> callback) {
            this.on_version.On(callback);
        }

        /// <summary>
        /// Subscribes to the 'raw_url' event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnRawUrl (Reactor.Action<string> callback) {
            this.on_raw_url.On(callback);
        }

        /// <summary>
        /// Subscribes to the 'header' event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnHeader (Reactor.Action<KeyValuePair<string, string>> callback) {
            this.on_header.On(callback);
        }

        /// <summary>
        /// Subscribes to the 'error' event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnError (Reactor.Action<Exception> callback) {
            this.on_error.On(callback);
        }

        /// <summary>
        /// Subscribes to the 'end' event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnEnd (Reactor.Action callback) {
            this.on_end.On(callback);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Begins reading this request.
        /// </summary>
        public void Begin () {
            this._Read();
        }

        /// <summary>
        /// Returns any data not consumed by this parser.
        /// </summary>
        /// <returns></returns>
        public Reactor.Buffer Unconsumed () {
            return this.line_reader.Unconsumed();
        }

        #endregion

        #region Internals

        private void ParseRequestLine (string line) {
            /* here, we check that the request line
             * is in 3 distinct parts.
             */ 
            var split = line.Split (new char[] { ' ' }, 3);
            if (split.Length != 3) {
                this._Error(new Exception("Invalid request line (parts)."));
                return;
            } this.on_request_line.Emit(line);
            /* here, we attempt to parse out the
             * http verb used in this request. 
             * additionally, we validate the 
             * characters here (as per mono
             * implementation)
             */
            var method = split[0];
            foreach (char c in method) {
                int ic = (int)c;
                if ((ic >= 'A' && ic <= 'Z') ||
                    (ic > 32 && c < 127 && c != '(' && c != ')' && c != '<' &&
                      c != '<' && c != '>' && c != '@' && c != ',' && c != ';' &&
                      c != ':' && c != '\\' && c != '"' && c != '/' && c != '[' &&
                      c != ']' && c != '?' && c != '=' && c != '{' && c != '}')) continue; 
                this._Error(new Exception("invalid verb"));
                return;
            } this.on_method.Emit(method);

            /* the following code attempts to 
             * parse out the raw_url and the
             * version. We do a bit of pre
             * validation first...
             */
            var rawurl = split[1];
            if (split[2].Length != 8 || !split[2].StartsWith("HTTP/")) {
                this._Error(new Exception("Invalid request line (version)."));
                return;
            } this.on_raw_url.Emit(rawurl);
            /* next, the version */
            Version version = null;
            try {
                version = new Version(split[2].Substring(5));
                if (version.Major < 1) {
                    this._Error(new Exception("invalid version"));
                    return;
                }
            }
            catch {
                this._Error(new Exception("Invalid request line (version)."));
                return;
            } this.on_version.Emit(version);
            /* mark the request line as
             * parsed. and complete.
             */
            this.parsed_request_line = true;
        }

        private void ParseHeaderLine (string line) {
            /* here, we simply split the
             * string by way of the colon, 
             * values are emitted back as
             * is.
             */
            int colon = line.IndexOf(':');
            if (colon == -1 || colon == 0) {
                this._Error(new Exception("Bad Request"));
                return;
            }
            string name  = line.Substring(0, colon).Trim();
            string value = line.Substring(colon + 1).Trim();
            this.on_header.Emit(new KeyValuePair<string, string>(name, value));
        }

        /// <summary>
        /// Handles parse errors.
        /// </summary>
        /// <param name="error"></param>
        private void _Error (Exception error) {
            if (!ended) {
                this.on_error.Emit(error);
                this._End();
            }
        }

        /// <summary>
        /// Handles ending the reader.
        /// </summary>
        private void _End () {
            if (!ended) {
                this.ended = true;
                this.on_end.Emit();
                this.on_request_line.Dispose();
                this.on_method.Dispose();
                this.on_version.Dispose();
                this.on_raw_url.Dispose();
                this.on_header.Dispose();
                this.on_error.Dispose();
                this.on_end.Dispose();
            }
        }

        /// <summary>
        /// Begins reading the request.
        /// </summary>
        private void _Read () {
            var state = 0;
            this.line_reader.OnRead  (line => {
                if (!this.ended) {
                    if (line.Length == 0) state = 2;
                    switch (state) {
                        case 0: this.ParseRequestLine (line); state = 1;  break;
                        case 1: this.ParseHeaderLine  (line); break;
                        case 2: /* end of header */ break;
                    }
                }
            });
            this.line_reader.OnError (this._Error);
            this.line_reader.OnEnd   (() => {
                /* here, we check that
                 * the request line was 
                 * actually parsed.
                 */
                if (!this.parsed_request_line) {
                    this._Error(new Exception("invalid request"));
                }
                this._End();
            });
            this.line_reader.Begin ();
        }

        #endregion

        #region Statics
        /// <summary>
        /// Returns a new Request reader.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static RequestReader Create(Reactor.Buffer buffer) {
            return new RequestReader (buffer);
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Disposes of this reader.
        /// </summary>
        public void Dispose() {
            this._End();
        }

        #endregion
    }
}
