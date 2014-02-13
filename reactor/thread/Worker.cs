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

using System;
using System.Threading;

namespace Reactor.Threads
{
    /// <summary>
    /// Reactor Worker Factory. Creates a Threaded Delegate.
    /// </summary>
    public static class Worker
    {
        /// <summary>
        /// Creates a threaded worker delegate.
        /// </summary>
        /// <typeparam name="TIn">The input type for this delegate</typeparam>
        /// <typeparam name="TOut">The output type for this delegate</typeparam>
        /// <param name="Func">The worker to be executed within the application threadpool.</param>
        /// <returns>The delegate.</returns>
        public static Action<TIn, Action<Exception, TOut>> Create<TIn, TOut>(Func<TIn, TOut> Func)
        {
            return new Action<TIn, Action<Exception, TOut>>((TIn input, Action<Exception, TOut> callback) =>
            {
                ThreadPool.QueueUserWorkItem((state) => {

                    try
                    {
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
    }
}