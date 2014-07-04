/*--------------------------------------------------------------------------

Reactor

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

namespace Reactor.Tcp
{
    /// <summary>
    /// A Reactor TcpSocket.
    /// </summary>
    public class Socket: IDuplexable
    {
        #region Command

        internal class Command
        {
            public Action<Exception> Callback { get; set; }
        }

        internal class WriteCommand : Command
        {
            public Buffer Buffer { get; set; }

            public WriteCommand(Buffer buffer, Action<Exception> callback)
            {
                this.Buffer = buffer;

                this.Callback = callback;
            }
        }

        internal class EndCommand : Command
        {
            public EndCommand(Action<Exception> callback)
            {
                this.Callback = callback;
            }
        }

        #endregion

        private System.Net.Sockets.Socket  socket;

        private NetworkStream              stream;

        public event Action                OnConnect;

        //---------------------------------
        // readable
        //---------------------------------

        private bool                       reading;

        private bool                       paused;

        private long                       received;

        //---------------------------------
        // writeable
        //---------------------------------

        private Queue<Command>             commands;

        private bool                       writing;

        private bool                       ended;

        //---------------------------------
        // shared
        //---------------------------------

        private bool                       closed;

        #region Constructors

        internal Socket(System.Net.Sockets.Socket socket) 
        {
            this.socket  = socket;

            this.stream  = new NetworkStream(this.socket);

            //---------------------
            // readable
            //---------------------
            this.received = 0;
            
            this.closed   = false;

            this.paused   = true;

            //---------------------
            // writeable
            //---------------------
            this.commands = new Queue<Command>();

            this.closed   = false;

            this.writing  = false;

            this.ended    = false;
        }
        
        public Socket(string hostname, int port) 
        {
            //---------------------
            // readable
            //---------------------
            this.received = 0;
            
            this.closed   = false;

            this.paused   = true;

            //---------------------
            // writeable
            //---------------------

            this.commands = new Queue<Command>();

            this.closed   = false;

            this.writing  = false;

            this.ended    = false;

            IO.Resolve(this.socket, hostname, (exception0, addresses) =>
            {
                if (exception0 != null)
                {
                    if (this.OnError != null)
                    {
                        this.OnError(exception0);
                    }

                    return;
                }

                if(addresses.Length == 0)
                {
                    if (this.OnError != null)
                    {
                        this.OnError(new Exception(string.Format("Unable to resolve hostname {0}", hostname)));
                    }

                    return;                    
                }

                this.socket = new System.Net.Sockets.Socket(addresses[0].AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                IO.Connect(this.socket, addresses[0], port, (exception1) =>
                {
                    if (exception1 != null)
                    {
                        if (this.OnError != null)
                        {
                            this.OnError(exception1);
                        }

                        return;
                    }

                    this.stream = new NetworkStream(this.socket);

                    if(this.OnConnect != null)
                    {
                        this.OnConnect();
                    }

                    if (this.ondata != null)
                    {
                        this.Resume();
                    }
                });
            });

        }

        public Socket(IPAddress address, int port)
        {
            this.socket = new System.Net.Sockets.Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            //---------------------
            // readable
            //---------------------
            this.received = 0;
            
            this.closed   = false;

            this.paused   = true;

            //---------------------
            // writeable
            //---------------------

            this.commands = new Queue<Command>();

            this.closed   = false;

            this.writing  = false;

            this.ended    = false;

            IO.Connect(this.socket, address, port, (exception) =>
            {
                if(exception != null)
                {
                    if(this.OnError != null)
                    {
                        this.OnError(exception);
                    }

                    return;
                }

                this.stream = new NetworkStream(this.socket);

                if (this.OnConnect != null)
                {
                    this.OnConnect();
                }

                if (this.ondata != null)
                {
                    this.Resume();
                }
            });
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

        #region IReadStream

        public event  Action<Exception> OnError;

        private event Action<Buffer> ondata;

        public event  Action<Buffer> OnData
        {
            add
            {
                this.ondata += value;

                this.Resume();
            }
            remove
            {
                this.ondata -= value;
            }
        }

        public event  Action        OnEnd;

        public IReadable Pipe(IWriteable writeable)
        {
            this.OnData += data =>
            {
                this.Pause();

                writeable.Write(data, (exception0) =>
                {
                    if (exception0 != null)
                    {
                        if (this.OnError != null)
                        {
                            this.OnError(exception0);
                        }

                        return;
                    }

                    writeable.Flush((exception1) =>
                    {
                        if (exception1 != null)
                        {
                            if (this.OnError != null)
                            {
                                this.OnError(exception1);
                            }

                            return;
                        }

                        this.Resume();
                    });
                });
            };

            this.OnEnd += () =>
            {
                writeable.End();
            };

            if (writeable is IReadable)
            {
                return writeable as IReadable;
            }

            return null;
        }

        public void Pause()
        {
            this.paused = true;
        }

        public void Resume()
        {
            this.paused = false;

            if (this.Connected) // special case
            {
                if (!this.reading)
                {
                    if (!this.closed)
                    {
                        this.Read();
                    }
                }
            }
        }

        public void Close()
        {
            this.closed = true;
        }

        #endregion

        #region IWriteable

        public void Write (Buffer buffer, Action<Exception> callback)
        {
            this.commands.Enqueue(new WriteCommand(buffer, callback));

            if (!this.writing)
            {
                this.writing = true;

                if (!this.ended)
                {
                    this.Write();
                }
            }
        }

        public void Write (Buffer buffer)
        {
            this.Write(buffer, exception => { });
        }

        public void Flush(Action<Exception> callback)
        {
            callback(null);
        }

        public void Flush()
        {
            this.Flush(exception => { });
        }

        public void End   (Action<Exception> callback)
        {
            this.commands.Enqueue(new EndCommand(callback));

            if (!this.writing)
            {
                this.writing = true;

                if (!this.ended)
                {
                    this.Write();
                }
            }
        }

        public void End   ()
        {
            this.End(exception => { });
        }

        #endregion

        private byte[] readbuffer = new byte[Reactor.Settings.DefaultReadBufferSize];

        private void Read  ()
          {
            this.reading = true;

            IO.Read(this.stream, this.readbuffer, (exception, read) =>
            {
                //----------------------------------------------
                // exception
                //----------------------------------------------

                if (exception != null)
                {
                    if (this.OnError != null)
                    {
                        this.OnError(exception);
                    }

                    if (this.OnEnd != null)
                    {
                        this.OnEnd();
                    }

                    try
                    {
                        this.stream.Dispose();
                    }
                    catch (Exception _exception)
                    {
                        if (this.OnError != null)
                        {
                            this.OnError(_exception);
                        }
                    }

                    this.reading = false;

                    this.closed = true;

                    return;
                }

                //----------------------------------------------
                // end of stream
                //----------------------------------------------
                if (read == 0)
                {
                    if (this.OnEnd != null)
                    {
                        this.OnEnd();
                    }

                    try
                    {
                        this.stream.Dispose();
                    }
                    catch (Exception _exception)
                    {
                        if (this.OnError != null)
                        {
                            this.OnError(_exception);
                        }
                    }

                    this.reading = false;

                    this.closed = true;

                    return;
                }

                //----------------------------------------------
                // increment received.
                //----------------------------------------------

                this.received = this.received + read;

                //----------------------------------------------
                // standard
                //----------------------------------------------
                if (this.ondata != null)
                {
                    this.ondata(new Buffer(this.readbuffer, 0, read));
                }

                //----------------------------------------------
                // continue
                //----------------------------------------------

                if (!this.paused)
                {
                    this.Read();
                }
                else
                {
                    this.reading = false;
                }
            });            
        }

        private void Write ()
        {
            var command  = this.commands.Dequeue();

            //----------------------------------
            // command: write
            //----------------------------------

            if (command is WriteCommand)
            {
                var write = command as WriteCommand;
                
                IO.Write(this.stream, write.Buffer.ToArray(), exception =>
                {
                    write.Callback(exception);

                    if (exception != null)
                    {
                        if (this.OnError != null)
                        {
                            this.OnError(exception);
                        }

                        this.ended = true;

                        return;
                    }

                    if (this.commands.Count > 0)
                    {
                        this.Write();

                        return;
                    }

                    this.writing = false;
                });
            }

            //----------------------------------
            // command: end
            //----------------------------------

            if (command is EndCommand)
            {
                var end = command as EndCommand;

                this.writing = false;

                this.ended   = true;

                try
                {
                    this.socket.Shutdown(SocketShutdown.Send);
                }
                catch(Exception exception)
                {
                    if (this.OnError != null)
                    {
                        this.OnError(exception);
                    }

                    if (this.OnEnd != null)
                    {
                        this.OnEnd();
                    }

                    end.Callback(exception);
                }

                IO.Disconnect(this.socket, false, exception0 =>
                {
                    if (exception0 != null)
                    {
                        if (this.OnError != null)
                        {
                            this.OnError(exception0);
                        }

                        if (this.OnEnd != null)
                        {
                            this.OnEnd();
                        }

                        end.Callback(exception0);

                        return;
                    }

                    try
                    {
                        this.stream.Dispose();

                        end.Callback(null);
                    }
                    catch(Exception exception1)
                    {
                        end.Callback(exception1);
                    }
                });
            }
        }

        #region Statics

        public static Socket Create(int port)
        {
            return new Socket(IPAddress.Loopback, port);
        }

        public static Socket Create(IPAddress address, int port)
        {
            return new Socket(address, port);
        }

        public static Socket Create(string hostname, int port)
        {
            return new Socket(hostname, port);
        }

        #endregion

        #region IWritables

        public void Write(byte[] buffer)
        {
            this.Write(Reactor.Buffer.Create(buffer));
        }

        public void Write(byte[] buffer, int index, int count)
        {
            this.Write(Reactor.Buffer.Create(buffer, 0, count));
        }

        public void Write(string data)
        {
            var buffer = System.Text.Encoding.UTF8.GetBytes(data);

            this.Write(buffer);
        }

        public void Write(string format, object arg0)
        {
            format = string.Format(format, arg0);

            var buffer = System.Text.Encoding.UTF8.GetBytes(format);

            this.Write(buffer);
        }

        public void Write(string format, params object[] args)
        {
            format = string.Format(format, args);

            var buffer = System.Text.Encoding.UTF8.GetBytes(format);

            this.Write(buffer);
        }

        public void Write(string format, object arg0, object arg1)
        {
            format = string.Format(format, arg0, arg1);

            var buffer = System.Text.Encoding.UTF8.GetBytes(format);

            this.Write(buffer);
        }

        public void Write(string format, object arg0, object arg1, object arg2)
        {
            format = string.Format(format, arg0, arg1, arg2);

            var buffer = System.Text.Encoding.UTF8.GetBytes(format);

            this.Write(buffer);
        }

        public void Write(byte data)
        {
            this.Write(new byte[1] { data });
        }

        public void Write(bool value)
        {
            var buffer = BitConverter.GetBytes(value);

            this.Write(buffer);
        }

        public void Write(short value)
        {
            var buffer = BitConverter.GetBytes(value);

            this.Write(buffer);
        }

        public void Write(ushort value)
        {
            var buffer = BitConverter.GetBytes(value);

            this.Write(buffer);
        }

        public void Write(int value)
        {
            var buffer = BitConverter.GetBytes(value);

            this.Write(buffer);
        }

        public void Write(uint value)
        {
            var buffer = BitConverter.GetBytes(value);

            this.Write(buffer);
        }

        public void Write(long value)
        {
            var buffer = BitConverter.GetBytes(value);

            this.Write(buffer);
        }

        public void Write(ulong value)
        {
            var buffer = BitConverter.GetBytes(value);

            this.Write(buffer);
        }

        public void Write(float value)
        {
            var buffer = BitConverter.GetBytes(value);

            this.Write(buffer);
        }

        public void Write(double value)
        {
            var buffer = BitConverter.GetBytes(value);

            this.Write(buffer);
        }

        #endregion
    }
}