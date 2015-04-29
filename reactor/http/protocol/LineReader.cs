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

namespace Reactor.Http.Protocol {
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
}
