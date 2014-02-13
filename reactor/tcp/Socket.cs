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
using System.Threading;

namespace Reactor.Tcp
{
    /// <summary>
    /// A Reactor TcpSocket.
    /// </summary>
    public class Socket: IReadable, IWriteable
    {
        private System.Net.Sockets.Socket TcpSocket { get; set; }
        
        private ReadStream                 ReadStream             { get; set; }

        private WriteStream                WriteStream            { get; set; }

        private NetworkStream              NetworkStream          { get; set; }

        public  IPAddress                  IPAddress              { get; set; }

        public  int                        Port                   { get; set; }

        public event Action                OnConnect;

        public event Action<Exception>     OnSocketError;

        #region Constructors

        internal Socket(System.Net.Sockets.Socket Socket) 
        {
            this.TcpSocket               = Socket;

            this.NetworkStream           = new NetworkStream(this.TcpSocket);

            this.ReadStream              = new ReadStream(this.NetworkStream);

            this.ReadStream.OnData      += this.ReadStreamOnData;

            this.ReadStream.OnError     += this.ReadStreamOnError;

            this.ReadStream.OnEnd       += this.ReadStreamOnEnd;

            this.ReadStream.OnClose     += this.ReadStreamOnClose;

            this.WriteStream             = new WriteStream(this.NetworkStream);

            this.WriteStream.OnError    += this.WriteStreamOnError;
        }
        
        public Socket(string Hostname, int Port) {

            Reactor.Net.Dns.GetHostAddresses(Hostname, (exception, addresses) => {

                if(exception != null) {

                    if(this.OnError != null) {

                        this.OnError(exception);

                    }

                    return;
                }

                if(addresses.Length == 0) {

                    if (this.OnError != null) {

                        Loop.Post(() => {

                            this.OnError(new Exception("Unable to resolve hostname."));
                        });
                    }

                    return;

                }

                this.IPAddress = addresses[0];

                this.Port      = Port;

                this.TcpSocket = new System.Net.Sockets.Socket(IPAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                this.Connect();
            });
        }

        public Socket(IPAddress IPAddress, int Port)
        {
            this.TcpSocket    = new System.Net.Sockets.Socket(IPAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            this.IPAddress    = IPAddress;

            this.Port         = Port;
                
            this.Connect();
        }

        #endregion

        #region Socket

        public AddressFamily AddressFamily
        {
            get
            {
                return this.TcpSocket.AddressFamily;
            }
        }

        public int Available
        {
            get
            {
                return this.TcpSocket.Available;
            }
        }
        
        public bool Blocking 
        {
            get
            {
                return this.TcpSocket.Blocking;
            }
            set
            {
                this.TcpSocket.Blocking = value;
            }
        }

        public bool Connected
        {
            get
            {
                return this.TcpSocket.Connected;
            }
        }

        public bool DontFragment
        {
            get
            {
                return this.TcpSocket.DontFragment;
            }
            set
            {
                this.TcpSocket.DontFragment = value;
            }
        }

        public bool EnableBroadcast
        {
            get
            {
                return this.TcpSocket.EnableBroadcast;
            }
            set
            {
                this.TcpSocket.EnableBroadcast = value;
            }
        }

        public bool ExclusiveAddressUse
        {
            get
            {
                return this.TcpSocket.ExclusiveAddressUse;
            }
            set
            {
                this.TcpSocket.ExclusiveAddressUse = value;
            }
        }

        public IntPtr Handle
        {
            get
            {
                return this.TcpSocket.Handle;
            }
        }

        public bool IsBound
        {
            get
            {
                return this.TcpSocket.IsBound;
            }
        }

        public LingerOption LingerState
        {
            get
            {
                return this.TcpSocket.LingerState;
            }
            set
            {
                this.TcpSocket.LingerState = value;
            }
        }

        public EndPoint LocalEndPoint
        {
            get
            {
                return this.TcpSocket.LocalEndPoint;
            }
        }

        public bool MulticastLoopback
        {
            get
            {
                return this.TcpSocket.MulticastLoopback;
            }
            set
            {
                this.TcpSocket.MulticastLoopback = value;
            }
        }

        public bool NoDelay
        {
            get
            {
                return this.TcpSocket.NoDelay;
            }
            set
            {
                this.TcpSocket.NoDelay = value;
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
                return this.TcpSocket.ProtocolType;
            }
        }

        public int ReceiveBufferSize
        {
            get
            {
                return this.TcpSocket.ReceiveBufferSize;
            }
            set
            {
                this.TcpSocket.ReceiveBufferSize = value;
            }
        }

        public int ReceiveTimeout
        {
            get
            {
                return this.TcpSocket.ReceiveTimeout;
            }
            set
            {
                this.TcpSocket.ReceiveTimeout = value;
            }
        }

        public EndPoint RemoteEndPoint
        {
            get
            {
                return this.TcpSocket.RemoteEndPoint;
            }
        }

        public int SendBufferSize
        {
            get
            {
                return this.TcpSocket.SendBufferSize;
            }
            set
            {
                this.TcpSocket.SendBufferSize = value;
            }
        }

        public int SendTimeout
        {
            get
            {
                return this.TcpSocket.SendTimeout;
            }
            set
            {
                this.TcpSocket.SendTimeout = value;
            }
        }

        public SocketType SocketType
        {
            get
            {
                return this.TcpSocket.SocketType;
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
                return this.TcpSocket.Ttl;
            }
            set
            {
                this.TcpSocket.Ttl = value;
            }
        }

        public bool UseOnlyOverlappedIO
        {
            get
            {
                return this.TcpSocket.Blocking;
            }
            set
            {
                this.TcpSocket.Blocking = value;
            }
        }

        #endregion

        #region IReadable

        public event Action<Exception> OnError;

        public event Action<Buffer>    OnData;

        public event Action            OnEnd;

        public event Action            OnClose;

        public IReadable Pipe(IWriteable writestream)
        {
            return this.ReadStream.Pipe(writestream);
        }

        public void Pause()
        {
            this.ReadStream.Pause();
        }

        public void Resume()
        {
            this.ReadStream.Resume();
        }

        #endregion

        #region IWriteable

        public void Write(byte[] data)
        {
            this.WriteStream.Write(data);
        }

        public void Write(Buffer buffer)
        {
            this.WriteStream.Write(buffer);
        }

        public void Write(string data)
        {
            this.WriteStream.Write(data);
        }
        
        public void Write(string format, object arg0)
        {
            this.WriteStream.Write(format, arg0);
        }

        public void Write(string format, params object[] args)
        {
            this.WriteStream.Write(format, args);
        }

        public void Write(string format, object arg0, object arg1)
        {
            this.WriteStream.Write(format, arg0, arg1);
        }

        public void Write(string format, object arg0, object arg1, object arg2)
        {
            this.WriteStream.Write(format, arg0, arg1, arg2);
        }

        public void Write(byte data)
        {
            this.WriteStream.Write(data);
        }

        public void Write(byte[] buffer, int index, int count)
        {
            this.WriteStream.Write(buffer, index, count);
        }

        public void Write(bool value)
        {
            this.WriteStream.Write(value);
        }

        public void Write(short value)
        {
            this.WriteStream.Write(value);
        }

        public void Write(ushort value)
        {
            this.WriteStream.Write(value);
        }

        public void Write(int value)
        {
            this.WriteStream.Write(value);
        }

        public void Write(uint value)
        {
            this.WriteStream.Write(value);
        }

        public void Write(long value)
        {
            this.WriteStream.Write(value);
        }

        public void Write(ulong value)
        {
            this.WriteStream.Write(value);
        }

        public void Write(float value)
        {
            this.WriteStream.Write(value);
        }

        public void Write(double value)
        {
            this.WriteStream.Write(value);
        }

        

        public void End()
        {
            this.WriteStream.End(() => 
            {
                try  
                {
                    this.TcpSocket.Disconnect(false);
                }
                catch(Exception exception) 
                {
                    Loop.Post(() => 
                    {
                        if(this.OnSocketError != null) 
                        {
                            OnSocketError(exception);
                        }
                    });
                }

                this.NetworkStream.Dispose();
            });
        }

        #endregion

        #region BeginConnect

        private void Connect()
        {
            Loop.Post(() =>
            {
                this.TcpSocket.BeginConnect(new IPEndPoint(this.IPAddress, this.Port), (Result) =>
                {
                    try
                    {
                        this.TcpSocket.EndConnect(Result);

                        this.NetworkStream = new NetworkStream(this.TcpSocket, true);

                        this.ReadStream = new ReadStream(this.NetworkStream);

                        this.WriteStream = new WriteStream(this.NetworkStream);

                        this.ReadStream.OnData += this.ReadStreamOnData;

                        this.ReadStream.OnError += this.ReadStreamOnError;

                        this.ReadStream.OnEnd += this.ReadStreamOnEnd;

                        this.ReadStream.OnClose += this.ReadStreamOnClose;

                        this.WriteStream.OnError += this.WriteStreamOnError;

                        Loop.Post(() =>
                        {
                            if (this.OnConnect != null)
                            {
                                this.OnConnect();
                            }
                        });
                    }
                    catch (Exception exception)
                    {
                        Loop.Post(() =>
                        {
                            if (this.OnSocketError != null)
                            {
                                this.OnSocketError(exception);
                            }
                        });
                    }

                }, null);
            });
        }

        #endregion

        #region Handlers

        private void WriteStreamOnError(Exception exception) 
        {
            if (this.OnError != null)
            {
                this.OnError(exception);
            }               
        }

        private void ReadStreamOnData(Buffer buffer) 
        {
            if(this.OnData != null)
            {
                this.OnData(buffer);
            }
        }

        private void ReadStreamOnEnd()
        {
            if (this.OnEnd != null)
            {
                this.OnEnd();
            }            
        }

        private void ReadStreamOnError(Exception exception) 
        {
            if (this.OnError != null)
            {
                this.OnError(exception);
            }            
        }

        private void ReadStreamOnClose()
        {
            if (this.OnClose != null)
            {
                this.OnClose();
            }
        }

        #endregion

        #region Statics

        /// <summary>
        /// Creates a new TcpSocket and connects to localhost on the given Port.
        /// </summary>
        /// <param name="Port">The Port</param>
        /// <returns>A new TcpSocket</returns>
        public static Socket Create(int Port)
        {
            return new Socket(IPAddress.Loopback, Port);
        }

        /// <summary>
        /// Creates a new TcpSocket to the given Hostname and Port.
        /// </summary>
        /// <param name="Hostname">The Hostname.</param>
        /// <param name="Port">The port.</param>
        /// <returns>A new TcpSocket.</returns>
        public static Socket Create(IPAddress address, int Port)
        {
            return new Socket(address, Port);
        }

        /// <summary>
        /// Creates a new TcpSocket to the given Hostname and Port.
        /// </summary>
        /// <param name="Hostname">The Hostname.</param>
        /// <param name="Port">The port.</param>
        /// <returns>A new TcpSocket.</returns>
        public static Socket Create(string Hostname, int Port)
        {
            return new Socket(Hostname, Port);
        }

        #endregion
    }
}