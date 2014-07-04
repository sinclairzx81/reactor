/*--------------------------------------------------------------------------

Reactor

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

using System;
using System.Collections.Generic;
using System.Threading;

namespace Reactor
{
    public class AsyncResult<TResult>
    {
        public Exception Exception { get; set ;}

        public TResult   Result    { get; set; }

        internal AsyncResult(Exception exception, TResult result)
        {
            this.Exception = exception;

            this.Result    = result;
        }
    }

    public class AsyncException: Exception
    {
        public List<Exception> Exceptions { get; private set; }

        internal AsyncException(List<Exception> exceptions) : base()
        {
            this.Exceptions = exceptions;
        }
    }

    public class Async
    {
        #region Wait

        /// <summary>
        /// Synchronously awaits on a asynchronous operation. 
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TOutput">The output type</typeparam>
        /// <param name="action">The method or action encapsulating the async operation</param>
        /// <param name="input">The method or action input argument.</param>
        /// <returns>The async operation result value.</returns>
        public static TOutput Wait<TInput, TOutput>(Reactor.Action<TInput, Reactor.Action<Exception, TOutput>> action, TInput input)
        {
            var manual = new ManualResetEvent(false);

            AsyncResult<TOutput> result = null;

            Reactor.Async.Task<object, object>((a) => {

                action(input, (exception, output) => {

                    result = new AsyncResult<TOutput>(exception, output);

                    manual.Set();
                });

                return null;

            }) (null, (exception, ret) => { });

            manual.WaitOne();

            if(result.Exception != null)
            {
                throw result.Exception;
            }

            return result.Result;            
        }

        #endregion

        #region Parallel

        /// <summary>
        /// Runs a asynchronous operation in parallel.
        /// </summary>
        /// <typeparam name="TInput">The methods input type</typeparam>
        /// <typeparam name="TOutput">The methods output type</typeparam>
        /// <param name="action">The method or action encapsulating the async operation</param>
        /// <param name="inputs">The method or action input array.</param>
        /// <param name="callback">The callback to receive async results</param>
        public static void Parallel <TInput, TOutput>(Reactor.Action<TInput, Reactor.Action<Exception, TOutput>> action, IEnumerable<TInput> inputs, Reactor.Action<IEnumerable<AsyncResult<TOutput>>> callback)
        {
            var count = 0;

            foreach(var item in inputs)
            {
                count++;
            }

            if (count == 0)
            {
                callback(new List<AsyncResult<TOutput>>());

                return;
            }

            Reactor.Action<int, TInput, Reactor.Action<int, Exception, TOutput>> container = null;

            container = (index, input, _callback) => {

                action(input, (exception, output) => {

                    _callback(index, exception, output);
                });
            };

            var outputs  = new AsyncResult<TOutput>[count];

            var completed = 0; 

            var index_in  = 0;

            foreach (var input in inputs)
            {
                container(index_in, input, (index_out, exception, output) =>
                {
                    outputs[index_out] = new AsyncResult<TOutput>(exception, output);

                    completed++;

                    if (completed == count)
                    {
                        callback(outputs);
                    }
                });

                index_in++;
            }
        }

        /// <summary>
        /// Runs a asynchronous operation synchronously in parallel.
        /// </summary>
        /// <typeparam name="TInput">The input type.</typeparam>
        /// <typeparam name="TOutput">The output type.</typeparam>
        /// <param name="action">The method or action encapsulating the async operation</param>
        /// <param name="inputs">The method or action input array.</param>
        /// <returns>The async results</returns>
        public static IEnumerable<TOutput> Parallel <TInput, TOutput>(Reactor.Action<TInput, Reactor.Action<Exception, TOutput>> action, IEnumerable<TInput> inputs)
        {
            var manual = new ManualResetEvent(false);

            IEnumerable<AsyncResult<TOutput>> asyncResults = null;

            Async.Task<object, object>((a) => {

                Async.Parallel<TInput, TOutput>(action, inputs, (outputs) => {

                    asyncResults = outputs;

                    manual.Set();
                });

                return null;

            }) (null, (exception, result) => { });

            manual.WaitOne();

            //----------------------------------
            // gather operation
            //----------------------------------

            var exceptions = new List<Exception>();

            var results    = new List<TOutput>();

            foreach(var result in asyncResults) {

                if (result.Exception != null) {

                    exceptions.Add(result.Exception);
                }

                results.Add(result.Result);
            }

            if(exceptions.Count > 0) {

                throw new AsyncException(exceptions);
            }

            return results;
        }

        #endregion

        #region Series

        /// <summary>
        /// Runs a asynchronous operation in series.
        /// </summary>
        /// <typeparam name="TInput">The methods input type</typeparam>
        /// <typeparam name="TOutput">The methods output type</typeparam>
        /// <param name="action">The method or action encapsulating the async operation</param>
        /// <param name="inputs">The method or action input array.</param>
        /// <param name="callback">The callback to receive async results</param>
        public static void Series <TInput, TOutput>(Reactor.Action<TInput, Reactor.Action<Exception, TOutput>> action, IEnumerable<TInput> inputs, Reactor.Action<IEnumerable<AsyncResult<TOutput>>> callback)
        {
            var queue = new Queue<TInput>();

            var list  = new List<AsyncResult<TOutput>>();

            foreach(var input in inputs)
            {
                queue.Enqueue(input);
            }

            if (queue.Count == 0)
            {
                callback(new List<AsyncResult<TOutput>>());

                return;
            }

            Reactor.Action container = null;

            container = () =>
            {
                var input = queue.Dequeue();

                action(input, (exception, output) =>
                {
                    list.Add(new AsyncResult<TOutput>(exception, output));

                    if(queue.Count > 0)
                    {
                        container();

                        return;
                    }

                    callback(list);
                });
            };

            container();
        }

        /// <summary>
        /// Runs a asynchronous operation synchronously in series.
        /// </summary>
        /// <typeparam name="TInput">The input type.</typeparam>
        /// <typeparam name="TOutput">The output type.</typeparam>
        /// <param name="action">The method or action encapsulating the async operation</param>
        /// <param name="inputs">The method or action input array.</param>
        /// <returns>The async results</returns>
        public static IEnumerable<TOutput> Series<TInput, TOutput>(Reactor.Action<TInput, Reactor.Action<Exception, TOutput>> action, IEnumerable<TInput> inputs)
        {
            var manual = new ManualResetEvent(false);

            IEnumerable<AsyncResult<TOutput>> asyncResults = null;

            Async.Task<object, object>((a) => {

                Async.Series<TInput, TOutput>(action, inputs, (outputs) => {

                    asyncResults = outputs;

                    manual.Set();
                });

                return null;

            }) (null, (exception, result) => { });

            manual.WaitOne();

            //----------------------------------
            // gather operation
            //----------------------------------

            var exceptions = new List<Exception>();

            var results    = new List<TOutput>();

            foreach(var result in asyncResults) {

                if (result.Exception != null) {

                    exceptions.Add(result.Exception);
                }

                results.Add(result.Result);
            }

            if(exceptions.Count > 0) {

                throw new AsyncException(exceptions);
            }

            return results;
        }

        #endregion

        #region Task

        /// <summary>
        /// Creates a asynchronous threaded task executed on the .net thread pool.
        /// </summary>
        /// <typeparam name="TIn">The task input type.</typeparam>
        /// <typeparam name="TOut">The task output type.</typeparam>
        /// <param name="Func">The task function body.</param>
        /// <returns>A delegate action encapsulating the task.</returns>
        public static Action<TIn, Action<Exception, TOut>> Task<TIn, TOut>(Func<TIn, TOut> Func)
        {
            return new Action<TIn, Action<Exception, TOut>>((TIn input, Action<Exception, TOut> callback) =>
            {
                ThreadPool.QueueUserWorkItem((state) => {
                
                    try {

                        var result = Func(input);

                        Loop.Post(() =>
                        {
                            callback(null, result);
                        });
                    }
                    catch (Exception exception)
                    {
                        Loop.Post(() =>
                        {
                            callback(exception, default(TOut));
                        });
                    }

                }, null);
            });
        }

        #endregion


        #region Map

        /// <summary>
        /// Maps the input type array into the output type array.
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TOutput">The output type.</typeparam>
        /// <param name="inputs">The array of inputs</param>
        /// <param name="callback">The callback to process the mapping between input and output types.</param>
        /// <returns>The mapped result.</returns>
        public static IEnumerable<TOutput> Map<TInput, TOutput>(IEnumerable<TInput> inputs, Func<TInput, int, IEnumerable<TInput>, TOutput> callback)
        {
            var count = 0;

            foreach (var input in inputs)
            {
                count += 1;
            }

            var outputs = new TOutput[count];

            var index = 0;

            foreach (var input in inputs)
            {
                outputs[index] = callback(input, index, inputs);

                index++;
            }

            return outputs;
        }

        #endregion
    }
}
