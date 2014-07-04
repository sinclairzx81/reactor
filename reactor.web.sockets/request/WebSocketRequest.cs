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

namespace Reactor.Web.Sockets
{
    internal class WebSocketRequest
    {
        private Reactor.Tcp.Socket socket;

        public Uri                        Uri     { get; set; }

        public Dictionary<string, string> Headers { get; set; }

        public string                     Method  { get; set; }

        public WebSocketRequest(string Url)
        {
            this.Uri     = new Uri(Url);

            this.Headers = new Dictionary<string, string>();

            this.Method = "GET";
        }

        public void GetResponse(Action<Exception, WebSocketResponse> callback)
        {
            //------------------------------------------
            // create method
            //------------------------------------------

            string http = string.Format("{0} {1} HTTP/1.1", this.Method.ToUpper(), this.Uri.PathAndQuery);

            //------------------------------------------
            // create header
            //------------------------------------------

            this.Headers["Cache-Control"]            = "no-cache";

            this.Headers["Connection"]               = "upgrade";

            this.Headers["Host"]                     = this.Uri.Authority;

            this.Headers["Origin"]                   = "";

            this.Headers["Pragma"]                   = "no-cache";

            this.Headers["Sec-WebSocket-Extensions"] = "permessage-deflate";

            this.Headers["Sec-WebSocket-Key"]        = this.CreateSecWebSocketKey();

            this.Headers["Sec-WebSocket-Version"]    = "13";

            this.Headers["Upgrade"]                  = "websocket";

            this.Headers["User-Agent"]               = "Reactor.Web.Sockets.WebSocket";

            //------------------------------------------
            // build request
            //------------------------------------------

            var buffer = Reactor.Buffer.Create();

            buffer.Write(http + "\r\n");

            foreach (var pair in this.Headers)
            {
                buffer.Write("{0}: {1}\r\n", pair.Key, pair.Value);
            }

            buffer.Write("\r\n");

            //------------------------------------------
            // send request
            //------------------------------------------

            this.socket = Reactor.Tcp.Socket.Create(this.Uri.DnsSafeHost, this.Uri.Port);

            this.socket.OnConnect += () =>
            {
                this.socket.Write(buffer, (exception) =>
                {
                    if (exception != null)
                    {
                        callback(exception, null);

                        return;
                    }

                    Reactor.Action<Buffer> ondata = null;
                    
                    ondata = (data) => {

                        this.socket.OnData -= ondata;

                        var response = new Reactor.Web.Sockets.WebSocketResponse(this.socket, data);

                        callback(null, response);
                    };

                    this.socket.OnData += ondata;

                });
            };

            this.socket.OnError += (exception) =>
            {
                callback(exception, null);
            };
        }

        #region Privates

        private string CreateSecWebSocketKey()
        {
            var random = new Random();

            var data = new byte[16];

            random.NextBytes(data);

            return Convert.ToBase64String(data);
        }

        #endregion

        #region Statics

        public static WebSocketRequest Create(string url)
        {
            return new WebSocketRequest(url);
        }

        #endregion
    }
}
