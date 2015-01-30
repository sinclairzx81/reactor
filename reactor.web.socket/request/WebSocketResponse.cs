/*--------------------------------------------------------------------------

Reactor.Web.Sockets

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

using Reactor.Web.Socket.Protocol;
using System;
using System.Collections.Generic;

namespace Reactor.Web.Socket
{
    internal class WebSocketResponse
    {
        public Reactor.Tcp.Socket         Socket     { get; set; }

        public Dictionary<string, string> Headers    { get; set; }

        public int                        StatusCode { get; set; }

        public List<Frame>                Frames     { get; set; }

        internal WebSocketResponse(Reactor.Tcp.Socket socket, Reactor.Buffer buffer)
        {
            this.Socket  = socket;

            this.Frames  = new List<Frame>();

            this.Headers = new Dictionary<string, string>();

            this.ParseResponseHeader(buffer);

            this.ParseResponseBody(buffer);
        }

        private void ParseResponseHeader(Reactor.Buffer buffer)
        {
            var data = buffer.ToString("utf8");

            string [] split = data.Split(new string [] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            
            try
            {
                var http = split[0];

                var s1 = http.Split(new char[] { ' ' });

                this.StatusCode = int.Parse(s1[1]);

                for(var i = 1; i < split.Length; i++)
                {
                    var s2 = split[i].Split(new char[] { ':' });

                    this.Headers[s2[0]] = s2[1];
                }
            }
            catch
            {

            }
        }

        private void ParseResponseBody(Reactor.Buffer buffer)
        {
            try
            {
                //---------------------------------------------
                // checking the body of the response. The goal
                // here is to extract any frames passed on the 
                // request from the server, as is the case with
                // socket.io.
                //---------------------------------------------

                var data = buffer.ToString("utf8");

                if (data.Contains("\r\n"))
                {
                    var split = data.Split(new string[] { "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                    if (split.Length == 2)
                    {
                        var bytes = buffer.ToArray();

                        //---------------------------------------------
                        // scan for frames
                        //---------------------------------------------

                        var framedatalist = new List<List<byte>>();

                        List<byte> framedata = null;

                        foreach (var b in bytes)
                        {
                            if (b == 0x81)
                            {
                                framedata = new List<byte>();

                                framedatalist.Add(framedata);
                            }
                            if (framedata != null)
                            {
                                framedata.Add(b);
                            }
                        }

                        //---------------------------------------------
                        // add frame to frame list.
                        //---------------------------------------------

                        foreach (var item in framedatalist)
                        {
                            var frame = Reactor.Web.Socket.Protocol.Frame.Parse(item.ToArray(), true);

                            this.Frames.Add(frame);
                        }
                    }

                }
            }
            catch
            {
                
            }
        }
    }
}
