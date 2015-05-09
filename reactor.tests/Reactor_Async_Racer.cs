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
using System;
using System.Threading.Tasks;

namespace Reactor.Tests {

    [TestClass]
    public class Reactor_Async_Racer {
        [ClassInitialize]
        public static void Startup(TestContext context) {
            Reactor.Loop.Start();
        }
        [ClassCleanup]
        public static void Shutdown() {
            Reactor.Loop.Stop();
        }

        [TestMethod]
        [TestCategory("Reactor.Async.Racer")]
        public async Task Racer_Test_Condition() {
            await Reactor.Async.Future.Create((resolve, reject) => {
                var racer = Reactor.Async.Racer.Create();
                var count = 0;
                Reactor.Timeout.Create(() => racer.Set(() => {
                    if (count != 0) {
                        Assert.Fail("detected multiple callbacks.");
                        reject(new Exception("detected multiple callbacks."));
                        return;
                    }
                    count++;
                }));
                Reactor.Timeout.Create(() => racer.Set(() => {
                    if (count != 0) {
                        Assert.Fail("detected multiple callbacks.");
                        reject(new Exception("detected multiple callbacks."));
                        return;
                    }
                    count++;
                }));
                Reactor.Timeout.Create(() => {
                    if (count == 0) {
                        Assert.Fail("neither callback completed.");
                        reject(new Exception("neither callback completed."));
                        return;
                    }
                    if (count > 1) {
                        Assert.Fail("detected multiple callbacks.");
                        reject(new Exception("detected multiple callbacks."));
                        return;
                    }
                    resolve();
                }, 100);
            });
        }
    }
}
