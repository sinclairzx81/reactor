
using System;
using System.Collections;
namespace Reactor.Fusion.Queues
{
    public class ByteQueue
    {
        private readonly byte[] _buffer;
        private int _startIndex, _endIndex;

        public ByteQueue(int capacity)
        {
            _buffer = new byte[capacity];
        }

        public ByteQueue() : this(32000)
        {
           
        }

        public int Length
        {
            get
            {
                if (_endIndex > _startIndex)
                    return _endIndex - _startIndex;
                if (_endIndex < _startIndex)
                    return (_buffer.Length - _startIndex) + _endIndex;
                return 0;
            }
        }

        public void Write(byte[] data)
        {
            if (Length + data.Length > _buffer.Length) throw new Exception("buffer overflow");
            if (_endIndex + data.Length >= _buffer.Length)
            {
                var endLen = _buffer.Length - _endIndex;
                var remainingLen = data.Length - endLen;

                Array.Copy(data, 0, _buffer, _endIndex, endLen);
                Array.Copy(data, endLen, _buffer, 0, remainingLen);
                _endIndex = remainingLen;
            }
            else
            {
                Array.Copy(data, 0, _buffer, _endIndex, data.Length);
                _endIndex += data.Length;
            }
        }

        public byte[] Read(int len, bool keepData = false)
        {
            if (len > Length) throw new Exception("not enough data in buffer");
            var result = new byte[len];
            if (_startIndex + len < _buffer.Length)
            {
                Array.Copy(_buffer, _startIndex, result, 0, len);
                if (!keepData) _startIndex += len;
                return result;
            }
            else
            {
                var endLen = _buffer.Length - _startIndex;
                var remainingLen = len - endLen;
                Array.Copy(_buffer, _startIndex, result, 0, endLen);
                Array.Copy(_buffer, 0, result, endLen, remainingLen);
                if (!keepData) _startIndex = remainingLen;
                return result;
            }
        }
    }
}
