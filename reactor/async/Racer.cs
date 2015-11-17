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

namespace Reactor {

    /// <summary>
    /// Racer allows two async operations to race each other. 
    /// </summary>
    /// <example><![CDATA[
    /// var racer = new Reactor.Racer();
    /// Reactor.Timeout.Create(() => {
    ///     racer.Set(() => {
    ///         // this code will execute.
    ///     })
    /// }, 1000);
    /// 
    /// Reactor.Timeout.Create(() => {
    ///     racer.Set(() => {
    ///         // this code won't.
    ///     })
    /// }, 2000);
    /// ]]>
    /// </example>
    public class Racer {

        #region Fields

        internal class Fields {
            public bool completed;
            public Fields() {
                this.completed = false;
            }
        } private Fields fields;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new racer.
        /// </summary>
        public Racer() {
            this.fields = new Fields();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Sets this race condition as complete.
        /// </summary>
        /// <param name="callback"></param>
        public void Set(Action callback) {
            lock (this.fields) {
                if (!this.fields.completed) {
                    this.fields.completed = true;
                    callback();
                }
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
