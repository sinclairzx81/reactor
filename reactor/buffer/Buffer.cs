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
using System.IO;
using System.Text;

namespace Reactor {

    /// <summary>
    /// The Reactor Buffer is a general purpose, dynamically resizable buffer used to 
    /// encapsulate data passed on IO bound data events. Supports both read and write 
    /// operations and can be used as a general in memory storage object.
    /// </summary>
    public class Buffer : IDisposable {

        internal class Fields {
            public bool         disposed;
            public bool         locked;
            public MemoryStream stream;
            public Encoding     encoding;
            public int          head;
            public int          tail;
            public Fields() {
                this.disposed = false;
                this.locked   = false;
                this.stream   = new MemoryStream();
                this.head     = 0;
                this.tail     = 0;
                this.encoding = Encoding.UTF8;
            }
        } private Fields fields;

        #region Constructor

        private Buffer() {
            this.fields = new Fields();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets this buffers default text encoding. Default is UTF8.
        /// </summary>
        public Encoding Encoding {
            get { lock (this.fields) return this.fields.encoding; }
            set { lock (this.fields) this.fields.encoding = value; }
        }

        /// <summary>
        /// The capacity for this buffer.
        /// </summary>
        public int Capacity {
            get  { lock (this.fields) return this.fields.stream.Capacity; }
        }

        /// <summary>
        /// The length of this buffer.
        /// </summary>
        public int  Length {
            get { lock (this.fields) return this.fields.tail - this.fields.head; }
        }

        /// <summary>
        /// Gets the 'head' index of this buffer.
        /// </summary>
        public int Head {
            get { lock (this.fields) return this.fields.head; }
        }

        /// <summary>
        /// Gets the 'tail' index of this buffer.
        /// </summary>
        public int Tail { 
            get { lock (this.fields) return this.fields.tail; }
        }

        /// <summary>
        /// Gets or sets whether this buffer is locked.
        /// </summary>
        public bool Locked {
            get { lock(this.fields) return this.fields.locked; }
            set { lock(this.fields) this.fields.locked = value; }
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
            this.CheckDisposed();
            this.CheckLocked();
            lock (this.fields) {
                var length = (this.fields.tail - this.fields.head);
                this.fields.stream.Seek(this.fields.tail, SeekOrigin.Begin);
                this.fields.stream.Write(data, offset, count);
                this.fields.tail += count;
            }
        }

        /// <summary>
        /// Writes a reactor buffer.
        /// </summary>
        /// <param name="buffer"></param>
        public void Write (Reactor.Buffer buffer) {
            var array = buffer.ToArray();
            this.Write(array, 0, array.Length);
        }

        /// <summary>
        /// Writes a string to the buffer.
        /// </summary>
        /// <param name="data">The string to write.</param>
        public void Write  (string data) {
            var buffer = this.fields.encoding.GetBytes(data);
            this.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Writes out a formatted string, using the same semantics as System.String.Format(System.String,System.Object).
        /// </summary>
        /// <param name="format">The formatting string.</param>
        /// <param name="args">The object array to write into the formatted string.</param>
        public void Write (string format, params object[] args) {
            format = string.Format(format, args);
            var buffer = this.fields.encoding.GetBytes(format);
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
            this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.Int16 value to the buffer.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write (short value) {
           this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.UInt16 value to the buffer.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write (ushort value) {
            this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.Int32 value to the buffer.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write (int value) {
            this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.UInt32 value to the buffer.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write (uint value) {
            this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.Int64 value to the buffer.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write (long value) {
            this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.UInt64 value to the buffer.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write (ulong value) {
            this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.Single value to the buffer.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write (float value) {
            this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.Double value to the buffer.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write (double value) {
            this.Write(BitConverter.GetBytes(value));
        }

        #endregion

        #region Read

        /// <summary>
        /// Reads bytes from this buffer.
        /// </summary>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns></returns>
        public System.Byte[] Read (int count) {
            this.CheckDisposed();
            this.CheckLocked();
            lock (this.fields) {
                var length = this.fields.tail - this.fields.head;
                if (count > length) 
                    count = (int)length;

                var data = new byte[count];
                this.fields.stream.Seek(this.fields.head, SeekOrigin.Begin);
                var read = this.fields.stream.Read(data, 0, count);
                this.fields.head += read;

                // reset on length 0
                if ((this.fields.tail - this.fields.head) == 0) {
                    this.fields.stream.SetLength(0);
                    this.fields.head = 0;
                    this.fields.tail = 0;
                }
                return data;
            }
        }

        /// <summary>
        /// Reads bytes from this buffer.
        /// </summary>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns></returns>
        public System.Byte [] ReadBytes (int count) {
            return this.Read(count);
        }

        /// <summary>
        /// Reads a single byte from this buffer.
        /// </summary>
        /// <returns></returns>
        public System.Byte ReadByte () {
            var data = this.Read(1);
            return data[0];
        }

        /// <summary>
        /// Reads a boolean from this buffer.
        /// </summary>
        /// <returns></returns>
        public System.Boolean ReadBool () {
            var data = this.Read(sizeof(System.Boolean));
            return BitConverter.ToBoolean(data, 0);
        }

        /// <summary>
        /// Reads a Int16 value from this buffer.
        /// </summary>
        /// <returns></returns>
        public System.Int16 ReadInt16 () {
            var data = this.Read(sizeof(System.Int16));
            return BitConverter.ToInt16(data, 0);
        }

        /// <summary>
        /// Reads a UInt16 value from this buffer.
        /// </summary>
        /// <returns></returns>
        public System.UInt16 ReadUInt16 () {
            var data = this.Read(sizeof(System.UInt16));
            return BitConverter.ToUInt16(data, 0);
        }

        /// <summary>
        /// Reads a Int32 value from this buffer.
        /// </summary>
        /// <returns></returns>
        public System.Int32 ReadInt32 () {
            var data = this.Read(sizeof(System.Int32));
            return BitConverter.ToInt32(data, 0);
        }

        /// <summary>
        /// Reads a UInt32 value from this buffer.
        /// </summary>
        /// <returns></returns>
        public System.UInt32 ReadUInt32 () {
            var data = this.Read(sizeof(System.UInt32));
            return BitConverter.ToUInt32(data, 0);
        }

        /// <summary>
        /// Reads a Int64 value from this buffer.
        /// </summary>
        /// <returns></returns>
        public System.Int64 ReadInt64 () {
            var data = this.Read(sizeof(System.Int64));
            return BitConverter.ToInt64(data, 0);
        }

        /// <summary>
        /// Reads a UInt64 value from this buffer.
        /// </summary>
        /// <returns></returns>
        public System.UInt64 ReadUInt64 () {
            var data = this.Read(sizeof(System.UInt64));
            return BitConverter.ToUInt64(data, 0);
        }

        /// <summary>
        /// Reads a Single precision value from this buffer.
        /// </summary>
        /// <returns></returns>
        public System.Single ReadSingle () {
            var data = this.Read(sizeof(System.Single));
            return BitConverter.ToSingle(data, 0);
        }

        /// <summary>
        /// Reads a Double precision value from this buffer.
        /// </summary>
        /// <returns></returns>
        public System.Double ReadDouble () {
            var data = this.Read(sizeof(System.Double));
            return BitConverter.ToDouble(data, 0);
        }

        #endregion

        #region Unshift

        /// <summary>
        /// Unshifts this data onto the Buffer.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public void Unshift(byte[] data, int offset, int count) {
            this.CheckDisposed();
            this.CheckLocked();
            lock (this.fields) {
                var length = (this.fields.tail - this.fields.head);
                if ((this.fields.head - count) < 0) {
                    // read all data from the stream.
                    var buf = new byte[length];
                    this.fields.stream.Seek(this.fields.head, SeekOrigin.Begin);
                    var read = this.fields.stream.Read(buf, 0, (int)length);

                    // we seek count into the stream and write buf
                    this.fields.stream.Seek(0, SeekOrigin.Begin);
                    this.fields.stream.Write(data, offset, count);
                    this.fields.stream.Write(buf, 0, read);
                    this.fields.head = 0;
                    this.fields.tail = count + read;
                }
                else {
                    this.fields.stream.Seek(this.fields.head - count, SeekOrigin.Begin);
                    this.fields.stream.Write(data, 0, count);
                    this.fields.head -= count;
                }
            }
        }

        /// <summary>
        /// Unshifts this data to the front of the buffer.
        /// </summary>
        /// <param name="buffer">The buffer to unshift.</param>
        public void Unshift (Reactor.Buffer buffer) {
            var array = buffer.ToArray();
            this.Unshift(array, 0, array.Length);
        }

        /// <summary>
        /// Unshifts this data to the front of the buffer.
        /// </summary>
        /// <param name="buffer"></param>
        public void Unshift (byte[] buffer) {
            this.Unshift(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Unshifts this data to the front of the buffer.
        /// </summary>
        /// <param name="data"></param>
        public void Unshift (string data) {
            var buffer = this.fields.encoding.GetBytes(data);
            this.Unshift(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Unshifts this data to the front of the buffer.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void Unshift (string format, params object[] args) {
            format = string.Format(format, args);
            var buffer = this.fields.encoding.GetBytes(format);
            this.Unshift(buffer, 0, buffer.Length);
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

        #region Clone

        /// <summary>
        /// Clones this Buffer.
        /// </summary>
        /// <returns></returns>
        public Reactor.Buffer Clone() {
            lock (this.fields) {
                var len = (this.fields.tail - this.fields.head);
                var buf = new byte[len];
                this.fields.stream.Seek(this.fields.head, SeekOrigin.Begin);
                var read = this.fields.stream.Read(buf, 0, (int)len);
                return Reactor.Buffer.Create(buf);
            }
        }

        #endregion

        #region Clear

        /// <summary>
        /// Clears this Buffer.
        /// </summary>
        public void Clear() {
            lock (this.fields) {
                this.fields.stream.SetLength(0);
                this.fields.head = 0;
                this.fields.tail = 0;
            }
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
            return this.fields.encoding.GetString(this.ToArray());
        }

        #endregion

        #region ToArray

        /// <summary>
        /// returns the contents of this buffer as a byte array.
        /// </summary>
        /// <param name="value"></param>
        public byte[] ToArray() {
            lock (this.fields) {
                var len = (this.fields.tail - this.fields.head);
                var buf = new byte[len];
                this.fields.stream.Seek(this.fields.head, SeekOrigin.Begin);
                var read = this.fields.stream.Read(buf, 0, (int)len);
                return buf;
            }
        }

        #endregion

        #region IDisposable

        private void CheckLocked() {
            lock (this.fields) {
                if (this.fields.locked) {
                    throw new Exception("Buffer is locked.");
                }
            }
        }

        private void CheckDisposed() {
            lock (this.fields) {
                if (this.fields.disposed) {
                    throw new ObjectDisposedException("Reactor.Buffer", "Object has been disposed");
                }
            }
        }

        private void Dispose(bool disposing) {
            lock (this.fields) {
                if (!this.fields.disposed) {
                    this.fields.stream.Dispose();
                    this.fields.disposed = true;
                }
            }
        }

        public void Dispose() {
            this.Dispose(true);
        }

        ~Buffer() {
            this.Dispose(false);
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
        public static Reactor.Buffer Create(string text) {
            var data   = System.Text.Encoding.UTF8.GetBytes(text);
            var buffer = new Reactor.Buffer();
            buffer.Write(data);
            return buffer;
        }

        #endregion
    }
}
