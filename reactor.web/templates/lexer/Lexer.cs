/*--------------------------------------------------------------------------

Reactor.Web

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

using System.Collections.Generic;

namespace Reactor.Web.Templates
{
    /// <summary>
    /// Document lexer
    /// </summary>
    internal static class Lexer
    {
        #region Utility

        private static int    Max        (Token token)
        {
            return token.BodyStart + token.BodyLength;
        }

        private static int    Advance    (Token token, int index)
        {
            for (var i = index; i < Max(token); i++)
            {
                var code = token.Content[i];

                if ((code >= 48   && code <= 57)  ||
                    (code >= 65   && code <= 122) ||
                     code == 123 ||
                     code == 125 ||
                     code == 64  ||
                     code == 40  ||
                     code == 41  ||
                     code == 34  ||
                     code == 39)
                {
                    return i;
                }
            }

            return Max(token);
        }

        private static int    AdvanceTo  (Token token, int index, int code)
        {
            for (var i = index; i < Max(token); i++)
            {
                var _code = token.Content[i];

                if(_code == code) {
                    
                    return i;
                }
            }

            return Max(token);
        }

        private static string Read       (Token token, int start, int length)
        {
            if (start >= Max(token))
            {
                return string.Empty;
            }

            if ((start + length) > Max(token))
            {
                var overflow = (start + length) - Max(token);

                length = length - overflow;
            }

            return token.Content.Substring(start, length);
        }

        #endregion

        #region Scanners

        private static int ScanSection   (Token token, int index)
        {
            var name       = "";

            var start      = index;

            var length     = 0;

            var bodystart  = 0;

            var bodylength = 0;

            var cursor     = (index + 8);

            // ensure there is a space between
            // @section and its name. otherwise return.
            
            if(token.Content[cursor] != 32)
            {
                return index;
            }

            // ensure that the next character is not a
            // opening brace, as @sections require names.
            // if this is the case, return.

            cursor = Advance(token, cursor);

            if (token.Content[cursor] == '{')
            {
                return index;
            }

            // scan ahead to obtain the section name.
            // once found, update the cursor. terminate
            // return the index if we reach the end
            // of the scope first.

            for (int i = cursor; i < Max(token); i++)
            {
                var code = token.Content[i];

                if (i == (Max(token) - 1))
                {
                    return index;
                }

                if((code < 48 || code > 57) && (code < 65 || code > 122)) {

                    name = Read(token, cursor, i - cursor);
                     
                    cursor = i;

                    break;
                }
            }

            // if the next char is 'not' a open body token, we
            // treat this as a bodyless section declartion.
            // we add the section and return with the end
            // compoents set to the cursor.

            var peek = Advance(token, cursor);

            if (token.Content[peek] != '{')
            {
                var section = new SectionToken(token.Content, name, start, (cursor - index), 0, 0);

                section = Lexer.Scan(section);

                token.Tokens.Add(section);

                return cursor;
            }

            // scan ahead looking for the body content. keep
            // a count of the opening and closing braces, and
            // only completing when the braces return to 0.

            var count = 0;

            for (var i = cursor; i < Max(token); i++)
            {
                var ch = token.Content[i];

                if(ch == '{') {

                    if(count == 0) {

                        bodystart = i + 1;
                    }

                    count += 1;
                }

                if(ch == '}') {
                
                    count -= 1;

                    if(count == 0) {
                    
                        bodylength = (i - bodystart);

                        length     = (i - index) + 1;

                        break;
                    }
                }
            }

            var _section = new SectionToken(token.Content, name, start, length, bodystart, bodylength);

            _section = Lexer.Scan(_section);

            token.Tokens.Add(_section);

            return index + _section.Length;
        }

        private static int ScanImport    (Token token, int index)
        {
            var filename = "";

            var start    = index;
            
            var length   = 0;
            
            var cursor   = (index + 7);

            cursor = Advance(token, cursor);

            var quote_flag = 0;

            // ensure that the next character after
            // @import is either a single or double
            // qoute. if detected, then set the quote
            // flag to be that value, otherwise return.
            var code = token.Content[cursor];

            if(code == 39 || code == 34) {

                quote_flag = code;
            }
            else {

                return (index);
            }

            // advance one and scan through the @import
            // filename and gather the content. if we recieve
            // a newline or other invalid character along the
            // way, then terminate and return the index starting
            // location.

            cursor += 1;

            for (var i = cursor; i < Max(token); i++)
            {
                code = token.Content[i];

                if(code == 10 || code == 13) {
                
                    return index;
                }

                if(code == quote_flag) {

                    filename = Read(token, cursor, i - cursor);

                    length = (i - index) + 1;

                    break;
                }
            }

            var import = new ImportToken(token.Content, filename, start, length);

            token.Tokens.Add(import);

            return index + import.Length;
        }

        private static int ScanRender    (Token token, int index)
        {
            var filename   = "";
            
            var start      = index;
            
            var length     = 0;
            
            var cursor     = (index + 7);

            cursor         = Advance(token, cursor);

            var quote_flag = 0;

            // ensure that the next character after
            // @render is either a single or double
            // qoute. if detected, then set the quote
            // flag to be that value, otherwise return.
            var code = token.Content[cursor];

            if(code == 39 || code == 34) {

                quote_flag = code;
            }
            else {

                return (index);
            }

            // advance one and scan through the @render
            // filename and gather the content. if we recieve
            // a newline or other invalid character along the
            // way, then terminate and return the index starting
            // location.

            cursor += 1;

            for (var i = cursor; i < Max(token); i++)
            {
                code = token.Content[i];

                if(code == 10 || code == 13) {
                
                    return index;
                }

                if(code == quote_flag) {

                    filename = Read(token, cursor, i - cursor);

                    length   = (i - index) + 1;

                    break;
                }
            }

            var render = new RenderToken(token.Content, filename, start, length);

            token.Tokens.Add(render);

            return index + render.Length;
        }

        private static int ScanComment   (Token token, int index)
        {
            var comment = "";

            var start   = index;
            
            var length  = 0;
            
            var cursor  = index + 2;

            // scan through the content reading the body
            // of the comment. we are checking for the
            // pattern *@, indicating the end of the
            // comment.

            for (var i = cursor; i < Max(token); i++)
            {
                var code = token.Content[i];

                if (code == 42) // *
                { 
                    if (token.Content[i + 1] == 64) // @
                    {
                        i = i + 1;

                        comment = Read(token, index, (i - cursor) + 2);

                        length = (i - index) + 1;

                        break;
                    }
                }
            }

            var declaration = new CommentToken(token.Content, start, length);

            token.Tokens.Add(declaration);

            return index + declaration.Length;
        }

        private static int ScanContent   (Token token, int index)
        {
            // here we scan to the next @. however, we
            // skip +1 from the index to prevent getting
            // stuck on subsequent calls. in the string "123@123"
            // this will match "123" on first pass and "@123" on
            // the subsequent pass.

            var cursor = AdvanceTo(token, index + 1, 64);

            var declaration = new ContentToken(token.Content, index, cursor - index);

            if (declaration.Length > 0)
            {
                token.Tokens.Add(declaration);
            }

            return (index + declaration.Length);
        }

        public static T Scan<T>(T token) where T : Token
        {
            var index = token.BodyStart;

            do 
            {        
                //---------------------------------------------
                // scan section
                //---------------------------------------------

                if (Read(token, index, 8) == "@section") 
                {
                    var next = ScanSection(token, index);
                    
                    if(next > index) 
                    {    
                        index = next;

                        continue;
                    }
                }

                //---------------------------------------------
                // scan import
                //---------------------------------------------
                if (Read(token, index, 7) == "@import")
                {
                    var next = ScanImport(token, index);

                    if(next > index) 
                    {    
                        index = next;

                        continue;
                    }
                }

                //---------------------------------------------
                // scan render
                //---------------------------------------------
                if (Read(token, index, 7) == "@render") 
                {
                    var next = ScanRender(token, index);
 
                    if(next > index) 
                    {    
                        index = next;

                        continue;
                    }
                }

                //---------------------------------------------
                // scan comment
                //---------------------------------------------
                if (Read(token, index, 2) == "@*")
                {
                    var next = ScanComment(token, index);

                    if(next > index) 
                    {    
                        index = next;

                        continue;
                    }
                }
                           
                //---------------------------------------------
                // scan content
                //---------------------------------------------

                index = ScanContent(token, index);

            } while (index < Max(token));

            return token;
        }

        #endregion
    }
}
