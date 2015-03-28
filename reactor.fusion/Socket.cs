/*--------------------------------------------------------------------------

Reactor.Fusion

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
using Reactor.Fusion.Protocol;
using Reactor.Fusion.Queues;

namespace Reactor.Fusion
{
    public class Socket
    {
        //-------------------------------------------------------------
        // statics
        //-------------------------------------------------------------

        public static int packet_size = 1400;

        //-------------------------------------------------------------
        // events
        //-------------------------------------------------------------

        public event Reactor.Action             OnConnect;

        public event Action<Reactor.Buffer>     OnData;

        public event Action<Exception>          OnError;

        public event Action                     OnEnd;

        //----------------------------------
        // state
        //----------------------------------

        private Reactor.Dgram.Socket              socket;

        private System.Net.EndPoint             endpoint;

        private bool                            sending;

        //----------------------------------
        // queues
        //----------------------------------

        private ByteQueue                       byte_queue;

        private SendQueue                       send_queue;

        private RecvQueue                       recv_queue;

        public Socket(Reactor.Dgram.Socket socket, System.Net.EndPoint endpoint)
        {
            this.OnConnect += ()    => { };

            this.OnData    += data  => { };

            this.OnError   += error => { };

            this.OnEnd     += ()    => { };

            this.sending    = false;

            this.socket     = socket;

            this.endpoint   = endpoint;
        }

        public Socket(System.Net.IPAddress address, int port)
        {
            this.OnConnect += ()    => { };

            this.OnData    += data  => { };

            this.OnError   += error => { };

            this.OnEnd     += ()    => { };

            this.sending    = false;

            this.endpoint   = new System.Net.IPEndPoint(address, port);

            this.socket     = Reactor.Dgram.Socket.Create();

            this.socket.Bind(System.Net.IPAddress.Any, 0);

            this.socket.OnMessage += (remote, data) => {

                if (remote.ToString() == this.endpoint.ToString()) {

                    this.Receive(data);
                }
            };

            var syn = new Syn(Random.Get());

            this.socket.Send(this.endpoint, syn.Serialize());
        }

        private void Setup  (uint sequenceNumber, uint acknowlegementNumber)
        {
            this.send_queue  = new SendQueue(acknowlegementNumber, Socket.packet_size);

            this.recv_queue  = new RecvQueue(sequenceNumber, 1);

            this.byte_queue  = new ByteQueue();

            this.OnConnect();
        }
        
        public  void Send   (byte[] data)
        {
            this.send_queue.Write(data);

            if (!sending) {

                this.sending = true;

                foreach (var payload in this.send_queue.Read(1)) {

                    this.socket.Send(this.endpoint, payload.Serialize());
                }
            }
        }

        public  void End    ()
        {
            this.send_queue.End();

            if (!sending) {

                this.sending = true;

                foreach (var payload in this.send_queue.Read(1))
                {
                    this.socket.Send(this.endpoint, payload.Serialize());
                }
            }
        }

        #region Receive

        internal void Receive             (byte []      data)         
        {
            PacketType packetType;

            var packet = Parser.Deserialize(data, out packetType);

            switch (packetType)
            {
                case PacketType.Syn:

                    this.ReceiveSyn(packet as Syn);

                    break;

                case PacketType.SynAck:

                    this.ReceiveSynAck(packet as SynAck);

                    break;

                case PacketType.Ack:

                    this.ReceiveAck(packet as Ack);

                    break;

                case PacketType.PayloadSyn:

                    this.ReceiveDataSyn(packet as DataSyn);

                    break;

                case PacketType.PayloadAck:

                    this.ReceiveDataAck(packet as DataAck);

                    break;

                case PacketType.FinSyn:

                    this.ReceiveFinSyn(packet as FinSyn);

                    break;

                case PacketType.FinAck:

                    this.ReceiveFinAck(packet as FinAck);

                    break;
            }
        }

        internal void ReceiveSyn          (Syn          syn)          
        {
            var synack = new SynAck(Random.Get(), syn.SequenceNumber + 1);

            this.socket.Send(this.endpoint, synack.Serialize());
        }

        internal void ReceiveSynAck       (SynAck       synack)       
        {
            this.socket.Send(this.endpoint, new Ack(synack.AcknowledgementNumber, synack.SequenceNumber + 1).Serialize());

            this.Setup (synack.AcknowledgementNumber, synack.SequenceNumber + 1);
        }

        internal void ReceiveAck          (Ack          ack)          
        {
            this.Setup(ack.AcknowledgementNumber, ack.SequenceNumber);
        }

        internal void ReceiveDataSyn      (DataSyn      syn)   
        {
            this.recv_queue.Write(syn);

            var data = this.recv_queue.Dequeue();

            this.OnData(Reactor.Buffer.Create(data));

            var ack = new DataAck(this.recv_queue.SequenceNumber, this.recv_queue.WindowSize);

            this.socket.Send(this.endpoint, ack.Serialize());
        }

        internal void ReceiveDataAck   (DataAck       ack)   
        {
            this.send_queue.Acknowledge(ack.AcknowledgementNumber);

            this.sending = false;

            foreach (var payload in this.send_queue.Read(1)) {

                this.socket.Send(this.endpoint, payload.Serialize());
            }
        }

        internal void ReceiveFinSyn  (FinSyn       syn) 
        {
            var ack = new FinAck(this.recv_queue.SequenceNumber, 0);

            this.socket.Send(this.endpoint, ack.Serialize());

            this.OnEnd();
        }

        internal void ReceiveFinAck  (FinAck       ack) 
        {
            this.OnEnd();
        }

        #endregion

        #region Statics

        public static Socket Create(int port)
        {
            return new Socket(System.Net.IPAddress.Loopback, port);
        }

        public static Socket Create(System.Net.IPAddress address, int port)
        {
            return new Socket(address, port);
        }

        #endregion
    }
}
