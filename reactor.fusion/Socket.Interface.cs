using System;
using System.Collections.Generic;
using System.Text;

namespace Reactor.Fusion
{
    public partial class Socket: Reactor.IReadable, Reactor.IWriteable
    {
        #region IReadable

        public IReadable Pipe(IWriteable writeable)
        {
            this.OnData += (data) => writeable.Write(data);

            this.OnEnd  += ()     => writeable.End();

            if (writeable is IReadable)
            {
                return writeable as IReadable;
            }

            return null;
        }

        public void Pause()
        {
            
        }

        public void Resume()
        {
             
        }
        
        #endregion

        #region IWriteable

        public void Write(Buffer buffer)
        {
            this.Send(buffer.ToArray());
        }

        public void Write(string data)
        {
            var buffer = Encoding.UTF8.GetBytes(data);

            this.Send(buffer);
        }

        public void Write(string format, object arg0)
        {
            format = string.Format(format, arg0);

            var buffer = Encoding.UTF8.GetBytes(format);

            this.Send(buffer);
        }

        public void Write(string format, params object[] args)
        {
            format = string.Format(format, args);

            var buffer = Encoding.UTF8.GetBytes(format);

            this.Send(buffer);
        }

        public void Write(string format, object arg0, object arg1)
        {
            format = string.Format(format, arg0, arg1);

            var buffer = Encoding.UTF8.GetBytes(format);

            this.Send(buffer);
        }

        public void Write(string format, object arg0, object arg1, object arg2)
        {
            format = string.Format(format, arg0, arg1, arg2);

            var buffer = Encoding.UTF8.GetBytes(format);

            this.Send(buffer);
        }

        public void Write(byte data)
        {
            this.Send(new byte[1] { data });
        }

        public void Write(byte[] buffer)
        {
            this.Send(buffer);
        }

        public void Write(byte[] buffer, int index, int count)
        {
            var _buffer = new byte[count];

            System.Buffer.BlockCopy(buffer, index, _buffer, 0, count);

            this.Send(_buffer);
        }

        public void Write(bool value)
        {
            var buffer = BitConverter.GetBytes(value);

            this.Send(buffer);
        }

        public void Write(short value)
        {
            var buffer = BitConverter.GetBytes(value);

            this.Send(buffer);
        }

        public void Write(ushort value)
        {
            var buffer = BitConverter.GetBytes(value);

            this.Send(buffer);
        }

        public void Write(int value)
        {
            var buffer = BitConverter.GetBytes(value);

            this.Send(buffer);
        }

        public void Write(uint value)
        {
            var buffer = BitConverter.GetBytes(value);

            this.Send(buffer);
        }

        public void Write(long value)
        {
            var buffer = BitConverter.GetBytes(value);

            this.Send(buffer);
        }

        public void Write(ulong value)
        {
            var buffer = BitConverter.GetBytes(value);

            this.Send(buffer);
        }

        public void Write(float value)
        {
            var buffer = BitConverter.GetBytes(value);

            this.Send(buffer);
        }

        public void Write(double value)
        {
            var buffer = BitConverter.GetBytes(value);

            this.Send(buffer);
        }

        #endregion
    }
}
