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
    /// the first buffer received from a http server.
    /// </summary>
    internal class ResponseReader : IDisposable {
        private Reactor.Http.Protocol.LineReader                  line_reader;
        private Reactor.Async.Event<string>                       on_response_line;
        private Reactor.Async.Event<Version>                      on_version;
        private Reactor.Async.Event<int>                          on_status_code;
        private Reactor.Async.Event<string>                       on_status_description;
        private Reactor.Async.Event<KeyValuePair<string, string>> on_header;
        private Reactor.Async.Event<Exception>                    on_error;
        private Reactor.Async.Event                               on_end;

        private bool parsed_response_line;
        private bool ended;

        #region Constructors

        public ResponseReader(Reactor.Buffer buffer) {
            this.line_reader           = new Reactor.Http.Protocol.LineReader(buffer);
            this.on_response_line      = new Reactor.Async.Event<string>();
            this.on_version            = new Reactor.Async.Event<Version>();
            this.on_status_code        = new Reactor.Async.Event<int>();
            this.on_status_description = new Reactor.Async.Event<string>();
            this.on_header             = new Reactor.Async.Event<KeyValuePair<string, string>>();
            this.on_error              = new Reactor.Async.Event<Exception>();
            this.on_end                = new Reactor.Async.Event();
            this.parsed_response_line   = false;
            this.ended                 = false;
        }

        #endregion

        #region Events

        /// <summary>
        /// Subscribes to the 'responseline' event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnResponseLine (Reactor.Action<string> callback) {
            this.on_response_line.On(callback);
        }

        /// <summary>
        /// Subscribes to the 'version' event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnVersion (Reactor.Action<Version> callback) {
            this.on_version.On(callback);
        }

        /// <summary>
        /// Subscribes to the 'status_code' event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnStatusCode (Reactor.Action<int> callback) {
            this.on_status_code.On(callback);
        }

        /// <summary>
        /// Subscribes to the 'status_description' event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnStatusDescription (Reactor.Action<string> callback) {
            this.on_status_description.On(callback);
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
        /// Begins Reading from this Buffer.
        /// </summary>
        public void Begin () {
            this._Read();
        }

        /// <summary>
        /// Returns any data not consumed by this read. This
        /// method should only be called once this reader
        /// has 'ended'.
        /// </summary>
        /// <returns></returns>
        public Reactor.Buffer Unconsumed () {
            return this.line_reader.Unconsumed();
        }

        #endregion

        #region Internals

        /// <summary>
        /// Parses the response Line.
        /// </summary>
        /// <param name="line"></param>
        private void ParseResponseLine (string line) {
            // HTTP/1.1 200 OK

            var split = line.Trim().Split(' ');
            if (split.Length < 2) {
                this._Error(new Exception("invalid response line."));
            }
            /* the following code is resposible 
             * for parsing the HTTP/x.x component
             * of the response line. 
             */
            if (!split[0].StartsWith("HTTP/")) {
                this._Error(new Exception("Invalid response line (version)."));
                return;
            }
            Version version = null;
            try {
                version = new Version(split[0].Substring(5));
                if (version.Major < 1) {
                    this._Error(new Exception("invalid version"));
                    return;
                }
            }
            catch {
                this._Error(new Exception("Invalid response line (version)."));
                return;
            } this.on_version.Emit(version);

            /* the following is responsible
             * for parsing the status code.
             */
            var statuscode = 0;
            if (!int.TryParse(split[1], out statuscode)) {
                this._Error(new Exception("invalid status code."));
                return;
            } this.on_status_code.Emit(statuscode);

            /* status description is optional
             * below we simply check for a 
             * third component of the response
             * line and treat it as the status
             * description.
             */
            if (split.Length == 3) {
                this.on_status_description.Emit(split[2]);
            }

            this.parsed_response_line = true;
        }

        /// <summary>
        /// Parses a header line.
        /// </summary>
        /// <param name="line"></param>
        private void ParseHeaderLine (string line) {
            /* parse header */
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
        /// Handles reader errors.
        /// </summary>
        /// <param name="error"></param>
        private void _Error (Exception error) {
            if (!ended) {
                this.on_error.Emit(error);
                this._End();
            }
        }

        /// <summary>
        /// Ends the reader.
        /// </summary>
        private void _End () {
            if (!ended) {
                this.ended = true;
                this.on_end.Emit();
                this.on_response_line.Dispose();
                this.on_version.Dispose();
                this.on_header.Dispose();
                this.on_error.Dispose();
                this.on_end.Dispose();
            }
        }

        /// <summary>
        /// Begins reading the response.
        /// </summary>
        private void _Read () {
            var state = 0;
            this.line_reader.OnRead  (line => {
                if (!this.ended) {
                    if (line.Length == 0) state = 2;
                    switch (state) {
                        case 0: this.ParseResponseLine (line); state = 1;  break;
                        case 1: this.ParseHeaderLine  (line); break;
                        case 2: /* end of header */ break;
                    }
                }
            });
            this.line_reader.OnError (this._Error);
            this.line_reader.OnEnd   (() => {
                if (!this.parsed_response_line) {
                    this._Error(new Exception("invalid request"));
                }
                this._End();
            });
            this.line_reader.Begin ();
        }

        #endregion

        #region Statics

        /// <summary>
        /// Returns a new response reader.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static ResponseReader Create(Reactor.Buffer buffer) {
            return new ResponseReader(buffer);
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
