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
using System.IO;
using System.Threading.Tasks;

namespace Reactor.Tests {

    [TestClass]
    public class Reactor_File_Reader {
        private static string sequential_file;
        private static string sequential_file_piped;

        [ClassInitialize]
        public static void Startup(TestContext context) {
            sequential_file       = context.TestResultsDirectory + "/sequential.dat";
            sequential_file_piped = context.TestResultsDirectory + "/sequential_piped.dat";
            Reactor.Tests.Utility.Files.CreateNumbericSequenceFile(sequential_file, 65536 * 16);
            Reactor.Loop.Start();
        }

        [ClassCleanup]
        public static void Shutdown() {
            Reactor.Loop.Stop();
            Reactor.Tests.Utility.Files.Delete(sequential_file);
        }

        [TestMethod]
        [TestCategory("Reactor.File.Reader")]
        public async Task Reader_Read_All_Compare_Length () {
            var future = Reactor.Async.Future.Create((resolve, reject) => {
                var received = 0;
                var reader   = Reactor.File.Reader.Create(sequential_file);
                reader.OnRead(buffer => {
                    received += buffer.Length;
                });
                reader.OnError(reject);
                reader.OnEnd(() => {
                    if (received != reader.Length) {
                        reject(new Exception("number of bytes read incorrect. expected " + reader.Length + " got " + received));
                        return;
                    }
                    resolve();
                });
            });
            Reactor.Timeout.Create(future.Cancel, 1000);
            await future;
        }

        [TestMethod]
        [TestCategory("Reactor.File.Reader")]
        public async Task Reader_Read_All_Sequential_Flowing () {
            var future = Reactor.Async.Future.Create((resolve, reject) => {
                var index    = -1;
                var reader   = Reactor.File.Reader.Create(sequential_file);
                var expected = (reader.Length / 4) - 1;
                reader.OnRead(buffer => {
                    while (buffer.Length > 0) {
                        if (buffer.Length >= 4) {
                            var temp = buffer.ReadInt32();
                            if (temp != (index + 1)) {
                                reject(new System.Exception("read unexpected ordinal."));
                                return;
                            }
                            index = temp;
                        }
                        else {
                            reader.Unshift(buffer);
                            break;
                        }
                    }
                });
                reader.OnError(reject);
                reader.OnEnd(() => {
                    if (index != expected) {
                        reject(new System.Exception("expected index " + index + " got " + expected));
                        return;
                    }
                    resolve();
                });
            });
            Reactor.Timeout.Create(future.Cancel, 1000);
            await future;
        }

        [TestMethod]
        [TestCategory("Reactor.File.Reader")]
        public async Task Reader_Read_All_Sequential_NonFlowing () {
            var future = Reactor.Async.Future.Create((resolve, reject) => {
                var index    = -1;
                var reader   = Reactor.File.Reader.Create(sequential_file);
                var expected = (reader.Length / 4) - 1;
                reader.OnReadable(() => {
                    var buffer = reader.Read();
                    while (buffer.Length > 0) {
                       if (buffer.Length >= 4) {
                            var temp = buffer.ReadInt32();
                            if (temp != (index + 1)) {
                                reject(new System.Exception("read unexpected ordinal."));
                                return;
                            }
                            index = temp;
                        }
                        else {
                            reader.Unshift(buffer);
                            break;
                        }
                    }
                });
                reader.OnError(reject);
                reader.OnEnd(() => {
                    if (index != expected) {
                        reject(new System.Exception("expected index " + index + " got " + expected));
                        return;
                    }
                    resolve();
                });
            });
            Reactor.Timeout.Create(future.Cancel, 1000);
            await future;
        }

        [TestMethod]
        [TestCategory("Reactor.File.Reader")]
        public async Task Reader_Read_Partial_Sequential_Flowing() {
            var future = Reactor.Async.Future.Create((resolve, reject) => {
                //-----------------------------
                // skip 100 ints, take 100..
                //-----------------------------
                var reader   = Reactor.File.Reader.Create(sequential_file, 100 * 4, 100 * 4);
                var expected = 199;
                var index    = 99;
                reader.OnRead(buffer => {
                    while (buffer.Length > 0) {
                        if (buffer.Length >= 4) {
                            var temp = buffer.ReadInt32();
                            if (temp != (index + 1)) {
                                reject(new System.Exception("read unexpected ordinal."));
                                return;
                            }
                            index = temp;
                        }
                        else {
                            reader.Unshift(buffer);
                            break;
                        }
                    }
                });
                reader.OnError(reject);
                reader.OnEnd(() => {
                    if (index != expected) {
                        reject(new System.Exception("expected index " + index + " got " + expected));
                        return;
                    }
                    resolve();
                });
            });
            Reactor.Timeout.Create(future.Cancel, 1000);
            await future;
        }

        [TestMethod]
        [TestCategory("Reactor.File.Reader")]
        public async Task Reader_Pipe_File_Sequential() {
            var future = Reactor.Async.Future.Create((resolve, reject) => {
                var writer = Reactor.File.Writer.Create(sequential_file_piped, FileMode.OpenOrCreate);
                var reader = Reactor.File.Reader.Create(sequential_file);
                reader.Pipe(writer);
                writer.OnEnd(() => {
                    if (!Reactor.Tests.Utility.Files.AreSame(sequential_file_piped, sequential_file)) {
                        reject(new Exception("files are not the same"));
                        return;
                    }
                    resolve();
                });
            });
            Reactor.Timeout.Create(future.Cancel, 1000);
            await future;
        }
    }
}
