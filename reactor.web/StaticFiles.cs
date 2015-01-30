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

namespace Reactor.Web
{
    public class StaticFiles
    {
        public static Middleware Serve(string basePath, string directory)
        {
            return (context, next) => {

                //-----------------------------------------
                // format basepath
                //-----------------------------------------

                if (basePath[basePath.Length - 1] != '/')
                {
                    basePath = basePath + "/";
                }

                //-----------------------------------------
                // check the basepath.
                //-----------------------------------------
                if(context.Request.Url.AbsolutePath.IndexOf(basePath) != 0) {

                    next();

                    return;
                }

                //-----------------------------------------
                // create filename and check exists
                //-----------------------------------------

                var filename = directory + "/" + context.Request.Url.AbsolutePath.Replace(basePath, string.Empty);

                if (!System.IO.File.Exists(filename))
                {
                    next();

                    return;                        
                }

                //-----------------------------------------
                // serve the file.
                //-----------------------------------------

                var readstream = Reactor.File.ReadStream.Create(filename);

                context.Response.ContentLength = readstream.Length;

                context.Response.ContentType   = Reactor.Web.Media.Mime.Lookup(filename);

                readstream.Pipe(context.Response);
            };
        }
    }
}
