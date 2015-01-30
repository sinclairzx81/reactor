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

namespace Reactor.Web.Templates
{

    /// <summary>
    /// Document declaration. Encapsulates a entire document.
    /// </summary>
    internal class DocumentToken : Token
    {
        public string Name { get; set; }

        public DocumentToken(string name, string content): base(content, "document", 0, content.Length, 0, content.Length)
        {
            this.Name = name;
        }
        
        public ImportToken GetImport()
        {
            foreach (var token in this.Tokens)
            {
                if (token is ImportToken)
                {
                    return token as ImportToken; 
                }
            }

            return null;
        }

        public SectionToken GetSection(string name)
        {
            foreach (var token in this.Tokens)
            {
                if (token is SectionToken)
                {
                    var section = token as SectionToken;

                    if(section.Name == name)
                    {
                        return section;
                    }
                }
            }
            return null;
        }
    }
}
