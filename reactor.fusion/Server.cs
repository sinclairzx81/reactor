/*--------------------------------------------------------------------------

Reactor.Fusion

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

using Reactor.Fusion.Protocol;
using System.Collections.Generic;

namespace Reactor.Fusion
{
    public class Server
    {
        private Reactor.Udp.Socket socket;

        private Reactor.Action<Reactor.Fusion.Socket> onsocket;

        private Dictionary<System.Net.EndPoint, Reactor.Fusion.Socket> sockets { get; set; }

        public Server()
        {
            this.socket   = Reactor.Udp.Socket.Create();

            this.onsocket = socket => { };

            this.sockets = new Dictionary<System.Net.EndPoint, Reactor.Fusion.Socket>();
        }

        private void OnMessage(Reactor.Udp.Message message)
        {
            PacketType packetType;
            
            var packet = Parser.Deserialize(message.Buffer.ToArray(), out packetType);

            if (packetType == PacketType.Syn) {

                if (!this.sockets.ContainsKey(message.EndPoint)) {

                    this.sockets[message.EndPoint] = new Socket(this.socket, message.EndPoint);

                    this.sockets[message.EndPoint].OnConnect += () => {

                        this.onsocket(this.sockets[message.EndPoint]);
                    };
                }
            }

            if (this.sockets.ContainsKey(message.EndPoint))
            {
                this.sockets[message.EndPoint].Receive(message.Buffer.ToArray());
            }
        }

        #region Setup

        public void Listen(int port)
        {
            this.socket.Bind(System.Net.IPAddress.Any, port);

            this.socket.OnRead(this.OnMessage);
        }

        public static Server Create(Reactor.Action<Reactor.Fusion.Socket> callback)
        {
            var server = new Server();

            server.onsocket = callback;

            return server;
        }

        #endregion
    }
}
