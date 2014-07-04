/*--------------------------------------------------------------------------

Reactor.Web.Sockets

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
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Reactor.Web.Sockets
{
    internal class ServerWebSocketUpgradeResponse
    {
        #region Statics

        private static string guid     = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

        private static string ctlf     = "\r\n";

        #endregion

        private ServerWebSocketUpgradeRequest     request;

        private Dictionary<string, string> Headers    { get; set; }

        private string                     Reason     { get; set; }

        private string                     StatusCode { get; set; }

        private ServerWebSocketUpgradeResponse(ServerWebSocketUpgradeRequest request)
        {
            this.request = request;

            this.Headers = new Dictionary<string, string>();
        }

        #region Methods

        private string CreateResponseKey(string secWebSocketKey)
        {
            var builder = new StringBuilder(secWebSocketKey, 64);

            builder.Append(guid);

            var sha1 = new SHA1CryptoServiceProvider();

            var data = Encoding.UTF8.GetBytes(builder.ToString());

            var hash = sha1.ComputeHash(data);

            return System.Convert.ToBase64String(hash);
        }

        public void Accept(Action<Exception> callback)
        {
            //----------------------------
            // create response
            //----------------------------

            this.Headers["Upgrade"]              = "websocket";

            this.Headers["Connection"]           = "Upgrade";

            this.Headers["Sec-WebSocket-Accept"] = this.CreateResponseKey(this.request.SecWebSocketKey);

            //----------------------------
            // response buffer
            //----------------------------

            var protocol = this.request.Context.Request.ProtocolVersion;

            var buffer = new StringBuilder(64);

            buffer.Append(string.Format("HTTP/{0} {1} {2}{3}", protocol, "101", this.Reason, ctlf));

            foreach (var pair in this.Headers)
            {
                buffer.Append(string.Format("{0}: {1}{2}", pair.Key, pair.Value, ctlf));
            }

            buffer.Append(ctlf);

            //----------------------------
            // send response
            //----------------------------

            var data = System.Text.Encoding.UTF8.GetBytes(buffer.ToString());

            this.request.Context.Connection.Write(Reactor.Buffer.Create(data), callback);
        }

        public void Reject(string reason, Action<Exception> callback)
        {
            this.request.Context.Response.StatusCode = 401;

            this.request.Context.Response.ContentType = "text/plain";

            this.request.Context.Response.Write(reason);

            this.request.Context.Response.End((exception) =>
            {
                callback(exception);
            });
        }

        #endregion

        #region Statics

        public static ServerWebSocketUpgradeResponse Create(ServerWebSocketUpgradeRequest request)
        {
            return new ServerWebSocketUpgradeResponse(request);
        }

        #endregion
    }
}
