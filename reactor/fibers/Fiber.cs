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

namespace Reactor.Fibers {

    /// <summary>
    /// A fiber allows callers to execute computationally expensive operations 
    /// within the .net thread pool. The results of the operation are scheduled
    /// on the reactor event loop.
    /// </summary>
    /// <example><![CDATA[
    /// 
    ///     // it is advised to try and isolate the data that a fiber has
    ///     // access to. In the example below, we wrap the fiber in a 
    ///     // method to with arguments a and b. The fiber should only
    ///     // interact with these values, ideally in a immutable fashion. C#
    ///     // provides no assurances here, but the author considers this good
    ///     // practice.
    /// 
    ///     public static Reactor.Future<int> Compute(int a, int b) {
    ///         return Reactor.Fiber.Create<int>(() => {
    ///             return a + b; // cpu intensive operation here.
    ///         });
    ///     });
    ///     ...
    ///     Compute(1, 2).Then(value => {
    ///         // 3 !!
    ///     });
    /// ]]>
    /// </example>    
    public class Fiber {

        /// <summary>
        /// Creates a new fiber.
        /// </summary>
        /// <param name="operation">The operation to in the fiber.</param>
        /// <returns></returns>
        public static Reactor.Future Create(Reactor.Action operation) {
            return new Reactor.Future((resolve, reject) => {
                System.Threading.ThreadPool.QueueUserWorkItem(state => {
                    try {
                        operation();
                        Loop.Post(() => resolve());
                    }
                    catch (Exception error) {
                        Loop.Post(() => reject(error));
                    }
                }, null);
            });
        }

        /// <summary>
        /// Creates a new fiber with a result.
        /// </summary>
        /// <param name="operation">The operation to in the fiber.</param>
        /// <returns></returns>
        public static Reactor.Future<T> Create<T>(Reactor.Func<T> operation) {
            return new Reactor.Future<T>((resolve, reject) => {
                System.Threading.ThreadPool.QueueUserWorkItem(state => {
                        try {
                            var result = operation();
                            Loop.Post(() => resolve(result));
                        }
                        catch (Exception error) {
                            Loop.Post(() => reject(error));
                        }
                }, null);
            });
        }
    }
}