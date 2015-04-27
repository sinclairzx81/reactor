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

namespace Reactor.Async {

    /// <summary>
    /// Reactor Spool. Allows users to queue asynchronous operations with
    /// a user defined level of concurrency.
    /// </summary>
    public class Spool : IDisposable {

        private Queue<Reactor.Action<Reactor.Action>> queue;
        private int                                   concurrency;
        private int                                   running;
        private bool                                  paused;

        #region Constructors

        /// <summary>
        /// Creates a new spool.
        /// </summary>
        /// <param name="concurrency"></param>
        public Spool(int concurrency) {

            this.queue       = new Queue<Reactor.Action<Reactor.Action>>();

            this.concurrency = concurrency;

            this.running     = 0;

            this.paused      = false;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Returns the number of queued operations on this spool.
        /// </summary>
        public int Pending {
            get {  return this.queue.Count; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Runs this operation. Callers must call "next" to continue processing.
        /// </summary>
        /// <param name="operation"></param>
        public void Run(Action<Action> operation) {
            this.queue.Enqueue(operation);
            if (!this.paused) {
                this._Process();
            }
        }
        /// <summary>
        /// Pauses the processing of this queue.
        /// </summary>
        public void Pause() {
            this.paused = true;
        }

        /// <summary>
        /// Resumes the processing of this queue.
        /// </summary>
        public void Resume() {
            this.paused = false;
            this._Process();
        }

        #endregion

        #region Internals

        /// <summary>
        /// Process a item from the queue recursively.
        /// </summary>
        private void _Process() {
            if (this.queue.Count > 0) {
                if (this.running < this.concurrency) {
                    var operation = this.queue.Dequeue();
                    this.running += 1;
                    operation(() => {
                        this.running -= 1;
                        if (!this.paused) {
                            this._Process();
                        }
                    });
                }
            }
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Disposes of this spool.
        /// </summary>
        public void Dispose() {
            this.Pause();
            this.queue.Clear();
        }

        #endregion

        #region Statics

        /// <summary>
        /// Creates a new spool.
        /// </summary>
        /// <param name="concurrency"></param>
        /// <returns></returns>
        public static Spool Create(int concurrency) {
            return new Spool(concurrency);
        }

        /// <summary>
        /// Creates a new spool with a concurrency of 1.
        /// </summary>
        /// <returns></returns>
        public static Spool Create() {
            return new Spool(1);
        }

        #endregion
    }
}
