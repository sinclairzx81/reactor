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

namespace Reactor
{
    /// <summary>
    /// Creates a queue object with the specified concurrency. Tasks added to the queue 
    /// are processed in parallel (up to the concurrency limit). If all workers are in 
    /// progress, the task is queued until one becomes available. Once a worker completes 
    /// a task, that task's callback is called.
    /// </summary>
    public class Throttle
    {
        private Queue<Reactor.Action<Action>> queue;

        private int concurrency;

        private int running;

        public Throttle(int concurrency)
        {
            this.queue = new Queue<Reactor.Action<Action>>();

            this.concurrency = concurrency;

            this.running = 0;
        }

        #region Future

        public Future Run(Func<Future> operation)
        {
            var deferred = new Deferred();

            this.queue.Enqueue((complete =>
            {
                operation().Then(() =>
                {
                    deferred.Resolve();

                    complete();

                }).Error(error =>
                {
                    deferred.Reject(error);

                    complete();
                });
            }));

            this.Process();

            return deferred.Future;
        }

        public Future Run<T0>(Func<T0, Future> operation, T0 arg0)
        {
            var deferred = new Deferred();

            this.queue.Enqueue((complete =>
            {
                operation(arg0).Then(() =>
                {
                    deferred.Resolve();

                    complete();

                }).Error(error =>
                {
                    deferred.Reject(error);

                    complete();
                });
            }));

            this.Process();

            return deferred.Future;
        }

        public Future Run<T0, T1>(Func<T0, T1, Future> operation, T0 arg0, T1 arg1)
        {
            var deferred = new Deferred();

            this.queue.Enqueue((complete =>
            {
                operation(arg0, arg1).Then(() =>
                {
                    deferred.Resolve();

                    complete();

                }).Error(error =>
                {
                    deferred.Reject(error);

                    complete();
                });
            }));

            this.Process();

            return deferred.Future;
        }

        public Future Run<T0, T1, T2>(Func<T0, T1, T2, Future> operation, T0 arg0, T1 arg1, T2 arg2)
        {
            var deferred = new Deferred();

            this.queue.Enqueue((complete =>
            {
                operation(arg0, arg1, arg2).Then(() =>
                {
                    deferred.Resolve();

                    complete();

                }).Error(error =>
                {
                    deferred.Reject(error);

                    complete();
                });
            }));

            this.Process();

            return deferred.Future;
        }

        public Future Run<T0, T1, T2, T3>(Func<T0, T1, T2, T3, Future> operation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
        {
            var deferred = new Deferred();

            this.queue.Enqueue((complete =>
            {
                operation(arg0, arg1, arg2, arg3).Then(() =>
                {
                    deferred.Resolve();

                    complete();

                }).Error(error =>
                {
                    deferred.Reject(error);

                    complete();
                });
            }));

            this.Process();

            return deferred.Future;
        }
        
        #endregion

        #region Future<T>

        public Future<TResult> Run<TResult>(Func<Future<TResult>> operation)
        {
            var deferred = new Deferred<TResult>();

            this.queue.Enqueue((complete =>
            {
                operation().Then(response =>
                {
                    deferred.Resolve(response);

                    complete();

                }).Error(error =>
                {
                    deferred.Reject(error);

                    complete();
                });
            }));

            this.Process();

            return deferred.Future;
        }

        public Future<TResult> Run<T0, TResult>(Func<T0, Future<TResult>> operation, T0 arg0)
        {
            var deferred = new Deferred<TResult>();

            this.queue.Enqueue((complete =>
            {
                operation(arg0).Then(response =>
                {
                    deferred.Resolve(response);

                    complete();

                }).Error(error =>
                {
                    deferred.Reject(error);

                    complete();
                });
            }));

            this.Process();

            return deferred.Future;
        }

        public Future<TResult> Run<T0, T1, TResult>(Func<T0, T1, Future<TResult>> operation, T0 arg0, T1 arg1)
        {
            var deferred = new Deferred<TResult>();

            this.queue.Enqueue((complete =>
            {
                operation(arg0, arg1).Then(response =>
                {
                    deferred.Resolve(response);

                    complete();

                }).Error(error =>
                {
                    deferred.Reject(error);

                    complete();
                });
            }));

            this.Process();

            return deferred.Future;
        }

        public Future<TResult> Run<T0, T1, T2, TResult>(Func<T0, T1, T2, Future<TResult>> operation, T0 arg0, T1 arg1, T2 arg2)
        {
            var deferred = new Deferred<TResult>();

            this.queue.Enqueue((complete =>
            {
                operation(arg0, arg1, arg2).Then(response =>
                {
                    deferred.Resolve(response);

                    complete();

                }).Error(error =>
                {
                    deferred.Reject(error);

                    complete();
                });
            }));

            this.Process();

            return deferred.Future;
        }

        public Future<TResult> Run<T0, T1, T2, T3, TResult>(Func<T0, T1, T2, T3, Future<TResult>> operation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
        {
            var deferred = new Deferred<TResult>();

            this.queue.Enqueue((complete =>
            {
                operation(arg0, arg1, arg2, arg3).Then(response =>
                {
                    deferred.Resolve(response);

                    complete();

                }).Error(error =>
                {
                    deferred.Reject(error);

                    complete();
                });
            }));

            this.Process();

            return deferred.Future;
        }

        #endregion

        #region Process

        private void Process()
        {
            if (this.queue.Count > 0)
            {
                if (this.running < this.concurrency)
                {
                    var action = this.queue.Dequeue();

                    this.running += 1;

                    action(() =>
                    {
                        this.running -= 1;

                        if (this.queue.Count > 0) this.Process();
                    });
                }
            }
        }

        #endregion
    }
}