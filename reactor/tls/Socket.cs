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
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace Reactor.Tls {

    /// <summary>
    /// Reactor TCP socket.
    /// </summary>
    public class Socket : Reactor.IDuplexable, IDisposable {

        #region States

        /// <summary>
        /// Readable state.
        /// </summary>
        internal enum State {
            /// <summary>
            /// The initial state of this stream. A stream
            /// in a pending state signals that the stream
            /// is waiting on the caller to issue a read request
            /// the the underlying resource, by attaching a
            /// OnRead, OnReadable, or calling Read().
            /// </summary>
            Pending,
            /// <summary>
            /// A stream in a reading state signals that the
            /// stream is currently requesting data from the
            /// underlying resource and is waiting on a 
            /// response.
            /// </summary>
            Reading,
            /// <summary>
            /// A stream in a paused state will bypass attempts
            /// to read on the underlying resource. A paused
            /// stream must be resumed by the caller.
            /// </summary>
            Paused,
            /// <summary>
            /// Indicates this stream has ended. Streams can end
            /// by way of reaching the end of the stream, or through
            /// error.
            /// </summary>
            Ended
        }

        /// <summary>
        /// Readable mode.
        /// </summary>
        internal enum Mode {
            /// <summary>
            /// This stream is using flowing semantics.
            /// </summary>
            Flowing,
            /// <summary>
            /// This stream is using non-flowing semantics.
            /// </summary>
            NonFlowing
        }

        #endregion

        private Reactor.Func<X509Certificate, X509Chain, SslPolicyErrors, bool> certificateValidationCallback;
        private System.Net.Sockets.Socket             socket;
        private Reactor.Async.Queue                   queue;
        private Reactor.Async.Event                   onconnect;
        private Reactor.Async.Event                   ondrain;
        private Reactor.Async.Event                   onreadable;
        private Reactor.Async.Event<Reactor.Buffer>   onread;
        private Reactor.Async.Event<Exception>        onerror;
        private Reactor.Async.Event                   onend;
        private Reactor.Streams.Reader                reader;
        private Reactor.Streams.Writer                writer;
        private Reactor.Buffer                        buffer;
        private Reactor.Interval                      poll;
        private State                                 state;
        private Mode                                  mode;
        private bool                                  corked;
        
        #region Constructors

        /// <summary>
        /// Binds a new socket.
        /// </summary>
        /// <param name="socket">The socket to bind.</param>
        internal Socket (System.Net.Sockets.Socket socket, SslStream stream) {
            this.queue      = Reactor.Async.Queue.Create(1);
            this.onconnect  = Reactor.Async.Event.Create();
            this.ondrain    = Reactor.Async.Event.Create();
            this.onreadable = Reactor.Async.Event.Create();
            this.onread     = Reactor.Async.Event.Create<Reactor.Buffer>();
            this.onerror    = Reactor.Async.Event.Create<Exception>();
            this.onend      = Reactor.Async.Event.Create();
            this.state      = State.Pending;
            this.mode       = Mode.NonFlowing;
            this.corked     = false;
            this.socket     = socket;
            this.reader     = Reactor.Streams.Reader.Create(stream);
            this.writer     = Reactor.Streams.Writer.Create(stream);
            this.poll       = Reactor.Interval.Create(this.Poll, 1000);
            this.buffer     = new Reactor.Buffer();
            this.reader.OnRead  (this._Data);
            this.reader.OnError (this._Error);
            this.reader.OnEnd   (this._End);
            this.writer.OnDrain (this._Drain);
            this.writer.OnError (this._Error);
            this.writer.OnEnd   (this._End);
            this.onconnect.Emit();
            this.queue.Resume();
        }

        /// <summary>
        /// Creates a new socket.
        /// </summary>
        /// <param name="endpoint">The endpoint to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        public Socket (System.Net.IPAddress endpoint, int port, Reactor.Func<X509Certificate, X509Chain, SslPolicyErrors, bool> certificateValidationCallback) {
            this.certificateValidationCallback = certificateValidationCallback;
            this.queue      = Reactor.Async.Queue.Create(1);
            this.onconnect  = Reactor.Async.Event.Create();
            this.ondrain    = Reactor.Async.Event.Create();
            this.onreadable = Reactor.Async.Event.Create();
            this.onread     = Reactor.Async.Event.Create<Reactor.Buffer>();
            this.onerror    = Reactor.Async.Event.Create<Exception>();
            this.onend      = Reactor.Async.Event.Create();
            this.state      = State.Pending;
            this.mode       = Mode.NonFlowing;
            this.corked     = false;
            this.queue.Pause();
            this.Connect(endpoint, port).Then(socket => {
                this.socket = socket;
                var networkstream  = new NetworkStream(socket);
                this.Authenticate(networkstream).Then(stream => {
                    this.reader = Reactor.Streams.Reader.Create(stream);
                    this.writer = Reactor.Streams.Writer.Create(stream);
                    this.poll   = Reactor.Interval.Create(this.Poll, 1000);
                    this.buffer = new Reactor.Buffer();
                    this.reader.OnRead  (this._Data);
                    this.reader.OnError (this._Error);
                    this.reader.OnEnd   (this._End);
                    this.writer.OnDrain (this._Drain);
                    this.writer.OnError (this._Error);
                    this.writer.OnEnd   (this._End);
                    if(this.corked) this.Cork();
                    this.onconnect.Emit();
                    this.queue.Resume();
                }).Error(this._Error);
            }).Error(this._Error);
        }

        /// <summary>
        /// Creates a new socket.
        /// </summary>
        /// <param name="endpoint">The endpoint to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        public Socket (string hostname, int port, Reactor.Func<X509Certificate, X509Chain, SslPolicyErrors, bool> certificateValidationCallback) {
            this.certificateValidationCallback = certificateValidationCallback;
            this.queue      = Reactor.Async.Queue.Create(1);
            this.onconnect  = Reactor.Async.Event.Create();
            this.ondrain    = Reactor.Async.Event.Create();
            this.onreadable = Reactor.Async.Event.Create();
            this.onread     = Reactor.Async.Event.Create<Reactor.Buffer>();
            this.onerror    = Reactor.Async.Event.Create<Exception>();
            this.onend      = Reactor.Async.Event.Create();
            this.state      = State.Pending;
            this.mode       = Mode.NonFlowing;
            this.corked     = false;
            this.queue.Pause();
            this.ResolveHost(hostname).Then(endpoint => {
                this.Connect(endpoint, port).Then(socket => {
                    this.socket = socket;
                    var networkstream  = new NetworkStream(socket);
                    this.Authenticate(networkstream).Then(stream => {
                        this.reader = Reactor.Streams.Reader.Create(stream);
                        this.writer = Reactor.Streams.Writer.Create(stream);
                        this.poll   = Reactor.Interval.Create(this.Poll, 1000);
                        this.buffer = new Reactor.Buffer();
                        this.reader.OnRead  (this._Data);
                        this.reader.OnError (this._Error);
                        this.reader.OnEnd   (this._End);
                        this.writer.OnDrain (this._Drain);
                        this.writer.OnError (this._Error);
                        this.writer.OnEnd   (this._End);
                        if(this.corked) this.Cork();
                        this.onconnect.Emit();
                        this.queue.Resume();
                    }).Error(this._Error);
                }).Error(this._Error);
            }).Error(this._Error);
        }

        #endregion

        #region Events

        /// <summary>
        /// Subscribes this action to the 'connect' event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnConnect (Reactor.Action callback) {
            this.onconnect.On(callback);
        }

        /// <summary>
        /// Unsubscribes this action from the 'connect' event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveConnect (Reactor.Action callback) {
            this.onconnect.Remove(callback);
        }

        /// <summary>
        /// Subscribes this action to the 'drain' event. The event indicates
        /// when a write operation has completed and the caller should send
        /// more data.
        /// </summary>
        /// <param name="callback"></param>
        public void OnDrain(Reactor.Action callback) {
            this.ondrain.On(callback);
        }

        /// <summary>
        /// Subscribes this action once to the 'drain' event. The event indicates
        /// when a write operation has completed and the caller should send
        /// more data.
        /// </summary>
        /// <param name="callback"></param>
        public void OnceDrain(Reactor.Action callback) {
            this.ondrain.Once(callback);
        }

        /// <summary>
        /// Unsubscribes from the OnDrain event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveDrain(Reactor.Action callback) {
            this.ondrain.Remove(callback);
        }

        /// <summary>
        /// Subscribes this action to the 'readable' event. When a chunk of 
        /// data can be read from the stream, it will emit a 'readable' event.
        /// Listening for a 'readable' event will cause some data to be read 
        /// into the internal buffer from the underlying resource. If a stream 
        /// happens to be in a 'paused' state, attaching a readable event will 
        /// transition into a pending state prior to reading from the resource.
        /// </summary>
        /// <param name="callback"></param>
        public void OnReadable (Reactor.Action callback) {
            this.onreadable.On(callback);
            this.queue.Run(next => {
                this.mode = Mode.NonFlowing;
                if (this.state == State.Paused) {
                    this.state = State.Pending;
                }
                this._Read();
                next();
            });
        }

        /// <summary>
        /// Subscribes this action once to the 'readable' event. When a chunk of 
        /// data can be read from the stream, it will emit a 'readable' event.
        /// Listening for a 'readable' event will cause some data to be read 
        /// into the internal buffer from the underlying resource. If a stream 
        /// happens to be in a 'paused' state, attaching a readable event will 
        /// transition into a pending state prior to reading from the resource.
        /// </summary>
        /// <param name="callback"></param>
        public void OnceReadable(Reactor.Action callback) {
            this.onreadable.Once(callback);
            this.queue.Run(next => {
                this.mode = Mode.NonFlowing;
                if (this.state == State.Paused) {
                    this.state = State.Pending;
                }
                this._Read();
                next();
            });
        }

        /// <summary>
        /// Unsubscribes this action from the 'readable' event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveReadable(Reactor.Action callback) {
			this.onreadable.Remove(callback);
        }

        /// <summary>
        /// Subscribes this action to the 'read' event. Attaching a data event 
        /// listener to a stream that has not been explicitly paused will 
        /// switch the stream into flowing mode and begin reading immediately. 
        /// Data will then be passed as soon as it is available.
        /// </summary>
        /// <param name="callback"></param>
        public void OnRead (Reactor.Action<Reactor.Buffer> callback) {
            this.onread.On(callback);
            this.queue.Run(next => {
                if (this.state == State.Pending) {
                    this.Resume();
                }
                next();
            });
        }

        /// <summary>
        /// Subscribes this action once to the 'read' event. Attaching a data event 
        /// listener to a stream that has not been explicitly paused will 
        /// switch the stream into flowing mode and begin reading immediately. 
        /// Data will then be passed as soon as it is available.
        /// </summary>
        /// <param name="callback"></param>
        public void OnceRead(Reactor.Action<Reactor.Buffer> callback) {
            this.onread.Once(callback);
            this.queue.Run(next => {
                if (this.state == State.Pending) {
                    this.Resume();
                }
                next();
            });
        }

        /// <summary>
        /// Unsubscribes this action from the 'read' event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveRead (Reactor.Action<Reactor.Buffer> callback) {
            this.onread.Remove(callback);
        }

        /// <summary>
        /// Subscribes this action to the 'error' event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnError (Reactor.Action<Exception> callback) {
            this.onerror.On(callback);
        }

        /// <summary>
        /// Unsubscribes this action from the 'error' event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveError (Reactor.Action<Exception> callback) {
            this.onerror.Remove(callback);
        }

        /// <summary>
        /// Subscribes this action to the 'end' event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnEnd (Reactor.Action callback) {
            this.onend.On(callback);
        }

        /// <summary>
        /// Unsubscribes this action from the 'end' event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveEnd (Reactor.Action callback) {
            this.onend.Remove(callback);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Will read this number of bytes out of the internal buffer. If there 
        /// is no data available, then it will return a zero length buffer. If 
        /// the internal buffer has been completely read, then this method will 
        /// issue a new read request on the underlying resource in non-flowing 
        /// mode. Any data read with a length > 0 will also be emitted as a 'read' 
        /// event.
        /// </summary>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns></returns>
        public Reactor.Buffer Read (int count) {
            var result = Reactor.Buffer.Create(this.buffer.Read(count));
            if (result.Length > 0) {
                this.onread.Emit(result);
            }
            if (this.buffer.Length == 0) {
                this.mode = Mode.NonFlowing;
                this._Read();
            }
            return result;
        }

        /// <summary>
        /// Will read all data out of the internal buffer. If no data is available 
        /// then it will return a zero length buffer. This method will then issue 
        /// a new read request on the underlying resource in non-flowing mode. Any 
        /// data read with a length > 0 will also be emitted as a 'read' event.
        /// </summary>
        public Reactor.Buffer Read () {
            return this.Read(this.buffer.Length);
        }

        /// <summary>
        /// Unshifts this buffer back to this stream.
        /// </summary>
        /// <param name="buffer">The buffer to unshift.</param>
        public void Unshift (Reactor.Buffer buffer) {
            this.buffer.Unshift(buffer);
        }

        /// <summary>
        /// Writes this buffer to the stream.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="callback">A callback to signal when this buffer has been written.</param>
        public Reactor.Async.Future Write (Reactor.Buffer buffer) {
            return new Reactor.Async.Future((resolve, reject)=>{
                this.queue.Run(next => {
                    this.writer.Write(buffer)
                               .Then(resolve)
                               .Error(reject)
                               .Finally(next);
                });
            });
        }

        /// <summary>
        /// Flushes this stream.
        /// </summary>
        /// <param name="callback">A callback to signal when this buffer has been flushed.</param>
        public Reactor.Async.Future Flush () {
            return new Reactor.Async.Future((resolve, reject)=>{
                this.queue.Run(next => {
                    this.writer.Flush()
                               .Then(resolve)
                               .Error(reject)
                               .Finally(next);
                });
            });
        }

        /// <summary>
        /// Ends this stream.
        /// </summary>
        /// <param name="callback">A callback to signal when this stream has ended.</param>
        public Reactor.Async.Future End () {
            return new Reactor.Async.Future((resolve, reject) => {
                this.queue.Run(next => {
                    this._End();
                    next();          
                });
            });
        }

        /// <summary>
        /// Forces buffering of all writes. Buffered data will be 
        /// flushed either at .Uncork() or at .End() call.
        /// </summary>
        public void Cork() {
            this.corked = true;
            if(this.writer != null)
                this.writer.Cork();
        }

        /// <summary>
        /// Flush all data, buffered since .Cork() call.
        /// </summary>
        public void Uncork() {
            this.corked = false;
            if(this.writer != null)
                this.writer.Uncork();
        }

        /// <summary>
        /// Pauses this stream. This method will cause a 
        /// stream in flowing mode to stop emitting data events, 
        /// switching out of flowing mode. Any data that becomes 
        /// available will remain in the internal buffer.
        /// </summary>
        public void Pause() {
            this.mode  = Mode.NonFlowing;
            this.state = State.Paused;
        }

        /// <summary>
        /// This method will cause the readable stream to resume emitting data events.
        /// This method will switch the stream into flowing mode. If you do not want 
        /// to consume the data from a stream, but you do want to get to its end event, 
        /// you can call readable.resume() to open the flow of data.
        /// </summary>
        public void Resume() {
            this.mode  = Mode.Flowing;
            this.state = State.Pending;
            this.queue.Run(next => {
                this._Read();
                next();
            });
        }

        /// <summary>
        /// Pipes data to a writable stream.
        /// </summary>
        /// <param name="writable"></param>
        /// <returns></returns>
        public Reactor.IReadable Pipe (Reactor.IWritable writable) {
            this.OnRead(data => {
                this.Pause();
                writable.Write(data)
                        .Then(this.Resume)
                        .Error(this._Error);
            });
            this.OnEnd (() => writable.End());
            return this;
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

        #region Buffer

        /// <summary>
        /// Writes this data to the stream.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (byte[] buffer, int index, int count) {
            return this.Write(Reactor.Buffer.Create(buffer, 0, count));
        }

        /// <summary>
        /// Writes this data to the stream.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (byte[] buffer) {
            return this.Write(Reactor.Buffer.Create(buffer));
        }

        /// <summary>
        /// Writes this data to the stream.
        /// </summary>
        /// <param name="data"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (string data) {
            return this.Write(System.Text.Encoding.UTF8.GetBytes(data));
        }

        /// <summary>
        /// Writes this data to the stream.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (string format, params object[] args) {
            format = string.Format(format, args);
            return this.Write(System.Text.Encoding.UTF8.GetBytes(format));
        }

        /// <summary>
        /// Writes this data to the stream.
        /// </summary>
        /// <param name="data"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (byte data) {
            return this.Write(new byte[1] { data });
        }

        /// <summary>
        /// Writes a System.Boolean value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (bool value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.Int16 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (short value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.UInt16 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (ushort value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.Int32 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (int value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.UInt32 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (uint value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.Int64 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (long value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.UInt64 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (ulong value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.Single value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (float value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.Double value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (double value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts this data to the stream.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        public void Unshift (byte[] buffer, int index, int count) {
            this.Unshift(Reactor.Buffer.Create(buffer, 0, count));
        }

        /// <summary>
        /// Unshifts this data to the stream.
        /// </summary>
        /// <param name="buffer"></param>
        public void Unshift (byte[] buffer) {
            this.Unshift(Reactor.Buffer.Create(buffer));
        }

        /// <summary>
        /// Unshifts this data to the stream.
        /// </summary>
        /// <param name="data"></param>
        public void Unshift (char data) {
            this.Unshift(data.ToString());
        }

        /// <summary>
        /// Unshifts this data to the stream.
        /// </summary>
        /// <param name="data"></param>
        public void Unshift (string data) {
            this.Unshift(System.Text.Encoding.UTF8.GetBytes(data));
        }

        /// <summary>
        /// Unshifts this data to the stream.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void Unshift (string format, params object[] args) {
            format = string.Format(format, args);
            this.Unshift(System.Text.Encoding.UTF8.GetBytes(format));
        }

        /// <summary>
        /// Unshifts this data to the stream.
        /// </summary>
        /// <param name="data"></param>
        public void Unshift (byte data) {
            this.Unshift(new byte[1] { data });
        }

        /// <summary>
        /// Unshifts a System.Boolean value to the stream.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (bool value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts a System.Int16 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (short value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts a System.UInt16 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (ushort value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts a System.Int32 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (int value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts a System.UInt32 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (uint value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts a System.Int64 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (long value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts a System.UInt64 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (ulong value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts a System.Single value to the stream.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (float value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts a System.Double value to the stream.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (double value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        #endregion

        #region Internal

        /// <summary>
        /// Resolves the host ip address from the hostname.
        /// </summary>
        /// <param name="hostname">The hostname or ip to resolve.</param>
        /// <returns></returns>
        private Reactor.Async.Future<System.Net.IPAddress> ResolveHost (string hostname) {
            return new Reactor.Async.Future<System.Net.IPAddress>((resolve, reject) => {
                Reactor.Dns.GetHostAddresses(hostname)
                           .Then(addresses => {
                                if (addresses.Length == 0) 
                                    reject(new Exception("host not found"));
                                else
                                    resolve(addresses[0]);
                            }).Error(reject);
            });
        }
        
        /// <summary>
        /// Connects to a remote TCP endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <param name="port">The port.</param>
        /// <returns></returns>
        private Reactor.Async.Future<System.Net.Sockets.Socket> Connect (System.Net.IPAddress endpoint, int port) {
            return new Reactor.Async.Future<System.Net.Sockets.Socket>((resolve, reject) => {
                var socket   = new System.Net.Sockets.Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try {
                    socket.BeginConnect(endpoint, port, result => {
                        Loop.Post(() => {
                            try {
                                socket.EndConnect(result);
                                resolve(socket);
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

        /// <summary>
        /// Authenticates the client network stream as a client.
        /// </summary>
        /// <param name="networkstream">the stream to authenticate.</param>
        /// <returns></returns>
        private Reactor.Async.Future<SslStream> Authenticate (System.Net.Sockets.NetworkStream networkstream) {
            return new Reactor.Async.Future<SslStream>((resolve, reject) => {
                var stream   = new SslStream(networkstream, false, (sender, certificate, chain, errors) => {
                    return this.certificateValidationCallback(certificate, chain, errors);
                }, null);
                try {
                    stream.BeginAuthenticateAsClient("localhost", result =>  {
                        Loop.Post(() => {
                            try {
                                stream.EndAuthenticateAsClient(result);
                                resolve(stream);
                            }
                            catch (Exception error) {
                                reject(error);
                            }
                        });

                    }, null);
                }
                catch (Exception error) {
                    reject(error);
                }
            });
        }


        /// <summary>
        /// Polls the active state on this socket.
        /// </summary>
        private int poll_failed = 0;
        private void Poll () {
            /* poll within a fiber to prevent interuptions
             * from the main thread. allow for 4 failed
             * attempts before signalling termination. */
            Reactor.Fibers.Fiber.Create(() => {
                var result = !(this.socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
                if (!result) {
                    poll_failed = poll_failed + 1;
                    if (poll_failed > 4) {
                        throw new Exception("socket: poll detected unexpected termination");
                    }
                } else poll_failed = 0;
            }).Error(this._Error);
        }

        /// <summary>
        /// Disconnects this socket.
        /// </summary>
        /// <returns></returns>
        private Reactor.Async.Future Disconnect () {
            return new Reactor.Async.Future((resolve, reject) => {
                try {
                    this.socket.BeginDisconnect(false, (result) => {
                        Loop.Post(() => {
                            try {
                                socket.EndDisconnect(result);
                                resolve();
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
        /// Handles OnDrain events.
        /// </summary>
        private void _Drain () {
            this.ondrain.Emit();
        }

        /// <summary>
        /// Begins reading from the underlying stream.
        /// </summary>
        private void _Read () {
            if (this.state == State.Pending) {
                this.state = State.Reading;
                if (this.buffer.Length > 0) {
                    var clone = this.buffer.Clone();
                    this.buffer.Clear();
                    this._Data(clone);
                }
                else {
                    this.reader.Read();
                }
            }
        }

        /// <summary>
        /// Handles incoming data from the stream.
        /// </summary>
        /// <param name="buffer"></param>
        private void _Data (Reactor.Buffer buffer) {
            if (this.state == State.Reading) {
                this.state = State.Pending;
                this.buffer.Write(buffer);
                switch (this.mode) {
                    case Mode.Flowing:
                        var clone = this.buffer.Clone();
                        this.buffer.Clear();
                        this.onread.Emit(clone);
                        this._Read();
                        break;
                    case Mode.NonFlowing:
                        this.onreadable.Emit();
                        break;
                } 
            }
        }

        /// <summary>
        /// Handles stream errors.
        /// </summary>
        /// <param name="error"></param>
        private void _Error (Exception error) {
            if (this.state != State.Ended) { 
                this.onerror.Emit(error);
                this._End();
            }
        }

        /// <summary>
        /// Terminates the stream.
        /// </summary>
        public void _End () {
            if (this.state != State.Ended) {
                this.state = State.Ended;
                try { this.socket.Shutdown(SocketShutdown.Send); } catch {}
                this.Disconnect().Finally(() => {
                    if (this.poll   != null) this.poll.Clear();
                    if (this.writer != null) this.writer.Dispose();
                    if (this.reader != null) this.reader.Dispose();
                    this.queue.Dispose();
                    this.onend.Emit();
                });
            }
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Disposes of this stream.
        /// </summary>
        public void Dispose() {
            this._End();
        }

        #endregion

        #region Statics

        /// <summary>
        /// Creates a new TLS socket and connects to localhost.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="clientValidationCallback">The client certificate validation callback.</param>
        /// <returns></returns>
        public static Socket Create (int port, Reactor.Func<X509Certificate, X509Chain, SslPolicyErrors, bool> clientValidationCallback) {
            return new Socket(IPAddress.Loopback, port, clientValidationCallback);
        }

        /// <summary>
        /// Creates a new TLS socket.
        /// </summary>
        /// <param name="endpoint">The endpoint to connect to.</param>
        /// <param name="port">The port.</param>
        /// <param name="clientValidationCallback">The client certificate validation callback.</param>
        /// <returns></returns>
        public static Socket Create (System.Net.IPAddress endpoint, int port, Reactor.Func<X509Certificate, X509Chain, SslPolicyErrors, bool> clientValidationCallback) {
            return new Socket(endpoint, port, clientValidationCallback);
        }

        /// <summary>
        /// Creates a new TLS socket.
        /// </summary>
        /// <param name="hostname">The hostname to connect to.</param>
        /// <param name="port">The port.</param>
        /// <param name="clientValidationCallback">The client certificate validation callback.</param>
        /// <returns></returns>
        public static Socket Create (string hostname, int port, Reactor.Func<X509Certificate, X509Chain, SslPolicyErrors, bool> clientValidationCallback) {
            return new Socket(hostname, port, clientValidationCallback);
        }

        /// <summary>
        /// Creates a new TLS socket and connects to localhost.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <returns></returns>
        public static Socket Create (int port) {
            return new Socket(IPAddress.Loopback, port, (certificate, chain, errors) => {
                if (errors == SslPolicyErrors.None) {
                    return true;
                }
                return false;
            });
        }

        /// <summary>
        /// Creates a new TLS socket.
        /// </summary>
        /// <param name="endpoint">The endpoint to connect to.</param>
        /// <param name="port">The port.</param>
        /// <returns></returns>
        public static Socket Create (System.Net.IPAddress endpoint, int port) {
            return new Socket(endpoint, port, (certificate, chain, errors) => {
                if (errors == SslPolicyErrors.None) {
                    return true;
                }
                return false;
            });
        }

        /// <summary>
        /// Creates a new TLS socket.
        /// </summary>
        /// <param name="hostname">The hostname to connect to.</param>
        /// <param name="port">The port.</param>
        /// <returns></returns>
        public static Socket Create (string hostname, int port) {
            return new Socket(hostname, port, (certificate, chain, errors) => {
                if (errors == SslPolicyErrors.None) {
                    return true;
                }
                return false;
            });
        }

        #endregion
    }
}