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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Reactor.Tests {
    
    [TestClass]
    public class Reactor_Async_Queue {
        [ClassInitialize]
        public static void Startup(TestContext context) {
            Reactor.Loop.Start();
        }

        [ClassCleanup]
        public static void Shutdown() {
            Reactor.Loop.Stop();
        }

        [TestMethod]
        [TestCategory("Reactor.Async.Queue")]
        public async Task Queue_Test_Back_Pressure() {
            var future = Reactor.Async.Future.Create((resolve, reject) => {
                var count = 0;
                var queue = Reactor.Async.Queue.Create(1);
                for (int i = 0; i < 100; i++) {
                    queue.Run(next => {
                        Reactor.Timeout.Create(() => {
                            next();
                            count = count + 1;
                            if (count == 100) {
                                resolve();
                            }
                        }, 1);
                    });
                }
            });
            Reactor.Timeout.Create(future.Cancel, 5000);
            await future;
        }
    }
}
