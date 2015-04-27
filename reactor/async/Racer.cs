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

namespace Reactor.Async {

    /// <summary>
    /// Functionality to protect against asynchronous race conditions.
    /// </summary>
    public class Racer {

        private bool completed;

        #region Constructors

        /// <summary>
        /// Creates a new racer.
        /// </summary>
        public Racer() {
            this.completed = false;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Sets this race condition as complete.
        /// </summary>
        /// <param name="callback"></param>
        public void Set(Action callback) {
            if (!this.completed) {
                this.completed = true;
                callback();
            }
        }

        #endregion

        #region Statics

        /// <summary>
        /// Returns a new Racer.
        /// </summary>
        /// <returns></returns>
        public static Racer Create() {
            return new Racer();
        }

        #endregion
    }
}
