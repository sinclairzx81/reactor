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

using System.IO;
using System.Text;

namespace Reactor
{
    /// <summary>
    /// The Reactor Buffer is a general purpose, dynamically resizable buffer used to 
    /// encapsulate data passed on IO bound data events. Supports both read and write 
    /// operations and can be used as a general in memory storage object.
    /// </summary>
    public class Buffer
    {
        private MemoryStream stream;

        private BinaryReader reader;

        private BinaryWriter writer;

        public Buffer()
        {
            this.stream = new MemoryStream();

            this.reader = new BinaryReader(stream);

            this.writer = new BinaryWriter(stream);
        }

        public Buffer(byte [] data, int index, int count)
        {
            this.stream = new MemoryStream(data, index, count);

            this.reader = new BinaryReader(stream);

            this.writer = new BinaryWriter(stream);
        }

        public Buffer(byte[] data) : this(data, 0, data.Length)
        {

        }

        #region Properties

        /// <summary>
        /// The size of this buffer in bytes.
        /// </summary>
        public long Length
        {
            get
            {
                return this.stream.Length;
            }
        }

        #endregion

        #region Write

        /// <summary>
        /// Writes a buffer at the end of this buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        public void Write(Buffer buffer)
        {
            var _buffer = buffer.stream.ToArray();

            this.writer.Write(_buffer, 0, _buffer.Length);
        }

        /// <summary>
        /// Writes a string to the buffer.
        /// </summary>
        /// <param name="data">The string to write.</param>
        public void Write(string data)
        {
            var buffer = Encoding.UTF8.GetBytes(data);

            this.writer.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Writes out a formatted string, using the same semantics as System.String.Format(System.String,System.Object).
        /// </summary>
        /// <param name="format">The formatting string.</param>
        /// <param name="arg0">An object to write into the formatted string.</param>
        public void Write(string format, object arg0)
        {
            format = string.Format(format, arg0);

            var buffer = Encoding.UTF8.GetBytes(format);

            this.writer.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Writes out a formatted string, using the same semantics as System.String.Format(System.String,System.Object).
        /// </summary>
        /// <param name="format">The formatting string.</param>
        /// <param name="args">The object array to write into the formatted string.</param>
        public void Write(string format, params object[] args)
        {
            format = string.Format(format, args);

            var buffer = Encoding.UTF8.GetBytes(format);

            this.writer.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Writes out a formatted string, using the same semantics as System.String.Format(System.String,System.Object).
        /// </summary>
        /// <param name="format">The formatting string.</param>
        /// <param name="arg0">An object to write into the formatted string.</param>
        /// <param name="arg1">An object to write into the formatted string.</param>
        public void Write(string format, object arg0, object arg1)
        {
            format = string.Format(format, arg0, arg1);

            var buffer = Encoding.UTF8.GetBytes(format);

            this.writer.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Writes out a formatted string, using the same semantics as System.String.Format(System.String,System.Object).
        /// </summary>
        /// <param name="format">The formatting string.</param>
        /// <param name="arg0">An object to write into the formatted string.</param>
        /// <param name="arg1">An object to write into the formatted string.</param>
        /// <param name="arg2">An object to write into the formatted string.</param>
        public void Write(string format, object arg0, object arg1, object arg2)
        {
            format = string.Format(format, arg0, arg1, arg2);

            var buffer = Encoding.UTF8.GetBytes(format);

            this.writer.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Writes a byte to the buffer.
        /// </summary>
        /// <param name="data">The byte to write</param>
        public void Write(byte data)
        {
            this.writer.Write(data);
        }

        /// <summary>
        /// Writes an array of bytes to the buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        public void Write(byte[] buffer)
        {
            this.writer.Write(buffer);
        }

        /// <summary>
        /// Writes a subarray of bytes to the buffer.
        /// </summary>
        /// <param name="buffer">The byte array to write data from.</param>
        /// <param name="index">Starting index in the buffer.</param>
        /// <param name="count">The number of bytes to write.</param>
        public void Write(byte[] buffer, int index, int count)
        {
            this.writer.Write(buffer, index, count);
        }

        /// <summary>
        /// Writes the System.Boolean value to the buffer.
        /// </summary>
        /// <param name="value">The Boolean to write.</param>
        public void Write(bool value)
        {
            this.writer.Write(value);
        }

        /// <summary>
        /// Writes a System.Int16 value to the buffer.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write(short value)
        {
            this.writer.Write(value);
        }

        /// <summary>
        /// Writes a System.UInt16 value to the buffer.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write(ushort value)
        {
            this.writer.Write(value);
        }

        /// <summary>
        /// Writes a System.Int32 value to the buffer.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write(int value)
        {
            this.writer.Write(value);
        }

        /// <summary>
        /// Writes a System.UInt32 value to the buffer.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write(uint value)
        {
            this.writer.Write(value);
        }

        /// <summary>
        /// Writes a System.Int64 value to the buffer.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write(long value)
        {
            this.writer.Write(value);
        }

        /// <summary>
        /// Writes a System.UInt64 value to the buffer.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write(ulong value)
        {
            this.writer.Write(value);
        }

        /// <summary>
        /// Writes a System.Single value to the buffer.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write(float value)
        {
            this.writer.Write(value);
        }

        /// <summary>
        /// Writes a System.Double value to the buffer.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write(double value)
        {
            this.writer.Write(value);
        }

        #endregion

        #region Read

        public byte ReadByte()
        {
            return this.reader.ReadByte();
        }

        public byte[] ReadBytes(int count)
        {
            return this.reader.ReadBytes(count);
        }

        public bool ReadBool()
        {
            return this.reader.ReadBoolean();
        }

        public short ReadInt16()
        {
            return this.reader.ReadInt16();
        }

        public ushort ReadUInt16()
        {
            return this.reader.ReadUInt16();
        }

        public int ReadInt32()
        {
            return this.reader.ReadInt32();
        }

        public uint ReadUInt32()
        {
            return this.reader.ReadUInt32();
        }

        public long ReadInt64()
        {
            return this.reader.ReadInt64();
        }

        public ulong ReadUInt64()
        {
            return this.reader.ReadUInt64();
        }

        public float ReadSingle()
        {
            return this.reader.ReadSingle();
        }

        public double ReadDouble()
        {
            return this.reader.ReadDouble();
        }

        #endregion

        #region Methods

        public void Seek(int position)
        {
            this.stream.Seek(position, SeekOrigin.Begin);
        }

        public void SetLength(int size)
        {
            this.stream.SetLength(size);
        }

        public byte [] ToArray()
        {
            return this.stream.ToArray();
        }
        
        public string ToString(System.Text.Encoding Encoding)
        {
            return Encoding.GetString(this.stream.ToArray());
        }

        public string ToString(string encoding)
        {
            encoding = encoding.ToLower();

            switch(encoding)
            {
                case "ascii":   return Encoding.ASCII.GetString(this.stream.ToArray());

                case "utf8":    return Encoding.UTF8.GetString(this.stream.ToArray());

                case "utf7":    return Encoding.UTF7.GetString(this.stream.ToArray());

                case "utf32":   return Encoding.UTF32.GetString(this.stream.ToArray());

                case "unicode": return Encoding.Unicode.GetString(this.stream.ToArray());

                default: return Encoding.UTF8.GetString(this.stream.ToArray());
            }
        }

        #endregion

        #region Statics

        /// <summary>
        /// Creates a new Reactor Buffer.
        /// </summary>
        /// <returns>A Buffer</returns>
        public static Buffer Create()
        {
            return new Buffer();
        }



        /// <summary>
        /// Creates a new Reactor Buffer.
        /// </summary>
        /// <returns>A Buffer</returns>
        public static Buffer Create(byte[] data, int index, int count)
        {
            return new Buffer(data, index, count);
        }

        /// <summary>
        /// Creates a new Reactor Buffer.
        /// </summary>
        /// <returns>A Buffer</returns>
        public static Buffer Create(byte[] data)
        {
            return new Buffer(data);
        }

        /// <summary>
        /// Creates a new Reactor Buffer from a string.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Buffer Create(string data)
        {
            var buffer = System.Text.Encoding.UTF8.GetBytes(data);

            return new Reactor.Buffer(buffer, 0, buffer.Length);
        }

        #endregion

        #region IDisposable

        private bool disposed = false;

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!this.disposed)
                {
                    this.stream.Dispose();

                    this.disposed = true;
                }
            }
        }

        /// <summary>
        /// Disposes of this Buffer.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }

        ~Buffer()
        {
            this.Dispose(false);
        }

        #endregion
    }
}
