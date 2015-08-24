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

using System.Net.Sockets;

namespace Reactor.Tls
{
    /// <summary>
    /// Defines the type of the value stored within a option.
    /// </summary>
    internal enum OptionValueType {
        Object,
        ByteArray,
        Boolean,
        Int32
    }

    /// <summary>
    /// An option used to initialize a socket.
    /// </summary>
    public class Option {

        internal OptionValueType ValueType         { get; set; }
        public SocketOptionLevel SocketOptionLevel { get; set; }
        public SocketOptionName  SocketOptionName  { get; set; }
        public object            Value             { get; set; }

        private Option() { }

        /// <summary>
        /// Creates a new option with these arguments.
        /// </summary>
        public static Option Create(SocketOptionLevel socketOptionLevel, SocketOptionName  socketOptionName, System.Object value) {
            return new Option {
                ValueType = OptionValueType.Object,
                SocketOptionLevel = socketOptionLevel,
                SocketOptionName = socketOptionName,
                Value = value
            };
        }

        /// <summary>
        /// Creates a new option with these arguments.
        /// </summary>
        public static Option Create(SocketOptionLevel socketOptionLevel, SocketOptionName  socketOptionName, System.Boolean value) {
            return new Option {
                ValueType = OptionValueType.Boolean,
                SocketOptionLevel = socketOptionLevel,
                SocketOptionName = socketOptionName,
                Value = value
            };
        }

        /// <summary>
        /// Creates a new option with these arguments.
        /// </summary>
        public static Option Create(SocketOptionLevel socketOptionLevel, SocketOptionName  socketOptionName, System.Byte [] value) {
            return new Option {
                ValueType = OptionValueType.ByteArray,
                SocketOptionLevel = socketOptionLevel,
                SocketOptionName = socketOptionName,
                Value = value
            };
        }

        /// <summary>
        /// Creates a new option with these arguments.
        /// </summary>
        public static Option Create(SocketOptionLevel socketOptionLevel, SocketOptionName  socketOptionName, System.Int32 value) {
            return new Option {
                ValueType = OptionValueType.Int32,
                SocketOptionLevel = socketOptionLevel,
                SocketOptionName = socketOptionName,
                Value = value
            };
        }
    }
}
