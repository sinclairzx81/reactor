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
using System.Text;

namespace Reactor.Http.Parsers {

    /// <summary>
    /// Reads lines of a http request buffer. Consumes
    /// lines until a empty line is found. Unread
    /// data from the buffer is stored in a unconsumed
    /// buffer for future use.
    /// </summary>
    internal class LineReader  {

        private Reactor.Buffer                   buffer;
        private Reactor.Async.Event<string>    onread;
        private Reactor.Async.Event<Exception> onerror;
        private Reactor.Async.Event            onend;
        private Reactor.Buffer                   unconsumed;
        private bool                             ended;
        
        public LineReader(Reactor.Buffer buffer) {

            this.buffer     = buffer;
            this.onread     = new Reactor.Async.Event<string>();
            this.onerror    = new Reactor.Async.Event<Exception>();
            this.onend      = new Reactor.Async.Event();
            this.unconsumed = new Reactor.Buffer(128);
            this.ended      = false;
        }

        #region Methods

        public Reactor.Buffer Unconsumed() {
            return this.unconsumed;
        }

        public void OnRead (Reactor.Action<string> callback) {
            this.onread.On(callback);
        }

        public void OnError (Reactor.Action<Exception> callback) {
            this.onerror.On(callback);
        }

        public void OnEnd (Reactor.Action callback) {
            this.onend.On(callback);
        }

        public void Begin () {
            this._Read();
        }

        #endregion

        #region Internals

        private void _Data (string data) {
            if (!this.ended) {
                this.onread.Emit(data);
            }
        }

        private void _Error (Exception error) {
            if (!this.ended) {
                this.onerror.Emit(error);
                this._End();
            }
        }

        private void _End () {
            if (!this.ended) {
                this.ended = true;
                this.onend.Emit();
            }
        }

        private void _Read () {
            try {
                var builder = new StringBuilder();
                var array   = buffer.ToArray();
                var index   = 0;
                var length  = 0;
                bool fin = false;
                while (index < array.Length && !fin) {
                    var b = array[index];
                    switch (b) {
                        /* ignore on CR */
                        case 13: break; 
                        /* publish on LF */
                        case 10:
                            var line = builder.ToString();
                            this._Data(line);
                            builder.Length = 0;
                            length         = 0;
                            /* fin */
                            if(line.Length == 0) 
                                fin = true;
                            break;
                        /* append */
                        default: 
                            builder.Append((char)b); 
                            length ++;
                            break;
                    }
                    index++;
                }
                this.unconsumed.Write(array, index,  array.Length - index);
                this._End();
            }
            catch (Exception error) {
                this._Error(error);
            }
        }

        #endregion

        #region Statics

        public static LineReader Create (Reactor.Buffer buffer) {
            return new LineReader(buffer);
        }

        #endregion
    }

    /// <summary>
    /// Reactor Http Parser. Parses the contents of a Reactor.Buffer
    /// and emits data events until ended. The parser writes to 
    /// a 'unconsumed' buffer that callers are expected to check
    /// onend of a parse. 
    /// </summary>
    internal class HttpParser : IDisposable {

        private Reactor.Http.Parsers.LineReader                   reader;
        private Reactor.Async.Event<string>                       onrequestline;
        private Reactor.Async.Event<string>                       onmethod;
        private Reactor.Async.Event<Version>                      onversion;
        private Reactor.Async.Event<string>                       onrawurl;
        private Reactor.Async.Event<KeyValuePair<string, string>> onheader;
        private Reactor.Async.Event<Exception>                    onerror;
        private Reactor.Async.Event                               onend;

        private bool parsed_request_line;
        private bool ended;

        public HttpParser(Reactor.Buffer buffer) {

            this.reader              = new LineReader(buffer);
            this.onrequestline       = new Reactor.Async.Event<string>();
            this.onmethod            = new Reactor.Async.Event<string>();
            this.onversion           = new Reactor.Async.Event<Version>();
            this.onrawurl            = new Reactor.Async.Event<string>();
            this.onheader            = new Reactor.Async.Event<KeyValuePair<string,string>>();
            this.onerror             = new Reactor.Async.Event<Exception>();
            this.onend               = new Reactor.Async.Event();
            this.parsed_request_line = false;
            this.ended               = false;
        }

        #region Methods

        public void OnRequestLine (Reactor.Action<string> callback) {
            this.onrequestline.On(callback);
        }

        public void OnMethod(Reactor.Action<string> callback) {
            this.onmethod.On(callback);
        }

        public void OnVersion (Reactor.Action<Version> callback) {
            this.onversion.On(callback);
        }

        public void OnRawUrl (Reactor.Action<string> callback) {
            this.onrawurl.On(callback);
        }

        public void OnHeader (Reactor.Action<KeyValuePair<string, string>> callback) {
            this.onheader.On(callback);
        }

        public void OnError (Reactor.Action<Exception> callback) {
            this.onerror.On(callback);
        }

        public void OnEnd (Reactor.Action callback) {
            this.onend.On(callback);
        }

        public void Begin () {
            this._Read();
        }

        public Reactor.Buffer Unconsumed () {
            return this.reader.Unconsumed();
        }

        #endregion

        #region Internals

        private void _ParseRequestLine (string line) {
            /* parse request line */
            var split = line.Split (new char[] { ' ' }, 3);
            if (split.Length != 3) {
                this._Error(new Exception("Invalid request line (parts)."));
                return;
            } this.onrequestline.Emit(line);

            
            /* parse verb */
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

            } this.onmethod.Emit(method);

            /* parse raw_url */
            var rawurl = split[1];
            if (split[2].Length != 8 || !split[2].StartsWith("HTTP/")) {
                this._Error(new Exception("Invalid request line (version)."));
                return;
            } this.onrawurl.Emit(rawurl);

            /* parse version */
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
            } this.onversion.Emit(version);
            this.parsed_request_line = true;
        }

        private void _ParseHeaderLine (string line) {
            /* parse header */
            int colon = line.IndexOf(':');
            if (colon == -1 || colon == 0) {
                this._Error(new Exception("Bad Request"));
                return;
            }

            string name  = line.Substring(0, colon).Trim();
            string value = line.Substring(colon + 1).Trim();
            this.onheader.Emit(new KeyValuePair<string, string>(name, value));
        }

        private void _Error (Exception error) {
            if (!ended) {
                this.onerror.Emit(error);
                this._End();
            }
        }

        private void _End () {
            if (!ended) {
                this.ended = true;
                this.onend.Emit();
                this.onrequestline.Dispose();
                this.onmethod.Dispose();
                this.onversion.Dispose();
                this.onrawurl.Dispose();
                this.onheader.Dispose();
                this.onerror.Dispose();
                this.onend.Dispose();
            }
        }

        private void _Read () {
            var state = 0;
            this.reader.OnRead  (line => {
                if (!this.ended) {
                    if (line.Length == 0) state = 2;
                    switch (state) {
                        case 0: this._ParseRequestLine (line); state = 1;  break;
                        case 1: this._ParseHeaderLine  (line); break;
                        case 2: /* end of header */ break;
                    }
                }
            });
            this.reader.OnError (this._Error);
            this.reader.OnEnd   (() => {
                if (!this.parsed_request_line) {
                    this._Error(new Exception("invalid request"));
                }
                this._End();
            });
            this.reader.Begin ();
        }

        #endregion

        #region Statics

        public static HttpParser Create(Reactor.Buffer buffer) {
            return new HttpParser(buffer);
        }

        #endregion

        #region IDisposable

        public void Dispose() {
            this._End();
        }

        #endregion
    }
}
