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

namespace Reactor.Process {

    /// <summary>
    /// Provides functionality to stream data over stdin/stdout/stderr for the current process.
    /// </summary>
    public static class Current {

        #region Fields

        public static Reactor.Process.Reader stdin;
        public static Reactor.Process.Writer stdout;
        public static Reactor.Process.Writer stderr;

        #endregion

        #region Constructor

        static Current() {
            Current.stdin  = Reactor.Process.Reader.Create(System.Console.OpenStandardInput());
            Current.stdout = Reactor.Process.Writer.Create(System.Console.OpenStandardOutput());
            Current.stderr = Reactor.Process.Writer.Create(System.Console.OpenStandardError());
        }

        #endregion

        #region Properties

        /// <summary>
        /// Returns the current process stdin stream.
        /// </summary>
        public static Reactor.Process.Reader StdIn {
            get { return stdin;  }
        }

        /// <summary>
        /// Returns the current process stdout stream.
        /// </summary>
        public static Reactor.Process.Writer StdOut {
            get { return stdout; }
        }

        /// <summary>
        /// Returns the current process stderr stream.
        /// </summary>
        public static Reactor.Process.Writer StdErr {
            get { return stderr; }
        }

        #endregion
    }
}
