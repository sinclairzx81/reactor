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
using System.Text;

namespace Reactor.Udp.Stun {

    /// <summary>
    /// Stun Error Code.
    /// </summary>
    internal class ErrorCode {
        public int    Code   { get; set; }
        public string Reason { get; set; }

        public ErrorCode(int code, string reason) {
            this.Code    = code;
            this.Reason = reason;
        }

        public ErrorCode() : this(0, string.Empty) { }
    }

    /// <summary>
    /// Stun Change Request
    /// </summary>
    internal class ChangeRequest {
        public bool ChangeIP   { get; set; }
        public bool ChangePort { get; set; }

        public ChangeRequest(bool change_ip, bool change_port) {
            this.ChangeIP   = change_ip;
            this.ChangePort = change_port;
        }

        public ChangeRequest() : this(true, true) { }
    }

    /// <summary>
    /// This enum specifies STUN message type.
    /// </summary>
    internal enum PacketType {
        /// <summary>
        /// STUN message is binding request.
        /// </summary>
        BindingRequest            = 0x0001,
        /// <summary>
        /// STUN message is binding request response.
        /// </summary>
        BindingResponse           = 0x0101,
        /// <summary>
        /// STUN message is binding requesr error response.
        /// </summary>
        BindingErrorResponse      = 0x0111,
        /// <summary>
        /// STUN message is "shared secret" request.
        /// </summary>
        SharedSecretRequest       = 0x0002,
        /// <summary>
        /// STUN message is "shared secret" request response.
        /// </summary>
        SharedSecretResponse      = 0x0102,
        /// <summary>
        /// STUN message is "shared secret" request error response.
        /// </summary>
        SharedSecretErrorResponse = 0x0112,
    }

    internal class Packet {
        private enum AttributeType {
            MappedAddress    = 0x0001,
            ResponseAddress  = 0x0002,
            ChangeRequest    = 0x0003,
            SourceAddress    = 0x0004,
            ChangedAddress   = 0x0005,
            Username         = 0x0006,
            Password         = 0x0007,
            MessageIntegrity = 0x0008,
            ErrorCode        = 0x0009,
            UnknownAttribute = 0x000A,
            ReflectedFrom    = 0x000B,
            XorMappedAddress = 0x8020,
            XorOnly          = 0x0021,
            ServerName       = 0x8022,
        }

        private enum IPFamily {
            IPv4 = 0x01,
            IPv6 = 0x02,
        }

        public PacketType    Type = PacketType.BindingRequest;
        public Guid          TransactionID   = Guid.Empty;
        public IPEndPoint    MappedAddress   = null;
        public IPEndPoint    ResponseAddress = null;
        public ChangeRequest ChangeRequest   = null;
        public IPEndPoint    SourceAddress   = null;
        public IPEndPoint    ChangedAddress  = null;
        public string        UserName        = null;
        public string        Password        = null;
        public ErrorCode     ErrorCode       = null;
        public IPEndPoint    ReflectedFrom   = null;
        public string        ServerName      = null;

        public Packet() {
            TransactionID = Guid.NewGuid();
        }

        public void Parse(byte[] data) {
            if (data.Length < 20) {
                throw new ArgumentException("Invalid STUN message value !");
            }

            int offset = 0;
            /* stun message type */
            int messageType = (data[offset++] << 8 | data[offset++]);
            if (messageType == (int)PacketType.BindingErrorResponse) {
                Type = PacketType.BindingErrorResponse;
            }
            else if (messageType == (int)PacketType.BindingRequest) {
                Type = PacketType.BindingRequest;
            }
            else if (messageType == (int)PacketType.BindingResponse) {
                Type = PacketType.BindingResponse;
            }
            else if (messageType == (int)PacketType.SharedSecretErrorResponse) {
                Type = PacketType.SharedSecretErrorResponse;
            }
            else if (messageType == (int)PacketType.SharedSecretRequest) {
                Type = PacketType.SharedSecretRequest;
            }
            else if (messageType == (int)PacketType.SharedSecretResponse) {
                Type = PacketType.SharedSecretResponse;
            }
            else {
                throw new ArgumentException("Invalid STUN message type value !");
            }

            /* message length */
            int messageLength = (data[offset++] << 8 | data[offset++]);

            /* transaction id */
            byte[] guid = new byte[16];
            Array.Copy(data, offset, guid, 0, 16);
            TransactionID = new Guid(guid);
            offset += 16;
            while ((offset - 20) < messageLength) {
                ParseAttribute(data, ref offset);
            }
        }

        public byte[] ToArray() {
            var message = new byte[512];
            var offset = 0;
            message[offset++] = (byte)((int)this.Type >> 8);
            message[offset++] = (byte)((int)this.Type & 0xFF);
            message[offset++] = 0;
            message[offset++] = 0;
            System.Buffer.BlockCopy(TransactionID.ToByteArray(), 0, message, offset, 16);
            offset += 16;
            if (this.MappedAddress != null) {
                StoreEndPoint(AttributeType.MappedAddress, this.MappedAddress, message, ref offset);
            }
            else if (this.ResponseAddress != null) {
                StoreEndPoint(AttributeType.ResponseAddress, this.ResponseAddress, message, ref offset);
            }
            else if (this.ChangeRequest != null) {
                /* attribute header */
                message[offset++] = (int)AttributeType.ChangeRequest >> 8;
                message[offset++] = (int)AttributeType.ChangeRequest & 0xFF;
                message[offset++] = 0;
                message[offset++] = 4;
                message[offset++] = 0;
                message[offset++] = 0;
                message[offset++] = 0;
                message[offset++] = (byte)(Convert.ToInt32(this.ChangeRequest.ChangeIP) << 2 | Convert.ToInt32(this.ChangeRequest.ChangePort) << 1);
            }
            else if (this.SourceAddress != null) {
                StoreEndPoint(AttributeType.SourceAddress, this.SourceAddress, message, ref offset);
            }
            else if (this.ChangedAddress != null) {
                StoreEndPoint(AttributeType.ChangedAddress, this.ChangedAddress, message, ref offset);
            }
            else if (this.UserName != null) {
                var userBytes = Encoding.ASCII.GetBytes(this.UserName);
                message[offset++] = (int)AttributeType.Username >> 8;
                message[offset++] = (int)AttributeType.Username & 0xFF;
                message[offset++] = (byte)(userBytes.Length >> 8);
                message[offset++] = (byte)(userBytes.Length & 0xFF);
                Array.Copy(userBytes, 0, message, offset, userBytes.Length);
                offset += userBytes.Length;
            }
            else if (this.Password != null) {
                byte[] userBytes = Encoding.ASCII.GetBytes(this.UserName);
                message[offset++] = (int)AttributeType.Password >> 8;
                message[offset++] = (int)AttributeType.Password & 0xFF;
                message[offset++] = (byte)(userBytes.Length >> 8);
                message[offset++] = (byte)(userBytes.Length & 0xFF);
                Array.Copy(userBytes, 0, message, offset, userBytes.Length);
                offset += userBytes.Length;
            }
            else if (this.ErrorCode != null) {
                byte[] reasonBytes = Encoding.ASCII.GetBytes(this.ErrorCode.Reason);
                /* header */
                message[offset++] = 0;
                message[offset++] = (int)AttributeType.ErrorCode;
                message[offset++] = 0;
                message[offset++] = (byte)(4 + reasonBytes.Length);
                
                /* empty */
                message[offset++] = 0;
                message[offset++] = 0;
                
                /* class */
                message[offset++] = (byte)Math.Floor((double)(this.ErrorCode.Code / 100));
                
                /* number */
                message[offset++] = (byte)(this.ErrorCode.Code & 0xFF);
                
                /* reason-phase */
                System.Array.Copy(reasonBytes, message, reasonBytes.Length);
                offset += reasonBytes.Length;
            }
            else if (this.ReflectedFrom != null) {
                StoreEndPoint(AttributeType.ReflectedFrom, this.ReflectedFrom, message, ref offset);
            }

            /* update message length: note: 20 byte header not included. */
            message[2] = (byte)((offset - 20) >> 8);
            message[3] = (byte)((offset - 20) & 0xFF);

            byte[] retVal = new byte[offset];
            Array.Copy(message, retVal, retVal.Length);
            return retVal;
        }

        private void ParseAttribute(byte[] data, ref int offset) {

            /* type */
            AttributeType type = (AttributeType)(data[offset++] << 8 | data[offset++]);

            /* length */
            int length = (data[offset++] << 8 | data[offset++]);

            /* mapped address */
            if (type == AttributeType.MappedAddress) {
                MappedAddress = ParseEndPoint(data, ref offset);
            }

            /* response address */
            else if (type == AttributeType.ResponseAddress) {
                ResponseAddress = ParseEndPoint(data, ref offset);
            }
            /* change address */
            else if (type == AttributeType.ChangeRequest) {
                offset += 3;
                ChangeRequest = new ChangeRequest((data[offset] & 4) != 0, (data[offset] & 2) != 0);
                offset++;
            }

            /* source address */
            else if (type == AttributeType.SourceAddress) {
                SourceAddress = ParseEndPoint(data, ref offset);
            }
            /* changed address */
            else if (type == AttributeType.ChangedAddress) {
                ChangedAddress = ParseEndPoint(data, ref offset);
            }

            /* username */
            else if (type == AttributeType.Username) {
                UserName = Encoding.Default.GetString(data, offset, length);
                offset += length;
            }

            /* password */
            else if (type == AttributeType.Password) {
                Password = Encoding.Default.GetString(data, offset, length);
                offset += length;
            }
            /* message integrity */
            else if (type == AttributeType.MessageIntegrity) {
                offset += length;
            }
            /* error code */
            else if (type == AttributeType.ErrorCode) {
                int errorCode = (data[offset + 2] & 0x7) * 100 + (data[offset + 3] & 0xFF);
                ErrorCode = new ErrorCode(errorCode, Encoding.Default.GetString(data, offset + 4, length - 4));
                offset += length;
            }
            /* unknown attribute */
            else if (type == AttributeType.UnknownAttribute) {
                offset += length;
            }
            /* reflected from */
            else if (type == AttributeType.ReflectedFrom) {

                ReflectedFrom = ParseEndPoint(data, ref offset);
            }
            /*
               XorMappedAddress
               XorOnly
               ServerName
            */
            else if (type == AttributeType.ServerName) {
                ServerName = Encoding.Default.GetString(data, offset, length);
                offset += length;
            }
            /* unknown */
            else {
                offset += length;
            }
        }

        private IPEndPoint ParseEndPoint(byte[] data, ref int offset) {
            offset++;
            offset++;
            /* port */
            var port = (data[offset++] << 8 | data[offset++]);

            /* address */
            var ip = new byte[4];
            ip[0] = data[offset++];
            ip[1] = data[offset++];
            ip[2] = data[offset++];
            ip[3] = data[offset++];
            return new IPEndPoint(new IPAddress(ip), port);
        }

        private void StoreEndPoint(AttributeType type, IPEndPoint endPoint, byte[] message, ref int offset) {

            /* header */
            message[offset++] = (byte)((int)type >> 8);
            message[offset++] = (byte)((int)type & 0xFF);
            message[offset++] = 0;
            message[offset++] = 8;

            /* unused */
            message[offset++] = 0;

            /* family */
            message[offset++] = (byte)IPFamily.IPv4;

            /* port */
            message[offset++] = (byte)(endPoint.Port >> 8);
            message[offset++] = (byte)(endPoint.Port & 0xFF);

            /* address */
            var ipBytes = endPoint.Address.GetAddressBytes();
            message[offset++] = ipBytes[0];
            message[offset++] = ipBytes[0];
            message[offset++] = ipBytes[0];
            message[offset++] = ipBytes[0];
        }
    }
}