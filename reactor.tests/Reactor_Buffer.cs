using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Reactor.Tests
{
    [TestClass]
    public class Reactor_Buffer {

        private void AssertBuffer(Reactor.Buffer buffer, 
                                  int capacity, 
                                  int length, 
                                  int head, 
                                  int tail,
                                  string message = "") {
            Assert.IsTrue(buffer.Capacity == capacity, "capacity : expected " + capacity + " got " + buffer.Capacity + ". on " + message); 
            Assert.IsTrue(buffer.Length   == length,   "length   : expected " + length   + " got " + buffer.Length   + ". on " + message); 
            Assert.IsTrue(buffer.Head     == head,     "head     : expected " + head     + " got " + buffer.Head     + ". on " + message);          
            Assert.IsTrue(buffer.Tail     == tail,     "tail     : expected " + tail     + " got " + buffer.Tail     + ". on " + message); 
        }

        private void AssertByteSequenceSame(byte [] src, 
                                            byte [] compare, 
                                            string message = "") {
            Assert.IsTrue(src.Length == compare.Length, "src and compared sequence length not the same.");
            for (int i = 0; i < src.Length; i++) {
                Assert.IsTrue(src[i] == compare[i], "byte at index " + i + " not equal.");
            }
        }

        [TestMethod]
        public void Create_Write_Full_Read_Full() {
            var buffer = Reactor.Buffer.Create(5, 5);
            AssertBuffer(buffer, 5, 0, 0, 0, "create buffer of capacity 5.");

            buffer.Write(new byte[5]);
            AssertBuffer(buffer, 5, 5, 0, 0, "write 5 bytes to buffer.");

            var data = buffer.Read(5);
            AssertBuffer(buffer, 5, 0, 0, 0, "read 5 bytes from buffer");
            AssertByteSequenceSame(data, new byte[5]);
        }

        [TestMethod]
        public void Create_Write_Partial_Read_Full() {
            var buffer = Reactor.Buffer.Create(5, 5);
            AssertBuffer(buffer, 5, 0, 0, 0, "create buffer of capacity 5.");
            buffer.Write(new byte[3]);
            AssertBuffer(buffer, 5, 3, 0, 3, "write 3 bytes to buffer.");
            buffer.Read(5);
            AssertBuffer(buffer, 5, 0, 3, 3, "read 5 bytes from buffer");          
        }

        [TestMethod]
        public void Create_Write_Partial_Read_Partial() {
            var buffer = Reactor.Buffer.Create(5, 5);
            AssertBuffer(buffer, 5, 0, 0, 0, "create buffer of capacity 5.");

            buffer.Write(new byte[3]);
            AssertBuffer(buffer, 5, 3, 0, 3, "write 3 bytes to buffer.");
            
            byte [] data = buffer.Read(1);
            AssertBuffer(buffer, 5, 2, 1, 3, "read 1 byte from buffer.");
            Assert.IsTrue(data.Length == 1, "verify length 1 of data.");
            
            data = buffer.Read(2);
            AssertBuffer(buffer, 5, 0, 3, 3, "read 2 bytes from buffer"); 
            Assert.IsTrue(data.Length == 2, "verify length 2 of data.");
        }

        [TestMethod]
        public void Create_Write_Partial_Unshift_Partial_Read_Full() {
            var buffer = Reactor.Buffer.Create(5, 5);
            AssertBuffer(buffer, 5, 0, 0, 0, "create buffer of capacity 5.");

            buffer.Write(new byte[3] { 0, 0, 0 });
            AssertBuffer(buffer, 5, 3, 0, 3, "write 3 bytes to buffer.");

            buffer.Unshift(new byte[2] {1, 1});
            AssertBuffer(buffer, 5, 5, 3, 3, "unshifted 2 bytes to buffer.");

            var data = buffer.Read(5);
            AssertBuffer(buffer, 5, 0, 3, 3, "read 5 bytes to buffer.");
            AssertByteSequenceSame(data, new byte[] {1, 1, 0, 0, 0}, "comparing byte output.");
        }

        [TestMethod]
        public void Create_Unshift_Partial_Write_Partial_Read_Full() {
            var buffer = Reactor.Buffer.Create(5, 5);
            AssertBuffer(buffer, 5, 0, 0, 0, "create buffer of capacity 5.");

            buffer.Unshift(new byte[2] {1, 1});
            AssertBuffer(buffer, 5, 2, 3, 0, "unshifted 2 bytes to buffer.");

            buffer.Write(new byte[3] { 0, 0, 0 });
            AssertBuffer(buffer, 5, 5, 3, 3, "write 3 bytes to buffer.");

            var data = buffer.Read(5);
            AssertBuffer(buffer, 5, 0, 3, 3, "read 5 bytes to buffer.");
            AssertByteSequenceSame(data, new byte[] {1, 1, 0, 0, 0}, "comparing byte output.");
        }

        [TestMethod]
        public void Create_Write_Full_Write_Resize_Read_Full() {
            var buffer = Reactor.Buffer.Create(5, 5);
            AssertBuffer(buffer, 5, 0, 0, 0, "create buffer of capacity 5.");

            buffer.Write(new byte[5] {0, 0, 0, 0, 0});
            AssertBuffer(buffer, 5, 5, 0, 0, "write 5 bytes to buffer.");

            /* note, we expect the buffer to be internally resize
             * when writing passed the end of the buffer, as such, 
             * the 'head' index is returned to 0 irrespective of
             * the state of the buffer. */
            buffer.Write(new byte[8]{ 1, 1, 1, 1, 1, 1, 1, 1 });
            AssertBuffer(buffer, 15, 13, 0, 13, "write 8 bytes to buffer.");

            var data = buffer.Read(13);
            AssertBuffer(buffer, 15, 0, 13, 13, "read 13 bytes from buffer.");
            AssertByteSequenceSame(data, new byte [13] {0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1});
        }

        [TestMethod]
        public void Create_Write_Partial_Write_Resize_Read_Full() {
            var buffer = Reactor.Buffer.Create(5, 5);
            AssertBuffer(buffer, 5, 0, 0, 0, "create buffer of capacity 5.");

            buffer.Write(new byte[3] {0, 0, 0});
            AssertBuffer(buffer, 5, 3, 0, 3, "write 3 bytes to buffer.");

            /* note, we expect the buffer to be internally resized
             * when writing passed the end of the buffer, as such, 
             * the 'head' index is returned to 0 irrespective of
             * the state of the buffer. */
            buffer.Write(new byte[8]{ 1, 1, 1, 1, 1, 1, 1, 1 });
            AssertBuffer(buffer, 15, 11, 0, 11, "write 8 bytes to buffer.");

            var data = buffer.Read(11);
            AssertBuffer(buffer, 15, 0, 11, 11, "read 11 bytes from buffer.");
            AssertByteSequenceSame(data, new byte [11] {0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1});
        }

        [TestMethod]
        public void Create_Unshift_Full_Unshift_Resize_Read_Full() {
            var buffer = Reactor.Buffer.Create(5, 5);
            AssertBuffer(buffer, 5, 0, 0, 0, "create buffer of capacity 5.");

            buffer.Unshift(new byte[5] {0, 0, 0, 0, 0});
            AssertBuffer(buffer, 5, 5, 0, 0, "unshift 5 bytes to buffer.");

            /* note, we expect the buffer to be internally resize
             * when unshifted passed the start of the buffer, as such, 
             * the 'head' index is returned to 0 irrespective of
             * the state of the buffer. */
            buffer.Unshift(new byte[8]{ 1, 1, 1, 1, 1, 1, 1, 1 });
            AssertBuffer(buffer, 15, 13, 0, 13, "unshift 8 bytes to buffer.");

            var data = buffer.Read(13);
            AssertBuffer(buffer, 15, 0, 13, 13, "read 13 bytes from buffer.");
            AssertByteSequenceSame(data, new byte [13] {1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0});
        }

        [TestMethod]
        public void Create_Unshift_Partial_Unshift_Resize_Read_Full() {
            var buffer = Reactor.Buffer.Create(5, 5);
            AssertBuffer(buffer, 5, 0, 0, 0, "create buffer of capacity 5.");

            buffer.Unshift(new byte[3] {0, 0, 0});
            AssertBuffer(buffer, 5, 3, 2, 0, "unshift 3 bytes to buffer.");

            /* note, we expect the buffer to be internally resized
             * when writing passed the end of the buffer, as such, 
             * the 'head' index is returned to 0 irrespective of
             * the state of the buffer. */
            buffer.Unshift(new byte[8]{ 1, 1, 1, 1, 1, 1, 1, 1 });
            AssertBuffer(buffer, 15, 11, 0, 11, "unshift 8 bytes to buffer.");

            var data = buffer.Read(11);
            AssertBuffer(buffer, 15, 0, 11, 11, "read 11 bytes from buffer.");
            AssertByteSequenceSame(data, new byte [11] {1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0});
        }
    }
}
