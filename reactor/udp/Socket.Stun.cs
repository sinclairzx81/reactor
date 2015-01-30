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
using System.Text;

namespace Reactor.Udp
{
    #region ChangeRequest

    internal class ChangeRequest
    {
        public bool ChangeIP { get; set; }

        public bool ChangePort { get; set; }

        public ChangeRequest() : this(true, true)
        {

        }

        public ChangeRequest(bool changeIP, bool changePort)
        {
            ChangeIP = changeIP;

            ChangePort = changePort;
        }
    }

    #endregion

    #region ErrorCode

    internal class ErrorCode
    {
        public int Code { get; set; }

        public string ReasonText { get; set; }

        public ErrorCode()
            : this(0, string.Empty)
        {

        }

        public ErrorCode(int code, string reasonText)
        {
            this.Code = code;

            this.ReasonText = reasonText;
        }
    }

    #endregion

    #region PacketType

    /// <summary>
    /// This enum specifies STUN message type.
    /// </summary>
    internal enum PacketType
    {
        /// <summary>
        /// STUN message is binding request.
        /// </summary>
        BindingRequest = 0x0001,

        /// <summary>
        /// STUN message is binding request response.
        /// </summary>
        BindingResponse = 0x0101,

        /// <summary>
        /// STUN message is binding requesr error response.
        /// </summary>
        BindingErrorResponse = 0x0111,

        /// <summary>
        /// STUN message is "shared secret" request.
        /// </summary>
        SharedSecretRequest = 0x0002,

        /// <summary>
        /// STUN message is "shared secret" request response.
        /// </summary>
        SharedSecretResponse = 0x0102,

        /// <summary>
        /// STUN message is "shared secret" request error response.
        /// </summary>
        SharedSecretErrorResponse = 0x0112,
    }

    #endregion

    #region Stun Packet

    internal class StunPacket
    {
        private enum AttributeType
        {
            MappedAddress = 0x0001,

            ResponseAddress = 0x0002,

            ChangeRequest = 0x0003,

            SourceAddress = 0x0004,

            ChangedAddress = 0x0005,

            Username = 0x0006,

            Password = 0x0007,

            MessageIntegrity = 0x0008,

            ErrorCode = 0x0009,

            UnknownAttribute = 0x000A,

            ReflectedFrom = 0x000B,

            XorMappedAddress = 0x8020,

            XorOnly = 0x0021,

            ServerName = 0x8022,
        }

        private enum IPFamily
        {
            IPv4 = 0x01,

            IPv6 = 0x02,
        }



        public PacketType Type = PacketType.BindingRequest;

        public Guid TransactionID = Guid.Empty;

        public IPEndPoint MappedAddress = null;

        public IPEndPoint ResponseAddress = null;

        public ChangeRequest ChangeRequest = null;

        public IPEndPoint SourceAddress = null;

        public IPEndPoint ChangedAddress = null;

        public string UserName = null;

        public string Password = null;

        public ErrorCode ErrorCode = null;

        public IPEndPoint ReflectedFrom = null;

        public string ServerName = null;

        public StunPacket()
        {
            TransactionID = Guid.NewGuid();
        }

        public void Parse(byte[] data)
        {
            if (data.Length < 20)
            {
                throw new ArgumentException("Invalid STUN message value !");
            }

            int offset = 0;

            // STUN Message Type
            int messageType = (data[offset++] << 8 | data[offset++]);

            if (messageType == (int)PacketType.BindingErrorResponse)
            {
                Type = PacketType.BindingErrorResponse;
            }
            else if (messageType == (int)PacketType.BindingRequest)
            {
                Type = PacketType.BindingRequest;
            }
            else if (messageType == (int)PacketType.BindingResponse)
            {
                Type = PacketType.BindingResponse;
            }
            else if (messageType == (int)PacketType.SharedSecretErrorResponse)
            {
                Type = PacketType.SharedSecretErrorResponse;
            }
            else if (messageType == (int)PacketType.SharedSecretRequest)
            {
                Type = PacketType.SharedSecretRequest;
            }
            else if (messageType == (int)PacketType.SharedSecretResponse)
            {
                Type = PacketType.SharedSecretResponse;
            }
            else
            {
                throw new ArgumentException("Invalid STUN message type value !");
            }

            // Message Length
            int messageLength = (data[offset++] << 8 | data[offset++]);

            // Transaction ID
            byte[] guid = new byte[16];

            Array.Copy(data, offset, guid, 0, 16);

            TransactionID = new Guid(guid);

            offset += 16;

            while ((offset - 20) < messageLength)
            {
                ParseAttribute(data, ref offset);
            }
        }

        public byte[] ToByteData()
        {
            byte[] msg = new byte[512];

            int offset = 0;

            msg[offset++] = (byte)((int)this.Type >> 8);

            msg[offset++] = (byte)((int)this.Type & 0xFF);

            msg[offset++] = 0;

            msg[offset++] = 0;

            Array.Copy(TransactionID.ToByteArray(), 0, msg, offset, 16);

            offset += 16;

            if (this.MappedAddress != null)
            {
                StoreEndPoint(AttributeType.MappedAddress, this.MappedAddress, msg, ref offset);
            }
            else if (this.ResponseAddress != null)
            {
                StoreEndPoint(AttributeType.ResponseAddress, this.ResponseAddress, msg, ref offset);
            }
            else if (this.ChangeRequest != null)
            {
                // Attribute header
                msg[offset++] = (int)AttributeType.ChangeRequest >> 8;

                msg[offset++] = (int)AttributeType.ChangeRequest & 0xFF;

                msg[offset++] = 0;

                msg[offset++] = 4;

                msg[offset++] = 0;

                msg[offset++] = 0;

                msg[offset++] = 0;

                msg[offset++] = (byte)(Convert.ToInt32(this.ChangeRequest.ChangeIP) << 2 | Convert.ToInt32(this.ChangeRequest.ChangePort) << 1);
            }
            else if (this.SourceAddress != null)
            {
                StoreEndPoint(AttributeType.SourceAddress, this.SourceAddress, msg, ref offset);
            }
            else if (this.ChangedAddress != null)
            {
                StoreEndPoint(AttributeType.ChangedAddress, this.ChangedAddress, msg, ref offset);
            }
            else if (this.UserName != null)
            {
                byte[] userBytes = Encoding.ASCII.GetBytes(this.UserName);

                msg[offset++] = (int)AttributeType.Username >> 8;

                msg[offset++] = (int)AttributeType.Username & 0xFF;

                msg[offset++] = (byte)(userBytes.Length >> 8);

                msg[offset++] = (byte)(userBytes.Length & 0xFF);

                Array.Copy(userBytes, 0, msg, offset, userBytes.Length);

                offset += userBytes.Length;
            }
            else if (this.Password != null)
            {
                byte[] userBytes = Encoding.ASCII.GetBytes(this.UserName);

                msg[offset++] = (int)AttributeType.Password >> 8;

                msg[offset++] = (int)AttributeType.Password & 0xFF;

                msg[offset++] = (byte)(userBytes.Length >> 8);

                msg[offset++] = (byte)(userBytes.Length & 0xFF);

                Array.Copy(userBytes, 0, msg, offset, userBytes.Length);

                offset += userBytes.Length;
            }
            else if (this.ErrorCode != null)
            {
                byte[] reasonBytes = Encoding.ASCII.GetBytes(this.ErrorCode.ReasonText);

                // Header
                msg[offset++] = 0;

                msg[offset++] = (int)AttributeType.ErrorCode;

                msg[offset++] = 0;

                msg[offset++] = (byte)(4 + reasonBytes.Length);

                // Empty
                msg[offset++] = 0;

                msg[offset++] = 0;

                // Class
                msg[offset++] = (byte)Math.Floor((double)(this.ErrorCode.Code / 100));

                // Number
                msg[offset++] = (byte)(this.ErrorCode.Code & 0xFF);

                // ReasonPhrase
                Array.Copy(reasonBytes, msg, reasonBytes.Length);

                offset += reasonBytes.Length;
            }
            else if (this.ReflectedFrom != null)
            {
                StoreEndPoint(AttributeType.ReflectedFrom, this.ReflectedFrom, msg, ref offset);
            }

            // Update Message Length. NOTE: 20 bytes header not included.

            msg[2] = (byte)((offset - 20) >> 8);

            msg[3] = (byte)((offset - 20) & 0xFF);

            // Make reatval with actual size.

            byte[] retVal = new byte[offset];

            Array.Copy(msg, retVal, retVal.Length);

            return retVal;
        }

        private void ParseAttribute(byte[] data, ref int offset)
        {
            // Type
            AttributeType type = (AttributeType)(data[offset++] << 8 | data[offset++]);

            // Length
            int length = (data[offset++] << 8 | data[offset++]);

            // MAPPED-ADDRESS
            if (type == AttributeType.MappedAddress)
            {
                MappedAddress = ParseEndPoint(data, ref offset);
            }
            // RESPONSE-ADDRESS
            else if (type == AttributeType.ResponseAddress)
            {
                ResponseAddress = ParseEndPoint(data, ref offset);
            }

            // CHANGE-REQUEST
            else if (type == AttributeType.ChangeRequest)
            {
                offset += 3;

                ChangeRequest = new ChangeRequest((data[offset] & 4) != 0, (data[offset] & 2) != 0);

                offset++;
            }
            // SOURCE-ADDRESS
            else if (type == AttributeType.SourceAddress)
            {
                SourceAddress = ParseEndPoint(data, ref offset);
            }
            // CHANGED-ADDRESS
            else if (type == AttributeType.ChangedAddress)
            {
                ChangedAddress = ParseEndPoint(data, ref offset);
            }
            // USERNAME
            else if (type == AttributeType.Username)
            {
                UserName = Encoding.Default.GetString(data, offset, length);

                offset += length;
            }
            // PASSWORD
            else if (type == AttributeType.Password)
            {
                Password = Encoding.Default.GetString(data, offset, length);

                offset += length;
            }
            // MESSAGE-INTEGRITY
            else if (type == AttributeType.MessageIntegrity)
            {
                offset += length;
            }

            else if (type == AttributeType.ErrorCode)
            {
                int errorCode = (data[offset + 2] & 0x7) * 100 + (data[offset + 3] & 0xFF);

                ErrorCode = new ErrorCode(errorCode, Encoding.Default.GetString(data, offset + 4, length - 4));

                offset += length;
            }

            else if (type == AttributeType.UnknownAttribute)
            {
                offset += length;
            }

            else if (type == AttributeType.ReflectedFrom)
            {
                ReflectedFrom = ParseEndPoint(data, ref offset);
            }

            // XorMappedAddress
            // XorOnly
            // ServerName
            else if (type == AttributeType.ServerName)
            {
                ServerName = Encoding.Default.GetString(data, offset, length);

                offset += length;
            }
            // Unknown
            else
            {
                offset += length;
            }
        }

        private IPEndPoint ParseEndPoint(byte[] data, ref int offset)
        {
            offset++;

            offset++;

            // Port
            int port = (data[offset++] << 8 | data[offset++]);

            // Address
            byte[] ip = new byte[4];

            ip[0] = data[offset++];

            ip[1] = data[offset++];

            ip[2] = data[offset++];

            ip[3] = data[offset++];

            return new IPEndPoint(new IPAddress(ip), port);
        }

        private void StoreEndPoint(AttributeType type, IPEndPoint endPoint, byte[] message, ref int offset)
        {
            // Header
            message[offset++] = (byte)((int)type >> 8);

            message[offset++] = (byte)((int)type & 0xFF);

            message[offset++] = 0;

            message[offset++] = 8;

            // Unused
            message[offset++] = 0;

            // Family
            message[offset++] = (byte)IPFamily.IPv4;

            // Port
            message[offset++] = (byte)(endPoint.Port >> 8);

            message[offset++] = (byte)(endPoint.Port & 0xFF);

            // Address
            byte[] ipBytes = endPoint.Address.GetAddressBytes();

            message[offset++] = ipBytes[0];

            message[offset++] = ipBytes[0];

            message[offset++] = ipBytes[0];

            message[offset++] = ipBytes[0];
        }
    }

    #endregion

    public partial class Socket
    {
        #region NatType

        /// <summary>
        /// Specifies UDP network type.
        /// </summary>
        public enum NatType
        {
            /// <summary>
            /// UDP is always blocked.
            /// </summary>
            UdpBlocked,

            /// <summary>
            /// No NAT, public IP, no firewall.
            /// </summary>
            OpenInternet,

            /// <summary>
            /// No NAT, public IP, but symmetric UDP firewall.
            /// </summary>
            SymmetricUdpFirewall,

            /// <summary>
            /// A full cone NAT is one where all requests from the same internal IP address and port are
            /// mapped to the same external IP address and port. Furthermore, any external host can send
            /// a packet to the internal host, by sending a packet to the mapped external address.
            /// </summary>
            FullCone,

            /// <summary>
            /// A restricted cone NAT is one where all requests from the same internal IP address and
            /// port are mapped to the same external IP address and port. Unlike a full cone NAT, an external
            /// host (with IP address X) can send a packet to the internal host only if the internal host
            /// had previously sent a packet to IP address X.
            /// </summary>
            RestrictedCone,

            /// <summary>
            /// A port restricted cone NAT is like a restricted cone NAT, but the restriction
            /// includes port numbers. Specifically, an external host can send a packet, with source IP
            /// address X and source port P, to the internal host only if the internal host had previously
            /// sent a packet to IP address X and port P.
            /// </summary>
            PortRestrictedCone,

            /// <summary>
            /// A symmetric NAT is one where all requests from the same internal IP address and port,
            /// to a specific destination IP address and port, are mapped to the same external IP address and
            /// port. If the same host sends a packet with the same source address and port, but to
            /// a different destination, a different mapping is used. Furthermore, only the external host that
            /// receives a packet can send a UDP packet back to the internal host.
            /// </summary>
            Symmetric
        }

        #endregion

        #region StunResponse

        public class StunResponse
        {
            public NatType NatType { get; set; }

            public IPEndPoint PublicEndPoint { get; set; }

            public StunResponse() : this(NatType.OpenInternet, null)
            {

            }

            public StunResponse(NatType netType, IPEndPoint publicEndPoint)
            {
                this.NatType = netType;

                this.PublicEndPoint = publicEndPoint;
            }
        }

        #endregion

        /// <summary>
        /// Pings a stun server and obtains a limited time public ip and port for this UDP socket.
        /// </summary>
        /// <param name="Host">The IP address of the Stun Server.</param>
        /// <param name="Port">The port of the Stun Server.</param>
        /// <param name="callback">A callback with the Stun response.</param>
        public void Stun(string Host, int Port, Action<Exception, StunResponse> callback)
        {
            //---------------------------------------------------------------- 
            // save any events on this socket.
            //---------------------------------------------------------------- 

            var actions = new List<Action<EndPoint, byte[]>>();

            if(this.OnMessage != null) {

                foreach(var _action in this.OnMessage.GetInvocationList()) {

                    var action = (Action<EndPoint, byte[]>)_action;

                    actions.Add(action);

                    this.OnMessage -= action;
                }
            }

            //---------------------------------------------------------------- 
            // resolve stun endpoint.
            //----------------------------------------------------------------

            Reactor.Net.Dns.GetHostAddresses(Host, (exception0, addresses) => {

                if (exception0 != null)
                {
                    callback(exception0, null);

                    return;
                }


                var remoteEP     = new IPEndPoint(addresses[0], Port);
                
                StunPacket test1 = new StunPacket();

                test1.Type       = PacketType.BindingRequest;

                this.StunRequest(remoteEP, test1, (test1response) =>
                {
                    if (test1response == null)
                    {
                        foreach(var action in actions) {
                                
                            this.OnMessage += action;
                        }

                        callback(null, new StunResponse(NatType.UdpBlocked, null)); 
                    }
                    else
                    {
                        if (this.LocalEndPoint.Equals(test1response.MappedAddress))
                        {
                            StunPacket test2    = new StunPacket();

                            test2.Type          = PacketType.BindingRequest;

                            test2.ChangeRequest = new ChangeRequest(true, true);

                            this.StunRequest(remoteEP, test2, (test2response) =>
                            {
                                if (test2response == null)
                                {
                                    foreach(var action in actions) {
                                
                                        this.OnMessage += action;
                                    }

                                    callback(null, new StunResponse(NatType.SymmetricUdpFirewall, test1response.MappedAddress));
                       
                                }
                                else
                                {
                                    foreach(var action in actions) {
                                
                                        this.OnMessage += action;
                                    }

                                    callback(null, new StunResponse(NatType.OpenInternet, test1response.MappedAddress));
                                }
                            });
                        }
                        else
                        {
                            StunPacket test2 = new StunPacket();

                            test2.Type = PacketType.BindingRequest;

                            test2.ChangeRequest = new ChangeRequest(true, true);

                            this.StunRequest(remoteEP, test2, (test2response) =>
                            {
                                if (test2response != null)
                                {
                                    foreach(var action in actions) {
                                
                                        this.OnMessage += action;
                                    }

                                    callback(null, new StunResponse(NatType.FullCone, test1response.MappedAddress));
                                }
                                else
                                {
                                    // Test I(II)
                                    StunPacket test12 = new StunPacket();

                                    test12.Type = PacketType.BindingRequest;

                                    this.StunRequest(remoteEP, test12, (test12response) =>
                                    {
                                        if (test12response == null)
                                        {
                                            foreach(var action in actions) {
                                
                                                this.OnMessage += action;
                                            }

                                            callback(null, new StunResponse(NatType.UdpBlocked, null));
                                        }
                                        else
                                        {
                                            StunPacket test3 = new StunPacket();

                                            test3.Type = PacketType.BindingRequest;

                                            test3.ChangeRequest = new ChangeRequest(false, true);

                                            this.StunRequest(remoteEP, test3, (test3response) =>
                                            {
                                                if (test3response == null)
                                                {
                                                    foreach(var action in actions) {
                                
                                                        this.OnMessage += action;
                                                    }

                                                    callback(null, new StunResponse(NatType.PortRestrictedCone, test1response.MappedAddress));
                                                }
                                                else
                                                {
                                                    foreach(var action in actions) {
                                
                                                        this.OnMessage += action;
                                                    }

                                                    callback(null, new StunResponse(NatType.RestrictedCone, test1response.MappedAddress));
                                                }
                                            });
                                        }
                                    });
                                }
                            });
                        }
                    }
                });
            });
        }

        private void StunRequest(EndPoint stunEndPoint, StunPacket packet, Action<StunPacket> callback)
        {
            bool hasresponded = false;

            Action<EndPoint, byte[]> action = null;

            //---------------------------------------
            // the stun response handler.
            //---------------------------------------

            action = new Action<EndPoint, byte[]>((endpoint, data) => {

                //---------------------------------------------
                // here, we try and catch any parse errors, which
                // may originate from other things attempting to
                // send data to this socket.
                //---------------------------------------------
                
                try {

                    StunPacket response = new StunPacket();

                    response.Parse(data);

                    this.OnMessage -= action;

                    hasresponded    = true;

                    callback(response);
                }
                catch {
                    
                }
            });

            //---------------------------------------
            // setup timeout in case of no response.
            //---------------------------------------
            
            Reactor.Timeout.Create(() => {

                if (!hasresponded) {

                    this.OnMessage -= action;

                    callback(null);
                }

            }, 2000);

            //---------------------------------------
            // set on response, make request.
            //---------------------------------------

            this.OnMessage += action;

            this.Send(stunEndPoint, packet.ToByteData());
        }
    }
}
