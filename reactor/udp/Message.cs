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

namespace Reactor.Udp {
    
    /// <summary>
    /// Encapsulates data passed over a udp socket.
    /// </summary>
    public class Message {
        /// <summary>
        /// The target endpoint for this message.
        /// </summary>
        public System.Net.EndPoint EndPoint {  get; set; }
        /// <summary>
        /// The data passed along with this message.
        /// </summary>
        public Reactor.Buffer Buffer {  get; set; }

        #region Constructors

        /// <summary>
        /// Creates a new UDP message.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="buffer"></param>
        public Message(System.Net.EndPoint endpoint, Reactor.Buffer buffer) {
            this.EndPoint = endpoint;
            this.Buffer   = buffer;
        }

        /// <summary>
        /// Creates a new UDP message.
        /// </summary>
        public Message(): this(new System.Net.IPEndPoint(System.Net.IPAddress.Any, 0), Reactor.Buffer.Create()) {
        }

        #endregion

        #region Statics

        /// <summary>
        /// Returns a new message.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static Message Create(System.Net.EndPoint endpoint, Reactor.Buffer buffer) {
            return new Message(endpoint, buffer);
        }

        /// <summary>
        /// Returns a new message.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Message Create(System.Net.EndPoint endpoint, byte [] data) {
            return new Message(endpoint, Reactor.Buffer.Create(data));
        }

        /// <summary>
        /// Creates a new message.
        /// </summary>
        /// <returns></returns>
        public static Message Create() {
            return new Message(new System.Net.IPEndPoint(System.Net.IPAddress.Any, 0), Reactor.Buffer.Create());
        }

        #endregion
    }
}
