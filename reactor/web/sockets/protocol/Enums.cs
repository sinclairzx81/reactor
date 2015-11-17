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


namespace Reactor.Web {

    internal enum Fin : byte {
        More = 0x0,
        Final = 0x1
    }

    internal enum Mask : byte {
        Unmask = 0x0,
        Mask = 0x1
    }

    internal enum Rsv : byte {
        Off = 0x0,
        On = 0x1
    }

    internal enum Opcode : byte {
        /// <summary>
        /// Equivalent to numeric value 0. Indicates a continuation frame.
        /// </summary>
        CONT = 0x0,
        /// <summary>
        /// Equivalent to numeric value 1. Indicates a text frame.
        /// </summary>
        TEXT = 0x1,
        /// <summary>
        /// Equivalent to numeric value 2. Indicates a binary frame.
        /// </summary>
        BINARY = 0x2,
        /// <summary>
        /// Equivalent to numeric value 8. Indicates a connection close frame.
        /// </summary>
        CLOSE = 0x8,
        /// <summary>
        /// Equivalent to numeric value 9. Indicates a ping frame.
        /// </summary>
        PING = 0x9,
        /// <summary>
        /// Equivalent to numeric value 10. Indicates a pong frame.
        /// </summary>
        PONG = 0xa
    }

    internal enum CloseStatusCode : ushort {
        /// <summary>
        /// Equivalent to close status 1000.
        /// Indicates a normal closure.
        /// </summary>
        Normal = 1000,
        /// <summary>
        /// Equivalent to close status 1001.
        /// Indicates that an endpoint is going away.
        /// </summary>
        Away = 1001,
        /// <summary>
        /// Equivalent to close status 1002.
        /// Indicates that an endpoint is terminating the connection due to a protocol error.
        /// </summary>
        ProtocolError = 1002,
        /// <summary>
        /// Equivalent to close status 1003.
        /// Indicates that an endpoint is terminating the connection because it has received an
        /// unacceptable type message.
        /// </summary>
        IncorrectData = 1003,
        /// <summary>
        /// Equivalent to close status 1004.
        /// Still undefined. Reserved value.
        /// </summary>
        Undefined = 1004,
        /// <summary>
        /// Equivalent to close status 1005.
        /// Indicates that no status code was actually present. Reserved value.
        /// </summary>
        NoStatusCode = 1005,
        /// <summary>
        /// Equivalent to close status 1006.
        /// Indicates that the connection was closed abnormally. Reserved value.
        /// </summary>
        Abnormal = 1006,
        /// <summary>
        /// Equivalent to close status 1007.
        /// Indicates that an endpoint is terminating the connection because it has received a message
        /// that contains a data that isn't consistent with the type of the message.
        /// </summary>
        InconsistentData = 1007,
        /// <summary>
        /// Equivalent to close status 1008.
        /// Indicates that an endpoint is terminating the connection because it has received a message
        /// that violates its policy.
        /// </summary>
        PolicyViolation = 1008,
        /// <summary>
        /// Equivalent to close status 1009.
        /// Indicates that an endpoint is terminating the connection because it has received a message
        /// that is too big to process.
        /// </summary>
        TooBig = 1009,
        /// <summary>
        /// Equivalent to close status 1010.
        /// Indicates that the client is terminating the connection because it has expected the server
        /// to negotiate one or more extension, but the server didn't return them in the handshake
        /// response.
        /// </summary>
        IgnoreExtension = 1010,
        /// <summary>
        /// Equivalent to close status 1011.
        /// Indicates that the server is terminating the connection because it has encountered an
        /// unexpected condition that prevented it from fulfilling the request.
        /// </summary>
        ServerError = 1011,
        /// <summary>
        /// Equivalent to close status 1015.
        /// Indicates that the connection was closed due to a failure to perform a TLS handshake.
        /// Reserved value.
        /// </summary>
        TlsHandshakeFailure = 1015
    }

    /// <summary>
    /// Contains the values that indicate whether the byte order is a Little-endian or Big-endian.
    /// </summary>
    internal enum ByteOrder : byte {
        /// <summary>
        /// Indicates a Little-endian.
        /// </summary>
        Little,

        /// <summary>
        /// Indicates a Big-endian.
        /// </summary>
        Big
    }
}
