/*--------------------------------------------------------------------------

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

using System;

namespace Reactor
{
    public interface IWriteable
    {
        void Write(Buffer buffer);

        void Write(string data);

        void Write(string format, object arg0);

        void Write(string format, params object[] args);

        void Write(string format, object arg0, object arg1);

        void Write(string format, object arg0, object arg1, object arg2);

        void Write(byte data);

        void Write(byte[] buffer);

        void Write(byte[] buffer, int index, int count);

        void Write(bool value);

        void Write(short value);

        void Write(ushort value);

        void Write(int value);

        void Write(uint value);

        void Write(long value);

        void Write(ulong value);

        void Write(float value);

        void Write(double value);

        void End();

        event Action<Exception> OnError;
    }
}
