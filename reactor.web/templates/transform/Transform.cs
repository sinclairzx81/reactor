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
    internal static class DocumentTransform
    {
        private static DocumentToken GetDocument(string name, List<DocumentToken> documents)
        {
            foreach (var document in documents)
            {
                if (document.Name == name)
                {
                    return document;
                }
            }

            return null;
        }

        private static List<DocumentToken> CreateDocumentChain(DocumentToken document, List<DocumentToken> documents)
        {
            var chain = new List<DocumentToken>();

            chain.Add(document);

            while (document != null)
            {
                var import = document.GetImport();

                if (import == null)
                {
                    document = null;

                    break;
                }

                if (GetDocument(import.Name, chain) != null)
                {
                    var message = string.Format("error: cyclic imports detected for ", import.Name);

                    var error = new DocumentToken(document.Name, message);

                    error.Tokens.Add(new ContentToken(error.Content, 0, error.Content.Length));

                    return new List<DocumentToken>(new DocumentToken[] {error});
                }

                document = GetDocument(import.Name, documents);

                if (document != null)
                {
                    chain.Add(document);
                }
            }

            chain.Reverse();

            return chain;
        }

        private static DocumentToken TransformSections(DocumentToken document, List<DocumentToken> documents)
        {
            var transform  = new DocumentToken(document.Name, document.Content);

            var chain      = CreateDocumentChain(document, documents);

            var root       = chain[0];

            var decendants = new List<DocumentToken>();

            for (int i = 1; i < chain.Count; i++) {

                decendants.Add(chain[i]);
            }

            foreach (var token in root.Tokens)
            {
                if (token is SectionToken)
                {
                    var section = token as SectionToken;

                    foreach (var decendant in decendants)
                    {
                        var subsection = decendant.GetSection(section.Name);

                        if (subsection != null)
                        {
                            section = subsection;
                        }
                        else
                        {
                            break;
                        }
                    }

                    transform.Tokens.Add(section);
                }
                else
                {
                    transform.Tokens.Add(token);
                }
            }

            return transform;
        }

        
        private static int renderdepth = 0;

        private static void TransformRender(DocumentToken transform, Token token, List<DocumentToken> documents)
        {
            if (token is RenderToken)
            {
                var render = token as RenderToken;

                renderdepth += 1;

                if(renderdepth > 32)
                {
                    renderdepth -= 1;

                    string message = string.Format("error: exceeded maximum render depth for ", render.Name);

                    transform.Tokens.Add(new ContentToken(message, 0, message.Length));

                    return;
                }

                var subdocument = GetDocument(render.Name, documents);

                if (subdocument != null)
                {
                    subdocument = Transform(subdocument, documents);

                    foreach (var _token in subdocument.Tokens)
                    {
                        TransformRender(transform, _token, documents);
                    }
                }

                renderdepth -= 1;
                
                return;
            }

            if (token is ContentToken)
            {
                transform.Tokens.Add(token);
            }

            foreach (var _token in token.Tokens)
            {
                TransformRender(transform, _token, documents);
            }
        }

        private static DocumentToken TransformRender(DocumentToken document, List<DocumentToken> documents)
        {
            var transform = new DocumentToken(document.Name, document.Content);

            foreach (var token in document.Tokens)
            {
                TransformRender(transform, token, documents);
            }

            return transform;
        }

        public static DocumentToken Transform(DocumentToken document, List<DocumentToken> documents)
        {
            var transform = DocumentTransform.TransformSections(document, documents);

            transform = DocumentTransform.TransformRender(transform, documents);

            return transform;
        }
    }
}
