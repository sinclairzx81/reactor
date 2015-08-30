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
    /// Async Queue allows callers to queue asynchronous 
    /// operations with a user defined level of concurrency.
    /// Useful when needing to control throughput on a given
    /// resource.
    /// </summary>
    /// <example><![CDATA[
    /// var queue = Reactor.Async.Queue(1);
    /// queue.Run(next => {
    ///     do_something_async(() => {
    ///         next();
    ///     });
    /// });
    /// ]]>
    /// </example>
    public class Queue : IDisposable {

        #region Fields

        internal class Fields {
            public Reactor.ConcurrentQueue<Reactor.Action<Reactor.Action>> queue;
            public int concurrency;
            public int running;
            public bool paused;
            public Fields() {
                this.queue       = new Reactor.ConcurrentQueue<Reactor.Action<Reactor.Action>>();
                this.concurrency = 0;
                this.running     = 0;
                this.paused      = false;
            }
        } private Fields fields;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new queue.
        /// </summary>
        /// <param name="concurrency">The level of concurrency allow for processing actions.</param>
        public Queue(int concurrency) {
            this.fields = new Fields();
            this.fields.concurrency = concurrency;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Returns the number of queued actions waiting to be
        /// processed.
        /// </summary>
        public int Pending {
            get { lock(this.fields) return this.fields.queue.Count; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Runs this action. Internally, this action is queued
        /// in a pool of action and executed in turn. If the 
        /// internal queue is empty and this queue is not explicitly
        /// paused, this function will immediately start processing 
        /// actions.
        /// </summary>
        /// <param name="operation"></param>
        public void Run(Reactor.Action<Reactor.Action> operation) {
            lock (this.fields) {
                this.fields.queue.Enqueue(operation);
                if (!this.fields.paused) {
                    this.Process();
                }
            }
        }
        
        /// <summary>
        /// Pauses this queue. Internally, actions currently
        /// being processed will be returned to their callers,
        /// but no future actions will be run until the caller
        /// 'resumes' this queue.
        /// </summary>
        public void Pause() {
            lock (this.fields) {
                this.fields.paused = true;
            }
        }

        /// <summary>
        /// Resumes this queue. Calling this function 
        /// will mark the queue as 'unpaused' and issue
        /// a call to internally process items off the 
        /// queue.
        /// </summary>
        public void Resume() {
            lock (this.fields) {
                this.fields.paused = false;
                this.Process();
            }
        }

        #endregion

        #region Machine

        /// <summary>
        /// If possible, dequeue the next action on the queue and
        /// process it. The process function expects that the 
        /// action being processed will eventially call 'next()', this
        /// resumes control back to this queue, in which case this 
        /// function check the state of the queue, and if possible,
        /// will recursively call itself until all items
        /// in the queue are cleared.
        /// </summary>
        private void Process() {
            lock (this.fields) {
                if (this.fields.queue.Count > 0) {
                    if (this.fields.running < this.fields.concurrency) {
                        Action<Action> action = null;
                        if (this.fields.queue.TryDequeue(out action)) {
                            this.fields.running += 1;
                            action(() => {
                                lock (this.fields) {
                                    this.fields.running -= 1;
                                    if (!this.fields.paused) {
                                        this.Process();
                                    }
                                }
                            });
                        }
                        else {
                            this.Process();
                        }
                    }
                }
            }
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Disposes of this queue.
        /// </summary>
        public void Dispose() {
            this.Pause();
            this.fields.queue.Clear();
        }

        #endregion

        #region Statics

        /// <summary>
        /// Creates a new queue with the specified concurrency.
        /// </summary>
        /// <param name="concurrency">The level of concurrency allow for processing actions.</param>
        /// <returns></returns>
        public static Queue Create(int concurrency) {
            return new Queue(concurrency);
        }

        /// <summary>
        /// Creates a new queue with a concurrency of 1.
        /// </summary>
        /// <returns></returns>
        public static Queue Create() {
            return new Queue(1);
        }

        #endregion
    }
}
