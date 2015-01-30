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
    public class DnsHeader
    {
        #region Header

        //                                   1  1  1  1  1  1
        //     0  1  2  3  4  5  6  7  8  9  0  1  2  3  4  5
        // +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        // |                      ID                       |
        // +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        // |QR|   Opcode  |AA|TC|RD|RA| Z|AD|CD|   RCODE   |
        // +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        // |                QDCOUNT/ZOCOUNT                |
        // +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        // |                ANCOUNT/PRCOUNT                |
        // +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        // |                NSCOUNT/UPCOUNT                |
        // +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        // |                    ARCOUNT                    |
        // +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

        #endregion

        private ushort usIdentification;        

        private ushort usFlags;                 

        private ushort usTotalQuestions;        

        private ushort usTotalAnswerRRs;        

        private ushort usTotalAuthorityRRs;

        private ushort usTotalAdditionalRRs;

        public DnsHeader(byte [] data)
        {
            using(var stream = new MemoryStream(data, 0, data.Length))
            {
                using (var reader = new BinaryReader(stream))
                {
                    //----------------------------------------------
                    // First sixteen bits are for identification
                    //----------------------------------------------
                    usIdentification   = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());

                    //----------------------------------------------
                    // Next sixteen contain the flags
                    //----------------------------------------------
                    usFlags            = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());

                    //----------------------------------------------
                    // Read the total numbers of questions in the quesion list
                    //----------------------------------------------
                    usTotalQuestions   = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());

                    //----------------------------------------------
                    // Read the total number of answers in the answer list
                    //----------------------------------------------
                    usTotalAnswerRRs   = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());

                    //----------------------------------------------
                    // Read the total number of entries in the authority list
                    //----------------------------------------------
                    usTotalAuthorityRRs = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());

                    //----------------------------------------------
                    // Total number of entries in the additional resource record list
                    //----------------------------------------------
                    usTotalAdditionalRRs = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());
                }
            }
        }

        public string Identification
        {
            get
            {
                return string.Format("0x{0:x2}", usIdentification);
            }
        }

        public string Flags
        {
            get
            {
                return string.Format("0x{0:x2}", usFlags);
            }
        }

        public string TotalQuestions
        {
            get
            {
                return usTotalQuestions.ToString();
            }
        }

        public string TotalAnswerRRs
        {
            get
            {
                return usTotalAnswerRRs.ToString();
            }
        }

        public string TotalAuthorityRRs
        {
            get
            {
                return usTotalAuthorityRRs.ToString();
            }
        }

        public string TotalAdditionalRRs
        {
            get
            {
                return usTotalAdditionalRRs.ToString();
            }
        }

        #region Statics

        public static DnsHeader Create(byte [] data)
        {
            return new DnsHeader(data);
        }

        #endregion
    }
}
