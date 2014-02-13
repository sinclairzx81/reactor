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

namespace Reactor.Fusion
{
    internal enum SocketState
    {
        Null,

        Connecting,

        Established,

        Closed
    }

    public partial class Socket
    {
        //-------------------------------------------------------------
        // configurables
        //-------------------------------------------------------------

        public static int    PacketSize          = 1400;

        public static int    RTTSamplerSize      = 32;

        public static ushort DefaultWindowSize   = 8;

        public static int    HandshakeTimeout    = 2000;

        public static int    KeepAliveTimeout    = 200;

        //-------------------------------------------------------------
        // members
        //-------------------------------------------------------------

        private Reactor.Udp.Socket    UdpSocket           { get; set; }

        private object                SendLock            { get; set; }

        private SocketState           SocketState         { get; set; }

        private object                ReceiveLock         { get; set; }

        private EndPoint              RemoteEndPoint      { get; set; }

        private SendBuffer            SendBuffer          { get; set; }

        private TimeoutBuffer         TimeoutBuffer       { get; set; }

        private ReceiveBuffer         ReceiveBuffer       { get; set; }

        private RTTBuffer             RTTBuffer           { get; set; }

        private bool                  IsServer            { get; set; }

        //-------------------------------------------------------------
        // sender variables
        //-------------------------------------------------------------

        private int                  SenderWindowSize    { get; set; }

        private int                  ReceiveWindowSize   { get; set; }

        //-------------------------------------------------------------
        // keep alive variables
        //-------------------------------------------------------------

        private DateTime             LastSignal               { get; set; }

        private int                  KeepAliveCounter         { get; set; }

        //-------------------------------------------------------------
        // events
        //-------------------------------------------------------------

        public event Action                  OnConnect;

        public event Action<Reactor.Buffer>  OnData;

        public event Action<Exception>       OnError;

        public event Action                  OnEnd;

        public event Action                  OnClose;

        #region Constructors

        internal Socket(Reactor.Udp.Socket Socket, EndPoint RemoteEndPoint)
        {
            this.SendLock                 = new object();

            this.SocketState              = SocketState.Null;

            this.ReceiveLock              = new object();

            this.RemoteEndPoint           = RemoteEndPoint;

            this.UdpSocket                = Socket;

            this.SenderWindowSize         = 1;

            this.ReceiveWindowSize        = 1;

            this.IsServer                 = true;

            
        }

        public Socket(string Hostname, int Port)
        {
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

                this.InitializeClientSocket(addresses[0], Port);
            });
        }

        public Socket(IPAddress IPAddress, int Port)
        {
            this.InitializeClientSocket(IPAddress, Port);
        }

        #endregion

        #region Properties
        
        public int Buffered
        {
            get
            {
                return this.SendBuffer.Length;
            }
        }

        #endregion

        #region Initializers
        
        private void InitializeClientSocket(IPAddress IPAddress, int Port)
        {
            this.SendLock       = new object();

            this.SocketState    = SocketState.Null;

            this.ReceiveLock    = new object();

            this.RemoteEndPoint = new IPEndPoint(IPAddress, Port);

            this.UdpSocket      = Reactor.Udp.Socket.Create();

            this.UdpSocket.Bind(IPAddress.Any, 0);

            this.UdpSocket.OnMessage += (remoteEP, data) => {

                if (remoteEP.ToString() == this.RemoteEndPoint.ToString()) {

                    this.Receive(data);
                }
            };

            this.SenderWindowSize  = 1;

            this.ReceiveWindowSize = 1;

            this.IsServer          = false;            

            this.SetupHandshake();
        }

        #endregion

        #region Transport

        private void SetupHandshake()
        {
            this.SocketState  = SocketState.Connecting;

            this.LastSignal   = DateTime.Now;

            Processor.OnTick += this.ProcessHandshake;

            this.UdpSocket.Send(this.RemoteEndPoint, new HandshakeSyn(RandomNumber.Get()).Serialize());
        }

        private void ProcessHandshake() 
        {
            var delta = DateTime.Now - this.LastSignal;

            if(delta.TotalMilliseconds > Socket.HandshakeTimeout) {

                Processor.OnTick -= this.ProcessHandshake;

                if(this.OnError != null) {

                    Loop.Post(() => {

                        this.OnError(new Exception("Could not connect to remote host at " + this.RemoteEndPoint));
                    });
                }
            }
        }

        private void SetupConnection (uint SequenceNumber, uint AcknowledgementNumber)
        {
            Processor.OnTick        -= this.ProcessHandshake;

            this.SocketState         = SocketState.Established;

            this.SendBuffer          = new SendBuffer    (AcknowledgementNumber, Socket.PacketSize);

            this.ReceiveBuffer       = new ReceiveBuffer (SequenceNumber, Fusion.Socket.DefaultWindowSize);

            this.TimeoutBuffer       = new TimeoutBuffer();

            this.RTTBuffer           = new RTTBuffer(Socket.RTTSamplerSize);

            this.ReceiveWindowSize   = Socket.DefaultWindowSize;

            this.SenderWindowSize    = Socket.DefaultWindowSize;

            Processor.OnTick        += this.ProcessConnection;

            if (this.OnConnect != null) {

                Loop.Post(() => this.OnConnect());
            }
        }

        private void ProcessConnection ()
        {
            lock (this.SendLock) {

                if(this.SocketState == SocketState.Established) {

                    //-----------------------------------------------------
                    // keep alive proceedure
                    //-----------------------------------------------------

                    var delta = DateTime.Now - this.LastSignal;

                    if (delta.TotalMilliseconds > Socket.KeepAliveTimeout) {

                        this.KeepAliveCounter += 1;

                        if(this.KeepAliveCounter < 4) {

                            this.LastSignal = DateTime.Now;

                            this.UdpSocket.Send(this.RemoteEndPoint, new KeepAliveSyn().Serialize());
                   
                        } else {

                            if(this.OnError != null) {

                                Loop.Post(() => {

                                    this.OnError(new Exception("Unable to write to transport."));
                                });
                            }

                            this.Disconnect();
                        }
                    }

                    //-----------------------------------------------------
                    // expire any payloads that exceed the average rtt.
                    //-----------------------------------------------------

                    int timeouts = this.TimeoutBuffer.Timeout(this.RTTBuffer.Average * 4.0);

                    //--------------------------------------------------------------
                    // if timeouts, adjust the sending window size.
                    //--------------------------------------------------------------

                    if (timeouts == 0) {

                        this.SenderWindowSize = this.SenderWindowSize + 1;

                        if (this.SenderWindowSize > this.ReceiveWindowSize)
                        {
                            this.SenderWindowSize = this.ReceiveWindowSize;
                        }
                    }
                    else {

                        this.SenderWindowSize = this.SenderWindowSize / 2;

                        if (this.SenderWindowSize == 0)
                        {
                            this.SenderWindowSize = 1;
                        }
                    }

                    //-----------------------------------------------------
                    // write into the timeout buffer from send buffer.
                    //-----------------------------------------------------

                    foreach (var payload in this.SendBuffer.Read(this.SenderWindowSize)) {

                        this.TimeoutBuffer.Write(payload);
                    }

                    //-----------------------------------------------------
                    // send payloads from the timeout buffer.
                    //-----------------------------------------------------

                    foreach (var payload in this.TimeoutBuffer.Read(this.SenderWindowSize))
                    {
                        this.UdpSocket.Send(this.RemoteEndPoint, payload.Serialize());

                        this.RTTBuffer.Write(payload.SequenceNumber);
                    }
                }
            }
        }

        private void Disconnect        ()
        {
            lock(this.SendLock) {

               Processor.OnTick -= this.ProcessConnection;

               this.SocketState = SocketState.Closed;

               if(this.OnClose != null) {
                    
                    Loop.Post(() => {

                        this.OnClose();
                    });
                }

                if(this.OnEnd != null) {

                    Loop.Post(() => {

                        this.OnEnd();
                    });
                }
            }
        }

        #endregion

        #region Send

        public void Send(byte [] data)
        {
            if(this.SocketState == Fusion.SocketState.Established) {

                this.SendBuffer.Write(data);
            }
        }

        public void End()
        {
            this.SendBuffer.End();
        }

        #endregion

        #region Receive

        internal void Receive(byte [] data)
        {
            lock(this.ReceiveLock)
            {
                MessageType type;

                var message = Deserializer.Deserialize(data, out type);

                switch (type)
                {
                    case MessageType.HandshakeSyn:

                        this.ReceiveHandshakeSyn((HandshakeSyn)message);

                        break;

                    case MessageType.HandshakeSynAck:

                        this.ReceiveHandshakeSynAck((HandshakeSynAck)message);

                        break;

                    case MessageType.HandshakeAck:

                        this.ReceiveHandshakeAck((HandshakeAck)message);

                        break;

                    case MessageType.PayloadSyn:

                        this.ReceivePayloadSyn((PayloadSyn)message);

                        break;

                    case MessageType.PayloadAck:

                        this.ReceivePayloadAck((PayloadAck)message);

                        break;

                    case MessageType.KeepAliveSyn:

                        this.ReceiveKeepAliveSyn((KeepAliveSyn)message);

                        break;

                    case MessageType.KeepAliveAck:

                        this.ReceiveKeepAliveAck((KeepAliveAck)message);

                        break;
                }
            }
        }

        private void ReceiveHandshakeSyn    (HandshakeSyn    handshakeSyn)
        {
            lock(this.ReceiveLock)
            {
                this.SocketState      = SocketState.Connecting;

                this.LastSignal       = DateTime.Now;

                this.KeepAliveCounter = 0;

                this.UdpSocket.Send(this.RemoteEndPoint, new HandshakeSynAck(RandomNumber.Get(), handshakeSyn.SequenceNumber + 1).Serialize());
            }
        }

        private void ReceiveHandshakeSynAck (HandshakeSynAck handshakeSynAck)
        {
            lock(this.ReceiveLock)
            {
                this.LastSignal       = DateTime.Now;

                this.KeepAliveCounter = 0;

                this.UdpSocket.Send(this.RemoteEndPoint, new HandshakeAck( handshakeSynAck.AcknowledgementNumber,  handshakeSynAck.SequenceNumber + 1).Serialize());

                this.SetupConnection(handshakeSynAck.AcknowledgementNumber, handshakeSynAck.SequenceNumber + 1);
            }
        }

        private void ReceiveHandshakeAck    (HandshakeAck    handshakeAck)
        {
            lock(this.ReceiveLock)
            {
                this.LastSignal       = DateTime.Now;

                this.KeepAliveCounter = 0;

                this.SetupConnection(handshakeAck.AcknowledgementNumber, handshakeAck.SequenceNumber);
            }
        }

        private void ReceivePayloadSyn      (PayloadSyn      payloadSyn)
        {
            lock (this.ReceiveLock)
            {
                if(this.SocketState == SocketState.Established) {

                    this.LastSignal = DateTime.Now;

                    this.KeepAliveCounter = 0;

                    //------------------------------------------------------
                    // write payload on recieve buffer.
                    //------------------------------------------------------
                    
                    this.ReceiveBuffer.Write(payloadSyn);

                    //------------------------------------------------------
                    // attempt to dequeue any data.
                    //------------------------------------------------------

                    byte [] data = this.ReceiveBuffer.Dequeue();

                    if(data.Length > 0) {

                        if(this.OnData != null) {

                            Loop.Post(() => {

                                this.OnData(Reactor.Buffer.Create(data));
                            });
                        }
                    }

                    //------------------------------------------------------
                    // if we receive a end on the payload, disconnect.
                    //------------------------------------------------------   
                
                    if (payloadSyn.End == 1) {

                        this.Disconnect();
                    }

                    //------------------------------------------------------
                    // acknowledge sender next sequence number
                    //------------------------------------------------------

                    this.UdpSocket.Send(this.RemoteEndPoint, new PayloadAck(this.ReceiveBuffer.SequenceNumber, this.ReceiveBuffer.WindowSize, payloadSyn.End).Serialize());
                }
            }
        }

        private void ReceivePayloadAck      (PayloadAck      payloadAck)
        {
            lock(this.SendLock) {

                if(this.SocketState == SocketState.Established) {

                    if(payloadAck != null) {

                        this.LastSignal = DateTime.Now;

                        this.KeepAliveCounter = 0;

                        //--------------------------------------------
                        // call acknowledge on send buffer
                        //--------------------------------------------

                        this.SendBuffer.Acknowledge (payloadAck.AcknowledgementNumber);

                        //--------------------------------------------
                        // call acknowledge on timeout buffer
                        //--------------------------------------------

                        this.TimeoutBuffer.Acknowledge(payloadAck.AcknowledgementNumber);

                        //--------------------------------------------
                        // call acknowledge on rtt buffer
                        //--------------------------------------------

                        this.RTTBuffer.Acknowledge  (payloadAck.AcknowledgementNumber - 1);
                
                        //--------------------------------------------
                        // set the reciever sender size
                        //--------------------------------------------

                        this.ReceiveWindowSize = payloadAck.WindowSize;

                        //--------------------------------------------
                        // we have a disconnect
                        //--------------------------------------------

                        //Console.WriteLine(payloadAck.AcknowledgementNumber);

                        if (payloadAck.End == 1)
                        {
                            this.Disconnect();
                        }
                    }
                }
            }
        }

        private void ReceiveKeepAliveSyn    (KeepAliveSyn    keepAliveSyn)
        {
            lock(this.ReceiveLock)
            {
                if(this.SocketState == SocketState.Established)
                {
                    this.LastSignal      = DateTime.Now;

                    this.KeepAliveCounter = 0;

                    this.UdpSocket.Send(this.RemoteEndPoint, new KeepAliveAck().Serialize());
                }
            }
        }

        private void ReceiveKeepAliveAck    (KeepAliveAck    keepAliveAck)
        {
            lock(this.ReceiveLock) {

                if(this.SocketState == SocketState.Established)
                {
                    this.LastSignal       = DateTime.Now;

                    this.KeepAliveCounter = 0;
                }
            }
        }
        
        #endregion

        #region Statics

        public static Socket Create(int port)
        {
            return new Socket(IPAddress.Loopback, port);
        }

        public static Socket Create(IPAddress IPAddress, int Port)
        {
            return new Socket(IPAddress, Port);
        }

        public static Socket Create(string Hostname, int Port)
        {
            return new Socket(Hostname, Port);
        }

        #endregion
    }
}
