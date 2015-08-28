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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Reactor.Tests
{
    [TestClass]
    public class Reactor_Buffer {

        #region Helpers

        private void AssertBuffer(Reactor.Buffer buffer, 
                                  int capacity, 
                                  int length, 
                                  int head, 
                                  int tail,
                                  string message = "") {
            Assert.IsTrue(buffer.Capacity == capacity, "CAPACITY : expected " + capacity + " got " + buffer.Capacity + ". on " + message); 
            Assert.IsTrue(buffer.Length   == length,   "LENGTH : expected " + length   + " got " + buffer.Length   + ". on " + message); 
            Assert.IsTrue(buffer.Head     == head,     "HEAD : expected " + head     + " got " + buffer.Head     + ". on " + message);          
            Assert.IsTrue(buffer.Tail     == tail,     "TAIL : expected " + tail     + " got " + buffer.Tail     + ". on " + message); 
        }

        private void AssertByteSequenceSame(byte [] src, 
                                            byte [] compare, 
                                            string message = "") {
            Assert.IsTrue(src.Length == compare.Length, "src and compared sequence length not the same.");
            for (int i = 0; i < src.Length; i++) {
                Assert.IsTrue(src[i] == compare[i], "byte at index " + i + " not equal.");
            }
        }

        #endregion

        [TestMethod]
        [TestCategory("Reactor.Buffer")]
        public void Buffer_Write() {
           
        }
    }
}
