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
using System.Collections.Generic;
using System.Net;

namespace Reactor.Fusion
{
    public class Server
    {
        internal Reactor.Udp.Socket         UdpSocket  { get; set; }

        private  Dictionary<EndPoint, Socket>  Sockets    { get; set; }

        private  Action<Socket>                OnSocket   { get; set; }

        public event Action<Exception>         OnError;

        public Server(Action<Socket> OnSocket)
        {
            this.OnSocket = OnSocket;

            this.Sockets  = new Dictionary<EndPoint, Socket>();

            this.UdpSocket = Reactor.Udp.Socket.Create();
        }

        public Server Listen(int Port)
        {
            this.UdpSocket.Bind(IPAddress.Any, Port);

            this.UdpSocket.OnMessage += (remoteEP, data) => {

                if (!this.Sockets.ContainsKey(remoteEP))
                {
                    this.Sockets[remoteEP] = new Socket(this.UdpSocket, remoteEP);

                    this.Sockets[remoteEP].OnConnect += () => {

                        Loop.Post(() => {

                            this.OnSocket(this.Sockets[remoteEP]);
                        });
                    };

                    this.Sockets[remoteEP].OnError += (exception) => {
                        
                        try {

                            this.Sockets.Remove(remoteEP);

                        }
                        catch(Exception _exception) {

                            if(this.OnError != null) {

                                Loop.Post(() => {

                                    this.OnError(_exception);
                                });
                            }
                        }
                    };
                }

                this.Sockets[remoteEP].Receive(data);
            };

            this.UdpSocket.OnError += (exception) => {

                if(this.OnError != null) {

                   

                    Loop.Post(() => {

                        this.OnError(exception);
                    });
                }
            };

            return this;
        }

        #region Stun

        public Server Stun(string host, int port, Action<Reactor.Udp.Socket.StunResponse> Callback)
        {
            this.UdpSocket.Stun(host, port, Callback);

            return this;
        }

        #endregion

        #region Statics

        public static Server Create(Action<Socket> OnSocket)
        {
            return new Server(OnSocket);
        }

        #endregion
    }
}
