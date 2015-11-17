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

using Reactor.Net;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Reactor.Web {

    /// <summary>
    /// Reactor Web Socket Handler.
    /// </summary>
    public class SocketRouter {

        #region Constants
        private static string guid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        private static string ctlf = "\r\n";
        #endregion

        #region Fields

        private string path;
        private Reactor.Action<Reactor.Web.Socket> onsocket;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new web socket at this path.
        /// </summary>
        /// <param name="path">The root relative path in which to initialize this socket.</param>
        public SocketRouter(string path, Reactor.Action<Reactor.Web.Socket> onsocket) {
            this.path     = path;
            this.onsocket = onsocket;
        }

        #endregion

        #region Process

        /// <summary>
        /// Processes a incoming http request.
        /// </summary>
        /// <param name="context">The http context.</param>
        /// <param name="next">The next function.</param>
        public void Process(Reactor.Http.Context context, Reactor.Action<Reactor.Http.Context> next) {
            var path       = context.Request.Url.AbsolutePath;
            var method     = context.Request.Method;
            var protocol   = context.Request.ProtocolVersion;
            var upgrade    = context.Request.Headers["Upgrade"];
            var connection = context.Request.Headers["Connection"];

            // check for web socket request.
            if (path       == this.path &&
                protocol   != null && protocol == HttpVersion.Version11 && 
                method     != null && method.ToLower() == "get" && 
                upgrade    != null && upgrade.ToLower().Contains("websocket") && 
                connection != null && connection.ToLower().Contains("upgrade")) {

                // initialize socket header..
                var secWebSocketExtensions = context.Request.Headers["Sec-WebSocket-Extensions"];
                var secWebSocketKey        = context.Request.Headers["Sec-WebSocket-Key"];
                var secWebSocketVersion    = context.Request.Headers["Sec-WebSocket-Version"];
                var headers = new Dictionary<string, string>() {
                    { "Upgrade", "websocket" },
                    { "Connection", "Upgrade" },
                    { "Sec-WebSocket-Accept", this.CreateResponseKey(secWebSocketKey) }
                };
                
                // write protocol upgrade response.
                var buffer = Reactor.Buffer.Create();
                var reason = "upgrade";
                buffer.Write(string.Format("HTTP/{0} {1} {2}{3}", protocol, "101", reason, ctlf));
                foreach (var pair in headers) {
                    buffer.Write(string.Format("{0}: {1}{2}", pair.Key, pair.Value, ctlf));
                } buffer.Write(ctlf);

                // emit socket to server.
                context.Transport.Write(buffer).Then(() =>
                    this.onsocket(new Socket(context.Transport)));
                   
            }
            else next(context);
        }

        /// <summary>
        /// Creates the web socket response key.
        /// </summary>
        /// <param name="secWebSocketKey">The Sec-WebSocket-Key header.</param>
        /// <returns>The response key.</returns>
        private string CreateResponseKey(string secWebSocketKey) {
            var builder = new StringBuilder(secWebSocketKey, 64);
            builder.Append(guid);
            var sha1 = new SHA1CryptoServiceProvider();
            var data = Encoding.UTF8.GetBytes(builder.ToString());
            var hash = sha1.ComputeHash(data);
            return System.Convert.ToBase64String(hash);
        }

        #endregion
        
        #region Statics
        /// <summary>
        /// Creates a new web socket router. By default, this router will listen for 
        /// incoming sockets on the root path.
        /// </summary>
        /// <param name="onsocket">The web socket handler.</param>
        /// <returns></returns>
        public static SocketRouter Create(Reactor.Action<Reactor.Web.Socket> onsocket) {
            return new SocketRouter("/", onsocket);
        }

        /// <summary>
        /// Creates a new web socket router on the given path.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="onsocket"></param>
        /// <returns></returns>
        public static SocketRouter Create(string path, Reactor.Action<Reactor.Web.Socket> onsocket) {
            return new SocketRouter(path, onsocket);
        }

        #endregion
        
        #region Implicit Cast

        /// <summary>
        /// Implicit cast to middleware.
        /// </summary>
        /// <param name="staticFiles"></param>
        public static implicit operator Reactor.Action<Reactor.Http.Context,
                                        Reactor.Action<Reactor.Http.Context>>(Reactor.Web.SocketRouter socketHandler) {
            return socketHandler.Process;
        }

        #endregion
    }
}
