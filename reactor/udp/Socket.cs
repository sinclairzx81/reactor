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
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Reactor.Udp {

    /// <summary>
    /// Reactor UDP Socket.
    /// </summary>
    public class Socket : IDisposable {

        #region State

        /// <summary>
        /// Socket state.
        /// </summary>
        internal enum State {
            /// <summary>
            /// A state indicating this socket is active.
            /// </summary>
            Active,
            /// <summary>
            /// A state indicating this socket is closed.
            /// </summary>
            Ended
        }

        #endregion

        private System.Net.Sockets.Socket                socket;
        private Reactor.Async.Queue                      queue;
        private Reactor.Async.Event<Reactor.Udp.Message> onread;
        private Reactor.Async.Event<System.Exception>    onerror;
        private Reactor.Async.Event                      onend;
        private State                                    state;
        private byte[]                                   read_buffer;

        #region Constructors

        /// <summary>
        /// Creates a new UDP socket.
        /// </summary>
        public Socket(int buffersize) {
            this.socket      = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            this.queue       = Reactor.Async.Queue.Create(1);
            this.onread      = Reactor.Async.Event.Create<Reactor.Udp.Message>();
            this.onerror     = Reactor.Async.Event.Create<System.Exception>();
            this.onend       = Reactor.Async.Event.Create();
            this.read_buffer = new byte[buffersize];
            this.state       = State.Active;
        }

        #endregion

        #region Events

        /// <summary>
        /// Subscribes this action to the OnRead event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnRead (Reactor.Action<Reactor.Udp.Message> callback) {
            this.onread.On(callback);
            this._Read();
        }

        /// <summary>
        /// Unsubscribes this action from the OnRead event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveRead (Reactor.Action<Reactor.Udp.Message> callback) {
            this.onread.Remove(callback);
        }

        /// <summary>
        /// Subscribes this action to the OnError event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnError (Reactor.Action<System.Exception> callback) {
            this.onerror.On(callback);
        }

        /// <summary>
        /// Unsubscribes this action from the OnError event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveError (Reactor.Action<System.Exception> callback) {
            this.onerror.Remove(callback);
        }

        /// <summary>
        /// Subscribes this action to the OnEnd event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnEnd(Reactor.Action callback) {
            this.onend.On(callback);
        }

        /// <summary>
        /// Unsubscribes this action from the OnEnd event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveEnd(Reactor.Action callback) {
            this.onend.Remove(callback);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Associates this socket with a local endpoint.
        /// </summary>
        /// <param name="endpoint"></param>
        public Socket Bind (IPEndPoint endpoint) {
            this.socket.Bind(endpoint);
            this._Read();
            return this;
        }

        /// <summary>
        /// Associates this socket with a local endpoint.
        /// </summary>
        /// <param name="endpoint"></param>
        public Socket Bind (IPAddress address, int port) {
            return this.Bind(new IPEndPoint(address, port));
        }

        /// <summary>
        /// Associates this socket with a local endpoint.
        /// </summary>
        /// <param name="endpoint"></param>
        public Socket Bind (int port) {
            return this.Bind(new IPEndPoint(IPAddress.Any, port));
        }

        /// <summary>
        /// Associates this socket with a local endpoint.
        /// </summary>
        /// <param name="endpoint"></param>
        public Socket Bind () {
           return this.Bind(new IPEndPoint(IPAddress.Any, 0));
        }

        /// <summary>
        /// Sends this message to the socket.
        /// </summary>
        /// <param name="message"></param>
        public Reactor.Async.Future<int> Send (Reactor.Udp.Message message) {
            message.Buffer.Locked = true;
            return new Reactor.Async.Future<int>((resolve, reject) => {
                this.SendTo(message)
                    .Then(resolve)
                    .Error(reject)
                    .Error(this._Error);
            });
        }

        /// <summary>
        /// Ends this socket.
        /// </summary>
        public void End() {
            this._End();
        }

        #endregion

        #region Socket

        /// <summary>
        /// Gets the address family of the Socket.
        /// </summary>
        public AddressFamily AddressFamily {
            get { return this.socket.AddressFamily; }
        }

        /// <summary>
        /// Gets the amount of data that has been received from the network and is available to be read.
        /// </summary>
        public int Available {
            get { return this.socket.Available; }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the Socket is in blocking mode.
        /// </summary>
        public bool Blocking {
            get { return this.socket.Blocking; }
            set { this.socket.Blocking = value; }
        }

        /// <summary>
        /// Gets a value that indicates whether a Socket is connected to a remote host as of the last Send or Receive operation.
        /// </summary>
        public bool Connected {
            get { return this.socket.Connected; }
        }

        /// <summary>
        /// Gets or sets a Boolean value that specifies whether the Socket allows Internet Protocol (IP) datagrams to be fragmented.
        /// </summary>
        public bool DontFragment {
            get { return this.socket.DontFragment; }
            set { this.socket.DontFragment = value; }
        }

        /// <summary>
        /// Gets or sets a Boolean value that specifies whether the Socket can send or receive broadcast packets.
        /// </summary>
        public bool EnableBroadcast {
            get { return this.socket.EnableBroadcast; }
            set { this.socket.EnableBroadcast = value; }
        }

        /// <summary>
        /// Gets or sets a Boolean value that specifies whether the Socket allows only one process to bind to a port.
        /// </summary>
        public bool ExclusiveAddressUse {
            get { return this.socket.ExclusiveAddressUse; }
            set { this.socket.ExclusiveAddressUse = value; }
        }

        /// <summary>
        /// Gets the operating system handle for the Socket.
        /// </summary>
        public IntPtr Handle {
            get { return this.socket.Handle; }
        }

        /// <summary>
        /// Gets a value that indicates whether the Socket is bound to a specific local port.
        /// </summary>
        public bool IsBound {
            get { return this.socket.IsBound; }
        }

        /// <summary>
        /// Gets or sets a value that specifies whether the Socket will delay closing a socket in an attempt to send all pending data.
        /// </summary>
        public LingerOption LingerState {
            get { return this.socket.LingerState; }
            set { this.socket.LingerState = value; }
        }

        /// <summary>
        /// Gets the local endpoint.
        /// </summary>
        public EndPoint LocalEndPoint {
            get { return this.socket.LocalEndPoint; }
        }

        /// <summary>
        /// Gets or sets a value that specifies whether outgoing multicast packets are delivered to the sending application.
        /// </summary>
        public bool MulticastLoopback {
            get { return this.socket.MulticastLoopback; }
            set { this.socket.MulticastLoopback = value; }
        }

        /// <summary>
        /// Gets or sets a Boolean value that specifies whether the stream Socket is using the Nagle algorithm.
        /// </summary>
        public bool NoDelay {
            get { return this.socket.NoDelay; }
            set { this.socket.NoDelay = value; }
        }

        /// <summary>
        /// Indicates whether the underlying operating system and network adaptors support Internet Protocol version 4 (IPv4).
        /// </summary>
        public static bool OSSupportsIPv4{
            get { return Socket.OSSupportsIPv4; }
        }

        /// <summary>
        /// Indicates whether the underlying operating system and network adaptors support Internet Protocol version 6 (IPv6).
        /// </summary>
        public static bool OSSupportsIPv6 {
            get { return Socket.OSSupportsIPv6; }
        }

        /// <summary>
        /// Gets the protocol type of the Socket.
        /// </summary>
        public ProtocolType ProtocolType {
            get { return this.socket.ProtocolType; }
        }

        /// <summary>
        /// Gets or sets a value that specifies the size of the receive buffer of the Socket.
        /// </summary>
        public int ReceiveBufferSize {
            get { return this.socket.ReceiveBufferSize; }
            set { this.socket.ReceiveBufferSize = value; }
        }

        /// <summary>
        /// Gets or sets a value that specifies the amount of time after which a synchronous Receive call will time out.
        /// </summary>
        public int ReceiveTimeout {
            get { return this.socket.ReceiveTimeout; }
            set { this.socket.ReceiveTimeout = value; }
        }

        /// <summary>
        /// Gets the remote endpoint.
        /// </summary>
        public EndPoint RemoteEndPoint {
            get { return this.socket.RemoteEndPoint; }
        }

        /// <summary>
        /// Gets or sets a value that specifies the size of the send buffer of the Socket.
        /// </summary>
        public int SendBufferSize {
            get { return this.socket.SendBufferSize; }
            set { this.socket.SendBufferSize = value; }
        }

        /// <summary>
        /// Gets or sets a value that specifies the amount of time after which a synchronous Send call will time out.
        /// </summary>
        public int SendTimeout {
            get { return this.socket.SendTimeout; }
            set { this.socket.SendTimeout = value; }
        }

        /// <summary>
        /// Gets the type of the Socket.
        /// </summary>
        public SocketType SocketType {
            get { return this.socket.SocketType; }
        }

        /// <summary>
        /// Gets or sets a value that specifies the Time To Live (TTL) value of Internet Protocol (IP) packets sent by the Socket.
        /// </summary>
        public short Ttl {
            get { return this.socket.Ttl; }
            set { this.socket.Ttl = value; }
        }

        /// <summary>
        /// Specifies whether the socket should only use Overlapped I/O mode.
        /// </summary>
        public bool UseOnlyOverlappedIO {
            get { return this.socket.Blocking; }
            set { this.socket.Blocking = value; }
        }

        /// <summary>
        /// Sets the specified Socket option to the specified Boolean value.
        /// </summary>
        /// <param name="optionLevel"></param>
        /// <param name="optionName"></param>
        /// <param name="optionValue"></param>
        public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, bool optionValue) {
            this.socket.SetSocketOption(optionLevel, optionName, optionValue);
        }

        /// <summary>
        /// Sets the specified Socket option to the specified value, represented as a byte array.
        /// </summary>
        /// <param name="optionLevel"></param>
        /// <param name="optionName"></param>
        /// <param name="optionValue"></param>
        public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue) {
            this.socket.SetSocketOption(optionLevel, optionName, optionValue);
        }

        /// <summary>
        /// Sets the specified Socket option to the specified integer value.
        /// </summary>
        /// <param name="optionLevel"></param>
        /// <param name="optionName"></param>
        /// <param name="optionValue"></param>
        public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, int optionValue) {
            this.socket.SetSocketOption(optionLevel, optionName, optionValue);
        }

        /// <summary>
        /// Sets the specified Socket option to the specified value, represented as an object.
        /// </summary>
        /// <param name="optionLevel"></param>
        /// <param name="optionName"></param>
        /// <param name="optionValue"></param>
        public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, object optionValue) {
            this.socket.SetSocketOption(optionLevel, optionName, optionValue);
        }
        
        #endregion

        #region Internal

        /// <summary>
        /// Resolves the hostname, and caches the result for future requests.
        /// </summary>
        private Dictionary<string, IPAddress> cached_addresses = new Dictionary<string,IPAddress>();
        private Reactor.Async.Future<System.Net.IPAddress> ResolveHost (string hostname) {
            return new Reactor.Async.Future<System.Net.IPAddress>((resolve, reject) => {
                if (cached_addresses.ContainsKey(hostname)) {
                    resolve(cached_addresses[hostname]);
                    return;
                }
                Reactor.Dns.GetHostAddresses(hostname).Then(addresses => {
                    if (addresses.Length == 0) {
                        reject(new Exception("host not found"));
                        return;
                    }
                    this.cached_addresses[hostname] = addresses[0];
                    resolve(addresses[0]);
                });
            });
        }

        /// <summary>
        /// Sends a message to the underlying socket.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private Reactor.Async.Future<int> SendTo(Reactor.Udp.Message message) {
            return new Reactor.Async.Future<int>((resolve, reject) => {
                this.queue.Run(next => {
                    try {
                        var data = message.Buffer.ToArray();
                        this.socket.BeginSendTo(data, 0, data.Length, SocketFlags.None, message.EndPoint, result => {
                            Loop.Post(() => {
                                try {
                                    int sent = socket.EndSendTo(result);
                                    resolve(sent);
                                    next();
                                }
                                catch (Exception error) {
                                    reject(error);
                                    next();
                                }
                            });
                        }, null);
                    }
                    catch(Exception error) {
                        reject(error);
                        next();
                    }
                });
            });
        }

        /// <summary>
        ///Receives a message from this socket.
        /// </summary>
        /// <returns></returns>
        private Reactor.Async.Future<Reactor.Udp.Message> ReceiveFrom () {
            return new Reactor.Async.Future<Reactor.Udp.Message>((resolve, reject) => {
                try {
                    EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    this.socket.BeginReceiveFrom(this.read_buffer, 0, this.read_buffer.Length, SocketFlags.None, ref remoteEP, result => {
                        Loop.Post(() => {
                            try {
                                int read = this.socket.EndReceiveFrom(result, ref remoteEP);
                                var buffer = Reactor.Buffer.Create(this.read_buffer, 0, read);
                                var endpoint = remoteEP;
                                resolve(new Reactor.Udp.Message(endpoint, buffer));
                            }
                            catch (Exception error) {
                                reject(error);
                            }
                        });
                    }, null);
                }
                catch(Exception error) {
                     reject(error);
                }
            });
        }

        #endregion

        #region Machine

        /// <summary>
        /// Emits a error.
        /// </summary>
        /// <param name="error"></param>
        private void _Error(Exception error) {
            if (this.state != State.Ended) {
                this.onerror.Emit(error);
                this._End();
            }
        }

        /// <summary>
        /// Ends this socket.
        /// </summary>
        private void _End() {
            if (this.state != State.Ended) {
                this.state = State.Ended;
                this.socket.Close();
                this.onend.Emit();
            }
        }

        /// <summary>
        /// Reads from the socket while active.
        /// </summary>
        private void _Read() {
            this.ReceiveFrom().Then(message => {
                if (this.state == State.Active) {
                    this.onread.Emit(message);
                    this._Read();
                }
            }).Error(this._Error);
        }

        #endregion

        #region IDisposable

        public void Dispose() {
            this._End();
        }

        ~Socket() {
            Loop.Post(() => { this._End(); });
        }

        #endregion
        
        #region Statics
        
        /// <summary>
        /// Returns a new UDP socket with the specified buffer receive size.
        /// </summary>
        /// <param name="buffersize"></param>
        /// <returns></returns>
        public static Socket Create(int buffersize) {
            return new Socket(buffersize);
        }

        /// <summary>
        /// Returns a new UDP socket. 
        /// </summary>
        /// <returns></returns>
        public static Socket Create() {
            return new Socket(Reactor.Settings.DefaultBufferSize);
        }

        #endregion
    }
}
