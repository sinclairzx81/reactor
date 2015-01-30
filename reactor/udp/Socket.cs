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

namespace Reactor.Udp
{
    /// <summary>
    /// A User Datagram Protocol Socket.
    /// </summary>
    public partial class Socket
    {
        private System.Net.Sockets.Socket             socket;

        public  event Action<EndPoint, byte []>       OnMessage;

        public  event Action<Exception>               OnError;

        public Socket()
        {
            this.socket = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
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
            this.socket.Bind(new IPEndPoint(address, port));

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
                return this.socket.AddressFamily;
            }
        }

        public int Available
        {
            get
            {
                return this.socket.Available;
            }
        }

        public bool Blocking
        {
            get
            {
                return this.socket.Blocking;
            }
            set
            {
                this.socket.Blocking = value;
            }
        }

        public bool Connected
        {
            get
            {
                return this.socket.Connected;
            }
        }

        public bool DontFragment
        {
            get
            {
                return this.socket.DontFragment;
            }
            set
            {
                this.socket.DontFragment = value;
            }
        }

        public bool EnableBroadcast
        {
            get
            {
                return this.socket.EnableBroadcast;
            }
            set
            {
                this.socket.EnableBroadcast = value;
            }
        }

        public bool ExclusiveAddressUse
        {
            get
            {
                return this.socket.ExclusiveAddressUse;
            }
            set
            {
                this.socket.ExclusiveAddressUse = value;
            }
        }

        public IntPtr Handle
        {
            get
            {
                return this.socket.Handle;
            }
        }

        public bool IsBound
        {
            get
            {
                return this.socket.IsBound;
            }
        }

        public LingerOption LingerState
        {
            get
            {
                return this.socket.LingerState;
            }
            set
            {
                this.socket.LingerState = value;
            }
        }

        public EndPoint LocalEndPoint
        {
            get
            {
                return this.socket.LocalEndPoint;
            }
        }

        public bool MulticastLoopback
        {
            get
            {
                return this.socket.MulticastLoopback;
            }
            set
            {
                this.socket.MulticastLoopback = value;
            }
        }

        public bool NoDelay
        {
            get
            {
                return this.socket.NoDelay;
            }
            set
            {
                this.socket.NoDelay = value;
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
                return this.socket.ProtocolType;
            }
        }

        public int ReceiveBufferSize
        {
            get
            {
                return this.socket.ReceiveBufferSize;
            }
            set
            {
                this.socket.ReceiveBufferSize = value;
            }
        }

        public int ReceiveTimeout
        {
            get
            {
                return this.socket.ReceiveTimeout;
            }
            set
            {
                this.socket.ReceiveTimeout = value;
            }
        }

        public EndPoint RemoteEndPoint
        {
            get
            {
                return this.socket.RemoteEndPoint;
            }
        }

        public int SendBufferSize
        {
            get
            {
                return this.socket.SendBufferSize;
            }
            set
            {
                this.socket.SendBufferSize = value;
            }
        }

        public int SendTimeout
        {
            get
            {
                return this.socket.SendTimeout;
            }
            set
            {
                this.socket.SendTimeout = value;
            }
        }

        public SocketType SocketType
        {
            get
            {
                return this.socket.SocketType;
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
                return this.socket.Ttl;
            }
            set
            {
                this.socket.Ttl = value;
            }
        }

        public bool UseOnlyOverlappedIO
        {
            get
            {
                return this.socket.Blocking;
            }
            set
            {
                this.socket.Blocking = value;
            }
        }
        
        public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, bool optionValue)
        {
            this.socket.SetSocketOption(optionLevel, optionName, optionValue);
        }
        
        public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue)
        {
            this.socket.SetSocketOption(optionLevel, optionName, optionValue);
        }
        
        public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, int optionValue)
        {
            this.socket.SetSocketOption(optionLevel, optionName, optionValue);
        }

        public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, object optionValue)
        {
            this.socket.SetSocketOption(optionLevel, optionName, optionValue);
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
            this.socket.SendTo(data, index, count, SocketFlags.None, endpoint);
        }

        #endregion

        #region ReceiveFrom

        private byte[] receive_buffer = new byte[4096];

        private void ReceiveFrom()
        {
            IO.ReceiveFrom(this.socket, receive_buffer, (exception, remoteEP, read) =>
            {
                if(exception != null)
                {
                    if(this.OnError != null)
                    {
                        this.OnError(exception);
                    }

                    return;
                }

                if (this.OnMessage != null)
                {
                    byte[] buffer = new byte[read];

                    System.Buffer.BlockCopy(receive_buffer, 0, buffer, 0, read);
                    
                    this.OnMessage(remoteEP, buffer);
                }

                this.ReceiveFrom();

            });
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
