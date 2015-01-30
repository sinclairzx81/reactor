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

using System;
using System.Net;
using System.Net.Sockets;

namespace Reactor.Tcp
{
    public class Server
    {
        private TcpListener             listener;

        private Action<Socket>          OnSocket;

        public  event Action<Exception> OnError;

        public Server(Action<Socket> OnSocket)
        {
            this.OnSocket = OnSocket;
        }

        public Server Listen(int port)
        {
            this.listener = new TcpListener(IPAddress.Any, port);

            this.listener.Start();

            this.AcceptSocket();

            return this;
        }

        private void AcceptSocket()
        {
            IO.AcceptSocket(this.listener, (exception, socket) =>
            {
                if(exception != null)
                {
                    if(this.OnError != null)
                    {
                        this.OnError(exception);
                    }

                    return;
                }

                if(this.OnSocket != null)
                {
                    this.OnSocket(new Socket(socket));
                }

                this.AcceptSocket();
            });
        }

        #region Statics

        /// <summary>
        /// Creates a new TcpServer. 
        /// </summary>
        /// <param name="OnSocket">A OnSocket callback to receive incoming socket connections.</param>
        /// <returns>This TcpServer.</returns>
        public static Server Create(Action<Socket> OnSocket)
        {
            return new Server(OnSocket);
        }

        #endregion
    }
}
