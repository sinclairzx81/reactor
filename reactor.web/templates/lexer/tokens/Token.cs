/*--------------------------------------------------------------------------

Reactor.Web

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

using System.Collections.Generic;

namespace Reactor.Web.Templates
{
    /// <summary>
    /// Base class declaration.
    /// </summary>
    internal class Token
    {
        public string      Content                 { get; set; }

        public string      Type                    { get; set; }

        public int         Start                   { get; set; }

        public int         Length                  { get; set; }

        public int         BodyStart               { get; set; }

        public int         BodyLength              { get; set; }

        public List<Token> Tokens { get; set; }

        public Token(string content, string type, int start, int length, int bodystart, int bodylength)
        {
            this.Content      = content;

            this.Type         = type;

            this.Start        = start;

            this.Length       = length;

            this.BodyStart    = bodystart;

            this.BodyLength   = bodylength;

            this.Tokens       = new List<Token>();
        }
    }
}
