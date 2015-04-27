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

using System.Timers;

namespace Reactor {

    /// <summary>
    /// Calls a function after a specified delay.
    /// </summary>
    public class Timeout {

        private Timer timer;

        #region Constructors

        /// <summary>
        /// Creates a new Timeout.
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="interval"></param>
        public Timeout(Reactor.Action callback, int interval) {
            this.timer          = new Timer();
            this.timer.Interval = interval;
            this.timer.Enabled  = false;
            this.timer.Start();
            this.timer.Elapsed += (sender, args) => {
                this.timer.Enabled = false;
                this.timer.Stop();
                this.timer.Dispose();
                Loop.Post(() => {
                    callback();
                });
            };
        }

        #endregion

        #region Statics

        /// <summary>
        /// Returns a new Timeout.
        /// </summary>
        /// <param name="callback">The callback to run after the interval has elapsed.</param>
        /// <param name="interval">The interval in milliseconds</param>
        /// <returns>A Timeout</returns>
        public static Timeout Create (Reactor.Action callback, int interval) {
            return new Timeout(callback, interval);
        }

        /// <summary>
        /// Returns a new Timeout. Defaults interval to 1 millisecond.
        /// </summary>
        /// <param name="callback">The callback to run after the interval has elapsed.</param>
        /// <returns></returns>
        public static Timeout Create (Reactor.Action callback) {
            return new Reactor.Timeout(callback, 1);
        }

        #endregion
    }
}
