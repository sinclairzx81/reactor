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
using System.Net.Sockets;

namespace Reactor.Udp
{
    /// <summary>
    /// A User Datagram Protocol Socket.
    /// </summary>
    public partial class Socket
    {
        private System.Net.Sockets.Socket             UdpSocket    { get; set; }

        public  event Action<EndPoint, byte []>       OnMessage;

        public  event Action<Exception>               OnError;

        public Socket()
        {
            this.UdpSocket                   = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            this.UdpSocket.SendBufferSize    = ushort.MaxValue;

            this.UdpSocket.ReceiveBufferSize = ushort.MaxValue;
        }

        #region Methods

        /// <summary>
        /// Binds the underlying Socket to the supplied IPAddress and Port. If
        /// setting up a socket as a listener, this is the method to use. Use
        /// IPAddress.Any to bind to all local addresses.
        /// </summary>
        /// <param name="address">The address to bind to.</param>
        /// <param name="port">The port to bind to.</param>
        public void Bind(IPAddress address, int port)
        {
            this.UdpSocket.Bind(new IPEndPoint(address, port));

            this.ReceiveFrom();
        }

        /// <summary>
        /// Binds the underlying Socket to the supplied IPAddress. The port is
        /// assigned by the socket. Use this method if you want to bind the 
        /// socket for a client who needs to receive messages back from a
        /// endpoint.
        /// </summary>
        /// <param name="address">The address to bind to.</param>
        public void Bind(IPAddress address)
        {
            this.Bind(address, 0);
        }

        #endregion

        #region Socket

        public AddressFamily AddressFamily
        {
            get
            {
                return this.UdpSocket.AddressFamily;
            }
        }

        public int Available
        {
            get
            {
                return this.UdpSocket.Available;
            }
        }

        public bool Blocking
        {
            get
            {
                return this.UdpSocket.Blocking;
            }
            set
            {
                this.UdpSocket.Blocking = value;
            }
        }

        public bool Connected
        {
            get
            {
                return this.UdpSocket.Connected;
            }
        }

        public bool DontFragment
        {
            get
            {
                return this.UdpSocket.DontFragment;
            }
            set
            {
                this.UdpSocket.DontFragment = value;
            }
        }

        public bool EnableBroadcast
        {
            get
            {
                return this.UdpSocket.EnableBroadcast;
            }
            set
            {
                this.UdpSocket.EnableBroadcast = value;
            }
        }

        public bool ExclusiveAddressUse
        {
            get
            {
                return this.UdpSocket.ExclusiveAddressUse;
            }
            set
            {
                this.UdpSocket.ExclusiveAddressUse = value;
            }
        }

        public IntPtr Handle
        {
            get
            {
                return this.UdpSocket.Handle;
            }
        }

        public bool IsBound
        {
            get
            {
                return this.UdpSocket.IsBound;
            }
        }

        public LingerOption LingerState
        {
            get
            {
                return this.UdpSocket.LingerState;
            }
            set
            {
                this.UdpSocket.LingerState = value;
            }
        }

        public EndPoint LocalEndPoint
        {
            get
            {
                return this.UdpSocket.LocalEndPoint;
            }
        }

        public bool MulticastLoopback
        {
            get
            {
                return this.UdpSocket.MulticastLoopback;
            }
            set
            {
                this.UdpSocket.MulticastLoopback = value;
            }
        }

        public bool NoDelay
        {
            get
            {
                return this.UdpSocket.NoDelay;
            }
            set
            {
                this.UdpSocket.NoDelay = value;
            }
        }

        public static bool OSSupportsIPv6
        {
            get
            {
                return Socket.OSSupportsIPv6;
            }
        }

        public ProtocolType ProtocolType
        {
            get
            {
                return this.UdpSocket.ProtocolType;
            }
        }

        public int ReceiveBufferSize
        {
            get
            {
                return this.UdpSocket.ReceiveBufferSize;
            }
            set
            {
                this.UdpSocket.ReceiveBufferSize = value;
            }
        }

        public int ReceiveTimeout
        {
            get
            {
                return this.UdpSocket.ReceiveTimeout;
            }
            set
            {
                this.UdpSocket.ReceiveTimeout = value;
            }
        }

        public EndPoint RemoteEndPoint
        {
            get
            {
                return this.UdpSocket.RemoteEndPoint;
            }
        }

        public int SendBufferSize
        {
            get
            {
                return this.UdpSocket.SendBufferSize;
            }
            set
            {
                this.UdpSocket.SendBufferSize = value;
            }
        }

        public int SendTimeout
        {
            get
            {
                return this.UdpSocket.SendTimeout;
            }
            set
            {
                this.UdpSocket.SendTimeout = value;
            }
        }

        public SocketType SocketType
        {
            get
            {
                return this.UdpSocket.SocketType;
            }
        }

        public static bool SupportsIPv4
        {
            get
            {
                return Socket.SupportsIPv4;
            }
        }

        public short Ttl
        {
            get
            {
                return this.UdpSocket.Ttl;
            }
            set
            {
                this.UdpSocket.Ttl = value;
            }
        }

        public bool UseOnlyOverlappedIO
        {
            get
            {
                return this.UdpSocket.Blocking;
            }
            set
            {
                this.UdpSocket.Blocking = value;
            }
        }

        #endregion

        #region Send

        public void Send(IPAddress address, int port, byte [] data)
        {
            this.Send(new IPEndPoint(address, port), data, 0, data.Length);
        }

        public void Send(IPAddress address, int port, byte[] data, int index, int count)
        {
            this.Send(new IPEndPoint(address, port), data, index, count);
        }

        public void Send(EndPoint endpoint, byte [] data)
        {
            this.Send(endpoint, data, 0, data.Length);
        }

        public void Send(EndPoint endpoint, byte [] data, int index, int count)
        {
            this.UdpSocket.SendTo(data, index, count, SocketFlags.None, endpoint);
        }

        #endregion

        #region ReceiveFrom

        private void ReceiveFrom()
        {
            EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

            try
            {
                var recv_buffer = new byte[this.UdpSocket.ReceiveBufferSize];

                this.UdpSocket.BeginReceiveFrom(recv_buffer, 0, recv_buffer.Length, 0, ref remoteEP, (Result) =>
                {
                    try {

                        int read = this.UdpSocket.EndReceiveFrom(Result, ref remoteEP);
                        
                        if (this.OnMessage != null) {

                            byte[] buffer = new byte[read];

                            System.Buffer.BlockCopy(recv_buffer, 0, buffer, 0, read);
                                
                            Loop.Post(() => {

                                this.OnMessage(remoteEP, buffer); 
                            });

                            this.ReceiveFrom();
                        }
                    }
                    catch (Exception exception) {

                        Loop.Post(() => {

                            this.ReceiveFrom();

                            if (this.OnError != null)
                            {
                                this.OnError(exception);
                            }
                        });
                    }
                    
                }, null);
            }
            catch (Exception exception) {

                Loop.Post(() => {

                    this.ReceiveFrom();

                    if (this.OnError != null)
                    {
                        this.OnError(exception);
                    }
                });
            }
        }

        #endregion

        #region Statics

        public static Socket Create() 
        {
            return new Socket();
        }

        #endregion
    }
}
