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
using System.Text;

namespace Reactor {

    /// <summary>
    /// A specialized Ring Buffer / FIFO byte queue which supports 
    /// dynamic resize on capacity. It is passed on all Reactor IO read
    /// events.
    /// </summary>
    public class Buffer {

        private readonly int resize = Reactor.Settings.DefaultBufferSize;
        private Encoding     encoding;
        private int          capacity;
        private int          length;
        private int          head;
        private int          tail;
        private byte[]       buffer;

        #region Constructors

        public Buffer (int capacity) {
            this.encoding = Encoding.UTF8;
            this.capacity = capacity;
            this.length   = 0;
            this.head     = 0;
            this.tail     = 0;
            this.buffer   = new byte[capacity];
        }

        public Buffer () : this(Reactor.Settings.DefaultBufferSize) { }

        #endregion

        #region Propeties

        /// <summary>
        /// Gets or sets this buffers default text encoding. Default is UTF8.
        /// </summary>
        public Encoding Encoding {
            get {  return this.encoding; }
            set {  this.encoding = value; }
        }

        /// <summary>
        /// The total capacity for this buffer.
        /// </summary>
        public int  Capacity {
            get  { return this.capacity; }
        }

        /// <summary>
        /// The length of this buffer.
        /// </summary>
        public int  Length {
            get { return length; }
        }

        #endregion

        #region Write

        /// <summary>
        /// Writes this data to the buffer.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public void Write (byte[] data, int offset, int count) {
            //-----------------------------
            // ignore 0 counts.
            //-----------------------------
            if (count == 0) return;
            //-----------------------------------
            // here, we are being a bit lazy
            // and realigning the buffer to be
            // at offset 0. this makes the copy
            // operations a bit more tangible
            // later on, but needs to be optimized.
            //-----------------------------------
            if (offset > 0) {
                var temp = new byte[count];
                System.Buffer.BlockCopy(data, offset, temp, 0, count);
                data = temp;
            }
            //-----------------------------------
            // if the incoming data exceeds the 
            // maximum space of this buffer, we 
            // need to resize the buffer. here,
            // we calculate a new buffer based
            // on the resize, and copy the old
            // and new data into it. Indices
            // are reset and offsets begin at 0.
            //-----------------------------------
            var space = (this.capacity - this.length);
            if (count > space) {
                //------------------------------
                // calculate new buffer size. 
                //------------------------------
                var overflow    = count - space;
                var newsize     = this.capacity + overflow;
                var remainder   = newsize % this.resize;
                if (remainder > 0) {
                    newsize = newsize + (this.resize - remainder);
                }
                //------------------------------
                // copy old data to temp.
                //------------------------------
                var temp = new byte[newsize];
                if (this.head >= this.tail) {
                    var src_0        = this.head;
                    var dst_0        = 0;
                    var count_0      = (this.capacity - this.head);
                    System.Buffer.BlockCopy(this.buffer, src_0, temp, dst_0, count_0);
                    var src_1        = 0;
                    var dst_1        = count_0;
                    var count_1      = this.tail;
                    System.Buffer.BlockCopy(this.buffer, src_1, temp, dst_1, count_1);
                }
                else {
                    System.Buffer.BlockCopy(this.buffer, this.head, temp, 0, this.length);
                }
                //------------------------------
                // update indices.
                //------------------------------
                this.buffer   = temp;
                this.capacity = newsize;
                this.head     = 0;
                this.tail     = this.length;

                //------------------------------
                // copy new data.
                //------------------------------
                System.Buffer.BlockCopy(data, 0, this.buffer, this.tail, count);
                this.length = (this.length + count);
                this.tail   = (this.tail   + count) % this.capacity;
                return;
            }
            var overrun = (count + this.tail) - capacity;
            if (overrun > 0) {
                System.Buffer.BlockCopy(data, 0, this.buffer, this.tail, this.capacity - this.tail);
                System.Buffer.BlockCopy(data, this.capacity - this.tail, this.buffer, 0, count - (this.capacity - this.tail));
                this.length = (this.length + count);
                this.tail   = (this.tail   + count) % this.capacity;
            }
            else {
                System.Buffer.BlockCopy(data, 0, this.buffer, this.tail, count);
                this.length = (this.length + count);
                this.tail   = (this.tail   + count) % this.capacity;
            }
        }

        /// <summary>
        /// Writes a reactor buffer.
        /// </summary>
        /// <param name="buffer"></param>
        public void Write (Reactor.Buffer buffer) {
            this.Write(buffer.ToArray());
        }

        /// <summary>
        /// Writes a string to the buffer.
        /// </summary>
        /// <param name="data">The string to write.</param>
        public void Write  (string data) {
            var buffer = this.encoding.GetBytes(data);
            this.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Writes out a formatted string, using the same semantics as System.String.Format(System.String,System.Object).
        /// </summary>
        /// <param name="format">The formatting string.</param>
        /// <param name="args">The object array to write into the formatted string.</param>
        public void Write (string format, params object[] args) {
            format = string.Format(format, args);
            var buffer = this.encoding.GetBytes(format);
            this.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Writes this data to the buffer.
        /// </summary>
        /// <param name="data"></param>
        public void Write (byte [] data) {
            this.Write(data, 0, data.Length);
        }

        /// <summary>
        /// Writes the System.Boolean value to the buffer.
        /// </summary>
        /// <param name="value">The Boolean to write.</param>
        public void Write (bool value) {
            this.Write(value);
        }

        /// <summary>
        /// Writes a System.Int16 value to the buffer.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write (short value) {
            this.Write(value);
        }

        /// <summary>
        /// Writes a System.UInt16 value to the buffer.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write (ushort value) {
            this.Write(value);
        }

        /// <summary>
        /// Writes a System.Int32 value to the buffer.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write (int value) {
            this.Write(value);
        }

        /// <summary>
        /// Writes a System.UInt32 value to the buffer.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write (uint value) {
            this.Write(value);
        }

        /// <summary>
        /// Writes a System.Int64 value to the buffer.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write (long value) {
            this.Write(value);
        }

        /// <summary>
        /// Writes a System.UInt64 value to the buffer.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write (ulong value) {
            this.Write(value);
        }

        /// <summary>
        /// Writes a System.Single value to the buffer.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write (float value) {
            this.Write(value);
        }

        /// <summary>
        /// Writes a System.Double value to the buffer.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write (double value) {
            this.Write(value);
        }

        #endregion

        #region Unshift

        /// <summary>
        /// Unshifts this data to the front of the buffer.
        /// </summary>
        /// <param name="buffer">The buffer to unshift.</param>
        public void Unshift(byte[] data, int offset, int count)
        {
            //-----------------------------
            // ignore 0 counts.
            //-----------------------------
            if (count == 0) return;

            
            //-----------------------------------
            // here, we are being a bit lazy
            // and realigning the buffer to be
            // at offset 0. this makes the copy
            // operations a bit more tangible
            // later on, but needs to be optimized.
            //-----------------------------------
            if (offset > 0) {
                var temp = new byte[count];
                System.Buffer.BlockCopy(data, offset, temp, 0, count);
                data = temp;
            }

            //-----------------------------------
            // if the incoming data exceeds the 
            // maximum space of this buffer, we 
            // need to resize the buffer. here,
            // we calculate a new buffer based
            // on the resize, and copy the old
            // and new data into it. Indices
            // are reset and offsets begin at 0.
            //-----------------------------------        
            var space = (this.capacity - this.length);
            if (count > space) {
                var overflow    = count - space;
                var newsize     = this.capacity + overflow;
                var remainder   = newsize % this.resize;
                if (remainder > 0) {
                    newsize = newsize + (this.resize - remainder);
                }
                //------------------------------------
                // copy new buffer
                //------------------------------------
                var temp = new byte[newsize];
                System.Buffer.BlockCopy(data, 0, temp, 0, data.Length);
                //------------------------------------
                // copy old buffer
                //------------------------------------
                if (this.head >= this.tail) {
                    var src_0        = this.head;
                    var dst_0        = data.Length;
                    var count_0      = (this.capacity - this.head);
                    System.Buffer.BlockCopy(this.buffer, src_0, temp, dst_0, count_0);
                    var src_1        = 0;
                    var dst_1        = count_0 + dst_0;
                    var count_1      = this.tail;
                    System.Buffer.BlockCopy(this.buffer, src_1, temp, dst_1, count_1);
                }
                else {
                    System.Buffer.BlockCopy(this.buffer, this.head, temp, data.Length, this.length);
                }
                this.buffer   = temp;
                this.capacity = newsize;
                this.length   = this.length + data.Length;
                this.head     = 0;
                this.tail     = (this.length % this.capacity);
                return;
            }
            //---------------------------------
            // copy data
            //---------------------------------
            var overrun = (this.head - count);
            if (overrun < 0) {
                overrun = -overrun;
                System.Buffer.BlockCopy(data, 0, this.buffer, this.capacity - overrun, overrun);
                System.Buffer.BlockCopy(data, overrun, this.buffer, 0, count - overrun);
                this.length = (this.length + count);
                this.head   = this.capacity - overrun;
            }
            else {
                System.Buffer.BlockCopy(data, 0, this.buffer, this.head - count, count);
                this.length = (this.length + count);
                this.head   = (this.head   - count);
            }
        }

        /// <summary>
        /// Unshifts this data to the front of the buffer.
        /// </summary>
        /// <param name="buffer">The buffer to unshift.</param>
        public void Unshift (Reactor.Buffer buffer) {
            var data = buffer.ToArray();
            this.Unshift(data, 0, data.Length);
        }

        /// <summary>
        /// Unshifts this data to the front of the buffer.
        /// </summary>
        /// <param name="buffer"></param>
        public void Unshift (byte[] buffer) {
            this.Unshift(Reactor.Buffer.Create(buffer));
        }

        /// <summary>
        /// Unshifts this data to the front of the buffer.
        /// </summary>
        /// <param name="data"></param>
        public void Unshift (string data) {
            var buffer = this.encoding.GetBytes(data);
            this.Unshift(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Unshifts this data to the front of the buffer.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void Unshift (string format, params object[] args) {
            format = string.Format(format, args);
            var buffer = this.encoding.GetBytes(format);
            this.Unshift(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Writes this data to the buffer.
        /// </summary>
        /// <param name="data"></param>
        public void Unshift (char data) {
            this.Unshift(data.ToString());
        }

        /// <summary>
        /// Unshifts this data to the front of the buffer.
        /// </summary>
        /// <param name="data"></param>
        public void Unshift (byte data) {
            this.Unshift(new byte[1] { data });
        }

        /// <summary>
        /// Unshifts this data to the front of the buffer.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (bool value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts this data to the front of the buffer.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (short value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts this data to the front of the buffer.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (ushort value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts this data to the front of the buffer.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (int value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts this data to the front of the buffer.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (uint value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts this data to the front of the buffer.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (long value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts this data to the front of the buffer.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (ulong value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts this data to the front of the buffer.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (float value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts this data to the front of the buffer.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (double value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        #endregion

        #region Fill

        /// <summary>
        /// Fills the buffer with this value. If the buffer already
        /// contains data, it will only fill up to the remaining
        /// space left in the buffer.
        /// </summary>
        /// <param name="data">The value to fill with.</param>
        public void Fill (byte data) {
            var space = (this.capacity - this.length);
            var buffer = new byte[space];
            for (int i = 0; i < buffer.Length; i++) {
                buffer[i] = data;
            }
            this.Write(buffer);
        }

        #endregion

        #region Slice

        /// <summary>
        /// Slices a new buffer which references the same memory as the old.
        /// Warning: modifications on the new buffer will result in changes
        /// to the original.
        /// </summary>
        /// <param name="start">The starting index.</param>
        /// <param name="end">The end index.</param>
        /// <returns></returns>
        public Reactor.Buffer Slice (int start, int end) {
            var select = end - start;
            if (select < 0) {
                throw new Exception("buffer: the end is less than the start.");
            }
            if (start == 0 && end == 0) {
                var buffer = Reactor.Buffer.Create(0);
                buffer.encoding = this.encoding;
                buffer.buffer   = this.buffer;
                buffer.capacity = this.capacity;
                buffer.length   = this.length;
                buffer.head     = this.head;
                buffer.tail     = this.head;
                return buffer;
            }
            else {
                var buffer = Reactor.Buffer.Create(0);
                buffer.encoding = this.encoding;
                buffer.buffer   = this.buffer;
                buffer.capacity = this.capacity;
                buffer.length   = end - start;
                start           = start % this.length;
                end             = end   % this.length;
                buffer.head     = (this.head + start) % this.capacity;
                buffer.tail     = (this.tail + end)   % this.capacity;
                return buffer;
            }
        }

        /// <summary>
        /// Slices a new buffer which references the same memory as the old.
        /// Will slice from this start index to the this buffers length.
        /// Warning: modifications on the new buffer will result in changes
        /// to the original. 
        /// </summary>
        /// <param name="start">The start index to slice from.</param>
        /// <returns></returns>
        public Reactor.Buffer Slice(int start) {
            return this.Slice(start, this.buffer.Length);
        }

        #endregion

        #region Read

        /// <summary>
        /// Reads bytes from this buffer.
        /// </summary>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns></returns>
        public byte[]  Read (int count) {
            //----------------------------
            // ignore counts of 0
            //----------------------------
            if(count == 0) return new byte[0];
            //----------------------------
            // ignore when length is 0
            //----------------------------
            if(this.length == 0) return new byte[0];
            //----------------------------
            // adjust count to meet the length.
            //----------------------------
            if (count > this.length) count = this.length;
            //----------------------------
            // copy data.
            //----------------------------
            var data = new byte[count];
            var overrun = (count + this.head) - capacity;
            if (overrun > 0) {
                System.Buffer.BlockCopy(this.buffer, this.head, data, 0, this.capacity - this.head);
                System.Buffer.BlockCopy(this.buffer, 0, data, this.capacity - this.head, count - (this.capacity - this.head));
                this.length = (this.length - count);
                this.head   = (this.head   + count) % this.capacity;
            }
            else {
                System.Buffer.BlockCopy(this.buffer, this.head, data,  0, count);
                this.length = (this.length - count);
                this.head   = (this.head   + count) % this.capacity;
            }
            return data;
        }

        /// <summary>
        /// Reads all data in this buffer.
        /// </summary>
        /// <returns></returns>
        public byte[] Read() {
            return this.Read(this.length);
        }

        /// <summary>
        /// Reads bytes from this buffer.
        /// </summary>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns></returns>
        public byte [] ReadBytes (int count) {
            return this.Read(count);
        }

        /// <summary>
        /// Reads a single byte from this buffer.
        /// </summary>
        /// <returns></returns>
        public byte ReadByte () {
            var data = this.Read(1);
            return data[0];
        }

        /// <summary>
        /// Reads a boolean from this buffer.
        /// </summary>
        /// <returns></returns>
        public bool ReadBool () {
            var data = this.Read(sizeof(System.Boolean));
            return BitConverter.ToBoolean(data, 0);
        }

        /// <summary>
        /// Reads a Int16 value from this buffer.
        /// </summary>
        /// <returns></returns>
        public short ReadInt16 () {
            var data = this.Read(sizeof(System.Int16));
            return BitConverter.ToInt16(data, 0);
        }

        /// <summary>
        /// Reads a UInt16 value from this buffer.
        /// </summary>
        /// <returns></returns>
        public ushort ReadUInt16 () {
            var data = this.Read(sizeof(System.UInt16));
            return BitConverter.ToUInt16(data, 0);
        }

        /// <summary>
        /// Reads a Int32 value from this buffer.
        /// </summary>
        /// <returns></returns>
        public int ReadInt32 () {
            var data = this.Read(sizeof(System.Int32));
            return BitConverter.ToInt32(data, 0);
        }

        /// <summary>
        /// Reads a UInt32 value from this buffer.
        /// </summary>
        /// <returns></returns>
        public uint ReadUInt32 () {
            var data = this.Read(sizeof(System.UInt32));
            return BitConverter.ToUInt32(data, 0);
        }

        /// <summary>
        /// Reads a Int64 value from this buffer.
        /// </summary>
        /// <returns></returns>
        public long ReadInt64 () {
            var data = this.Read(sizeof(System.Int64));
            return BitConverter.ToInt64(data, 0);
        }

        /// <summary>
        /// Reads a UInt64 value from this buffer.
        /// </summary>
        /// <returns></returns>
        public ulong ReadUInt64 () {
            var data = this.Read(sizeof(System.UInt64));
            return BitConverter.ToUInt64(data, 0);
        }

        /// <summary>
        /// Reads a Single precision value from this buffer.
        /// </summary>
        /// <returns></returns>
        public float ReadSingle () {
            var data = this.Read(sizeof(System.Single));
            return BitConverter.ToSingle(data, 0);
        }

        /// <summary>
        /// Reads a Double precision value from this buffer.
        /// </summary>
        /// <returns></returns>
        public double ReadDouble () {
            var data = this.Read(sizeof(System.Double));
            return BitConverter.ToDouble(data, 0);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Clears the buffer.
        /// </summary>
        public void Clear() {
            this.length = 0;
            this.head   = 0;
            this.tail   = 0;
        }

        /// <summary>
        /// Clones this buffer.
        /// </summary>
        /// <returns></returns>
        public Reactor.Buffer Clone() {
            var buffer = Reactor.Buffer.Create(this.ToArray());
            buffer.encoding = this.encoding;
            return buffer;
        }

        /// <summary>
        /// Copies this buffer to a byte[].
        /// </summary>
        /// <returns></returns>
        public byte [] ToArray() {
            if(this.length == 0) return new byte[0];
            var dst = new byte[this.length];
            if (this.head >= this.tail) {
                var src_0        = this.head;
                var dst_0        = 0;
                var count_0      = (this.capacity - this.head);
                System.Buffer.BlockCopy(this.buffer, src_0, dst, dst_0, count_0);

                var src_1        = 0;
                var dst_1        = count_0;
                var count_1      = this.tail;
                System.Buffer.BlockCopy(this.buffer, src_1, dst, dst_1, count_1);
            }
            else {
                System.Buffer.BlockCopy(this.buffer, this.head, dst, 0, this.length);
            }
            return dst;
        }

        #endregion

        #region ToString

        /// <summary>
        /// Converts the contents of this buffer ToString.
        /// </summary>
        /// <param name="Encoding">The encoding to use.</param>
        /// <returns></returns>
        public string ToString (System.Text.Encoding Encoding) {
            return Encoding.GetString(this.ToArray());
        }

        /// <summary>
        /// Converts the contents of this buffer ToString.
        /// </summary>
        /// <param name="Encoding">The encoding to use.</param>
        /// <returns></returns>
        public string ToString (string encoding) {
            encoding = encoding.ToLower();
            switch(encoding) {
                case "ascii":   return System.Text.Encoding.ASCII.GetString(this.ToArray());
                case "utf8":    return System.Text.Encoding.UTF8.GetString(this.ToArray());
                case "utf7":    return System.Text.Encoding.UTF7.GetString(this.ToArray());
                case "utf32":   return System.Text.Encoding.UTF32.GetString(this.ToArray());
                case "unicode": return System.Text.Encoding.Unicode.GetString(this.ToArray());
                default: return System.Text.Encoding.UTF8.GetString(this.ToArray());
            }
        }

        /// <summary>
        /// Returns this buffer as a string using this buffers encoding.
        /// </summary>
        /// <returns></returns>
        public override string ToString () {
            return this.encoding.GetString(this.ToArray());
        }

        #endregion

        #region Statics

        /// <summary>
        /// Creates a new Reactor buffer.
        /// </summary>
        /// <returns></returns>
        public static Reactor.Buffer Create() {
            return new Reactor.Buffer();
        }

        /// <summary>
        /// Creates a Reactor Buffer with this starting capacity.
        /// </summary>
        /// <returns></returns>
        public static Reactor.Buffer Create(int capacity) {
            return new Reactor.Buffer(capacity);
        }

        /// <summary>
        /// Creates a new Reactor Buffer.
        /// </summary>
        /// <returns>A Buffer</returns>
        public static Reactor.Buffer Create(byte[] data, int index, int count) {
            var buffer = new Reactor.Buffer();
            buffer.Write(data, index, count);
            return buffer;
        }

        /// <summary>
        /// Creates a new Reactor Buffer.
        /// </summary>
        /// <returns>A Buffer</returns>
        public static Reactor.Buffer Create(byte[] data) {
            var buffer = new Reactor.Buffer();
            buffer.Write(data);
            return buffer;
        }

        /// <summary>
        /// Creates a new Reactor Buffer from a string.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Reactor.Buffer Create(string data) {
            var buffer = new Reactor.Buffer();
            buffer.Write(data);
            return buffer;
        }

        #endregion
    }
}