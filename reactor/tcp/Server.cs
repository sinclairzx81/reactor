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
using System.Net;
using System.Net.Sockets;

namespace Reactor.Tcp
{
    /// <summary>
    /// A Tcp socket server.
    /// </summary>
    public class Server
    {
        private TcpListener                Listener { get; set; }

        private Action<Socket>          OnSocket;

        public  event Action<Exception>    OnSocketError;

        public Server(Action<Socket> OnSocket)
        {
            this.OnSocket = OnSocket;
        }

        /// <summary>
        /// Starts this server listening on the supplied port.
        /// </summary>
        /// <param name="Port">The port to listen on.</param>
        /// <returns>This TcpServer.</returns>
        public Server Listen(int Port)
        {
            this.Listener = new TcpListener(IPAddress.Any, Port);

            this.Listener.Start();

            this.AcceptSocket();

            return this;
        }

        #region AcceptSocket

        private void AcceptSocket()
        {
            Loop.Post(() =>
            {
                this.Listener.BeginAcceptSocket((Result) =>
                {
                    try
                    {
                        var socket = new Socket(this.Listener.EndAcceptSocket(Result));

                        Loop.Post(() =>
                        {
                            this.OnSocket(socket);

                            this.AcceptSocket();
                        });
                    }
                    catch (Exception exception)
                    {
                        Loop.Post(() =>
                        {
                            if (this.OnSocketError != null) {

                                this.OnSocketError(exception);
                            }
                        });
                    }

                }, null);
            });
        }

        #endregion 

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
