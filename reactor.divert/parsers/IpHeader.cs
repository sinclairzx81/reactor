/*--------------------------------------------------------------------------

Reactor.Divert

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

using System.IO;
using System.Net;

namespace Reactor.Divert.Parsers
{
    public enum ProtocolType
    {
        TCP,

        UDP,

        Unknown
    }

    public class IpHeader
    {
        #region Header

        //------------------------------------------------------------------                 
        // 0                   1                   2                   3   
        // 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 
        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        // |Version|  IHL  |Type of Service|          Total Length         |
        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        // |         Identification        |Flags|      Fragment Offset    |
        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        // |  Time to Live |    Protocol   |         Header Checksum       |
        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        // |                       Source Address                          |
        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        // |                    Destination Address                        |
        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        // |                    Options                    |    Padding    |
        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
 
        #endregion

        #region Fields

        private byte   byVersionAndHeaderLength;

        private byte   byDifferentiatedServices;

        private ushort usTotalLength;

        private ushort usIdentification;

        private ushort usFlagsAndOffset;

        private byte   byTTL;

        private byte   byProtocol;

        private short  sChecksum;

        private uint   uiSourceIPAddress;

        private uint   uiDestinationIPAddress;    
        
        private byte   byHeaderLength;

        private byte[] byIPData = new byte[4096];

        #endregion

        public IpHeader (byte[] data)
        {
            using (var stream = new MemoryStream(data, 0, data.Length))
            {
                using(var reader = new BinaryReader(stream))
                {
                    //-----------------------------------------------
                    // The first eight bits of the IP header contain the version and
                    // header length so we read them
                    //-----------------------------------------------
                    this.byVersionAndHeaderLength = reader.ReadByte();
                    
                    //-----------------------------------------------
                    // The next eight bits contain the Differentiated services
                    //-----------------------------------------------
                    this.byDifferentiatedServices = reader.ReadByte();

                    //-----------------------------------------------
                    // Next eight bits hold the total length of the datagram
                    //-----------------------------------------------
                    this.usTotalLength            = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());

                    //-----------------------------------------------
                    // Next sixteen have the identification bytes
                    //-----------------------------------------------
                    this.usIdentification         = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());

                    //-----------------------------------------------
                    // Next sixteen bits contain the flags and fragmentation offset
                    //-----------------------------------------------
                    this.usFlagsAndOffset         = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());

                    //-----------------------------------------------
                    // Next eight bits have the TTL value
                    //-----------------------------------------------
                    this.byTTL                    = reader.ReadByte();

                    //-----------------------------------------------
                    //Next eight represnts the protocol encapsulated in the datagram
                    //-----------------------------------------------
                    this.byProtocol               = reader.ReadByte();

                    //-----------------------------------------------
                    // Next sixteen bits contain the checksum of the header
                    //-----------------------------------------------

                    this.sChecksum                = IPAddress.NetworkToHostOrder(reader.ReadInt16());

                    //-----------------------------------------------
                    // read source IP address
                    //-----------------------------------------------
                    this.uiSourceIPAddress        = (uint)(reader.ReadInt32());

                    //-----------------------------------------------
                    // read destination IP address
                    //-----------------------------------------------
                    this.uiDestinationIPAddress   = (uint)(reader.ReadInt32());

                    //-----------------------------------------------
                    // calculate the header length
                    //-----------------------------------------------
                    this.byHeaderLength           = byVersionAndHeaderLength;

                    //-----------------------------------------------
                    // The last four bits of the version and header length field contain the
                    // header length, we perform some simple binary airthmatic operations to
                    // extract them
                    //-----------------------------------------------
                    
                    this.byHeaderLength <<= 4;

                    this.byHeaderLength >>= 4;

                    //-----------------------------------------------
                    // Multiply by four to get the exact header length
                    //-----------------------------------------------

                    byHeaderLength *= 4;

                    System.Buffer.BlockCopy(data, 
                                            byHeaderLength, 
                                            byIPData, 
                                            0, 
                                            usTotalLength - byHeaderLength);
                }
            }
        }

        public string       Version                
        {
            get
            {
                //Calculate the IP version

                //The four bits of the IP header contain the IP version
                if ((byVersionAndHeaderLength >> 4) == 4)
                {
                    return "IP v4";
                }
                else if ((byVersionAndHeaderLength >> 4) == 6)
                {
                    return "IP v6";
                }
                else
                {
                    return "Unknown";
                }
            }
        }

        public string       HeaderLength           
        {
            get
            {
                return byHeaderLength.ToString();                
            }
        }

        public ushort       MessageLength          
        {
            get
            {
                // MessageLength = Total length of the datagram - Header length
                return (ushort)(usTotalLength - byHeaderLength);
            }
        }

        public string       DifferentiatedServices 
        {
            get
            {
                // Returns the differentiated services in hexadecimal format
                return string.Format ("0x{0:x2} ({1})", byDifferentiatedServices, byDifferentiatedServices);
            }
        }

        public string       Flags                  
        {
            get
            {
                //The first three bits of the flags and fragmentation field 
                //represent the flags (which indicate whether the data is 
                //fragmented or not)

                int nFlags = usFlagsAndOffset >> 13;

                if (nFlags == 2) {

                    return "Don't fragment";
                }
                else if (nFlags == 1) {

                    return "More fragments to come";
                }
                else {

                    return nFlags.ToString();
                }
            }
        }

        public string       FragmentationOffset    
        {
            get
            {
                // The last thirteen bits of the flags 
                // and fragmentation field contain 
                // the fragmentation offset
                int nOffset = usFlagsAndOffset << 3;

                nOffset >>= 3;

                return nOffset.ToString();
            }
        }

        public string       TTL                    
        {
            get
            {
                return byTTL.ToString();
            }
        }

        public ProtocolType ProtocolType           
        {
            get
            {
                // The protocol field represents 
                // the protocol in the data portion
                // of the datagram
                if (byProtocol == 6)        // A value of six represents the TCP protocol
                {
                    return ProtocolType.TCP;
                }
                else if (byProtocol == 17)  // Seventeen for UDP
                {
                    return ProtocolType.UDP;
                }
                else
                {
                    return ProtocolType.Unknown;
                }
            }
        }

        public string       Checksum               
        {
            get
            {
                // Returns the checksum in hexadecimal format
                return string.Format ("0x{0:x2}", sChecksum);
            }
        }

        public IPAddress    SourceAddress          
        {
            get
            {
                return new IPAddress(uiSourceIPAddress);
            }
        }

        public IPAddress    DestinationAddress     
        {
            get
            {
                return new IPAddress(uiDestinationIPAddress);
            }
        }

        public string       TotalLength            
        {
            get
            {
                return usTotalLength.ToString();
            }
        }

        public string       Identification         
        {
            get
            {
                return usIdentification.ToString();
            }
        }

        public byte[]       Data                   
        {
            get
            {
                return byIPData;
            }
        }
        
        #region Statics
        
        public static IpHeader Create(byte [] data)
        {
            return new IpHeader(data);
        }

        #endregion
    }
}
