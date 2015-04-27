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
    /// Calls a function repeatedly, with a fixed time delay between each call to that function. 
    /// </summary>
    public class Interval {

        private Timer timer;

        #region Constructors

        /// <summary>
        /// Creates a new interval.
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="interval"></param>
        public Interval(Reactor.Action callback, double interval) {
            this.timer          = new Timer();
            this.timer.Interval = interval;
            this.timer.Enabled  = false;
            this.timer.Start();
            this.timer.Elapsed += (sender, args) => {
                Loop.Post(() => {
                    callback();   
                });
            };
        }

        #endregion

        #region Methods

        /// <summary>
        /// Stops and clears this interval.
        /// </summary>
        public void Clear() {
            this.timer.Enabled = false;
            this.timer.Stop();
            this.timer.Dispose();
        }

        #endregion

        #region Statics

        /// <summary>
        /// Creates a new interval.
        /// </summary>
        /// <param name="callback">The callback to run after the interval has elapsed.</param>
        /// <param name="interval">The interval in milliseconds</param>
        /// <returns>A Timeout</returns>
        public static Interval Create(Reactor.Action callback, double interval) {
            return new Interval(callback, interval);
        }

        /// <summary>
        /// Creates a new interval (defaults to 1ms)
        /// </summary>
        /// <param name="?"></param>
        /// <returns></returns>
        public static Interval Create(Reactor.Action callback) {
            return new Interval(callback, 1);
        }

        #endregion
    }
}
