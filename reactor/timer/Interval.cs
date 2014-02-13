/*--------------------------------------------------------------------------

The MIT License (MIT)

Copyright (c) 2014 Haydn Paterson (sinclair) <haydn.developer@gmail.com>

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

namespace Reactor
{
    /// <summary>
    /// Timer utility to evaluate an expression each time a specified number of milliseconds has elapsed.
    /// </summary>
    public class Interval
    {
        private Timer Timer  { get; set; }

        public Interval(Action callback, double interval)
        {
            this.Timer          = new Timer();

            this.Timer.Interval = interval;

            this.Timer.Enabled  = false;

            this.Timer.Start();

            this.Timer.Elapsed += (sender, args) =>
            {
                Loop.Post(() =>
                {
                    callback();   
                });
            };
        }

        /// <summary>
        /// Stops and clears this interval.
        /// </summary>
        public void Clear()
        {
            this.Timer.Enabled = false;

            this.Timer.Stop();

            this.Timer.Dispose();
        }

        #region Statics

        /// <summary>
        /// Creates a new instance of a Interval.
        /// </summary>
        /// <param name="callback">The callback to run after the interval has elapsed.</param>
        /// <param name="interval">The interval in milliseconds</param>
        /// <returns>A Timeout</returns>
        public static Interval Create(Action callback, double interval)
        {
            return new Interval(callback, interval);
        }

        #endregion
    }
}
