///*--------------------------------------------------------------------------

//Reactor.Web

//The MIT License (MIT)

//Copyright (c) 2015 Haydn Paterson (sinclair) <haydn.developer@gmail.com>

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

//---------------------------------------------------------------------------*/

//using System.IO;

//namespace Reactor.Web.Media
//{
//    public static class Stream
//    {
//        /// <summary>
//        /// Streams media from this file to the http response stream.
//        /// </summary>
//        /// <param name="context">The reactor web context</param>
//        /// <param name="filename">The filename to stream.</param>
//        public static void From (Reactor.Web.Context context, string filename)
//        {
//            //--------------------------------------------------------
//            // if file not found, 404 response.
//            //--------------------------------------------------------

//            if (!System.IO.File.Exists(filename))
//            {
//                var buffer = Reactor.Buffer.Create("file not found");

//                context.Response.StatusCode    = 404;

//                context.Response.ContentType   = "text/plain";

//                context.Response.ContentLength = buffer.Length;

//                context.Response.Write(buffer);

//                context.Response.End();

//                return;
//            }

//            //--------------------------------------------------------
//            // format disposition
//            //--------------------------------------------------------

//            var disposition = System.IO.Path.GetFileName(filename).Replace(",", string.Empty).Replace(";", string.Empty).Replace("\"", string.Empty)
//                                                                  .Replace("'", string.Empty).Replace("+", string.Empty).Replace("\n", string.Empty)
//                                                                  .Replace("\t", string.Empty).Replace("@", string.Empty).Replace("$", string.Empty)
//                                                                  .Replace("^", string.Empty).Replace("%", string.Empty).Replace("(", string.Empty);

//            //--------------------------------------------------------
//            // check for content range.
//            //--------------------------------------------------------

//            var range = context.Request.Headers["Range"];

//            if (range == null)
//            {
//                var readstream = Reactor.File.ReadStream.Create(filename, FileMode.OpenOrCreate, FileShare.ReadWrite);

//                context.Response.ContentType = Reactor.Web.Media.Mime.Lookup(filename);

//                context.Response.ContentLength = readstream.Length;

//                context.Response.StatusCode = 200;

//                context.Response.AppendHeader("Content-Disposition", "inline; filename=" + disposition);

//                context.Response.AddHeader("Cache-Control", "public");

//                readstream.Pipe(context.Response);

//                return;
//            }

//            //-----------------------------------------------
//            // if ranged, compute parameters.
//            //-----------------------------------------------

//            var info = new System.IO.FileInfo(filename);

//            var split = range.Replace("bytes=", "").Split(new char[] { '-' });

//            var start = long.Parse(split[0]);

//            var end = info.Length - 1;

//            if (split[1] != "")
//            {
//                end = long.Parse(split[1]);
//            }

//            //---------------------------------------------
//            // stream ranged request
//            //---------------------------------------------

//            context.Response.ContentType   = Reactor.Web.Media.Mime.Lookup(filename);

//            context.Response.ContentLength = (end - start) + 1;

//            context.Response.StatusCode    = 206;

//            context.Response.AppendHeader("Content-Disposition", "inline; filename=" + disposition);

//            context.Response.AddHeader("Content-Range", string.Format("bytes {0}-{1}/{2}", start, end, info.Length));

//            context.Response.AddHeader("Cache-Control", "public");

//            var streamed = Reactor.File.ReadStream.Create(filename, start, (end - start) + 1, FileMode.OpenOrCreate, FileShare.ReadWrite);

//            streamed.Pipe(context.Response);
//        }

//        /// <summary>
//        /// Streams media from the http request stream to file. Provides a callback on response.
//        /// </summary>
//        /// <param name="context">The reactor web context</param>
//        /// <param name="filename">The filename to stream this file to.</param>
//        /// <param name="callback">The callback fired on complete.</param>
//        public static void To   (Reactor.Web.Context context, string filename, Reactor.Action<System.Exception> callback)
//        {
//            Reactor.File.WriteStream writestream;

//            //---------------------------------------------------------------
//            // create appropriate file writestream
//            //--------------------------------------------------------------- 

//            var range = context.Request.Headers["Range"];

//            if (range == null)
//            {
//                if(!System.IO.File.Exists(filename)) 
//                {
//                    writestream = Reactor.File.WriteStream.Create(filename, 0, FileMode.OpenOrCreate, FileShare.ReadWrite);
//                }
//                else 
//                {
//                    writestream = Reactor.File.WriteStream.Create(filename, 0, FileMode.Truncate, FileShare.ReadWrite);
//                }
//            }
//            else
//            {
//                System.Console.WriteLine(range);

//                //---------------------------------------------------------------
//                // compute range
//                //---------------------------------------------------------------

//                long start = 0;

//                long end   = long.MaxValue;

//                var split  = range.Replace("bytes=", "").Split(new char[] { '-' });

//                if(split.Length > 0)
//                {
//                    long.TryParse(split[0], out start);
//                }
//                if(split.Length > 1)
//                {
//                    if (split[1] != "")
//                    {
//                        long.TryParse(split[1], out end);
//                    }
//                }

//                //---------------------------------------------------------------
//                // create the writestream based on the start offset. note that
//                // if the start is 0, we truncate the file. If greater than 0,
//                // we set the write index to that position and begin writing
//                // from there.
//                //--------------------------------------------------------------- 

//                try
//                {
//                    if (start == 0)
//                    {
//                        if (!System.IO.File.Exists(filename))
//                        {
//                            writestream = Reactor.File.WriteStream.Create(filename, start, FileMode.Create, FileShare.ReadWrite);
//                        }
//                        else
//                        {
//                            writestream = Reactor.File.WriteStream.Create(filename, start, FileMode.Truncate, FileShare.ReadWrite);
//                        }
//                    }
//                    else
//                    {
//                        if (!System.IO.File.Exists(filename))
//                        {
//                            callback(new System.Exception("received offer greater than 0 for non existent file."));

//                            return;
//                        }
//                        else
//                        {
//                            var info = new FileInfo(filename);

//                            if(start > info.Length)
//                            {
//                                callback(new System.Exception("offset value greater than the size of this file."));

//                                return;
//                            }

//                            writestream = Reactor.File.WriteStream.Create(filename, start, FileMode.OpenOrCreate, FileShare.ReadWrite);
//                        }
//                    }
//                }
//                catch(System.Exception exception)
//                {
//                    callback(exception);

//                    return;
//                }
//            }

//            //---------------------------------------------------------------
//            // stream the file to the writestream. 
//            //--------------------------------------------------------------- 

//            context.Request.Pipe(writestream);

//            var complete = false;

//            context.Request.OnError += (error) => {

//                if(!complete) {

//                    writestream.End();

//                    complete = true;

//                    callback(error);
//                }
//            };

//            context.Request.OnEnd += () => {

//                if(!complete) {

//                    complete = true;

//                    callback(null);
//                }             
//            };
//        }

//        /// <summary>
//        /// Serves client streaming script.
//        /// </summary>
//        /// <param name="context">The reactor web http context.</param>
//        public static void Script   (Reactor.Web.Context context)
//        {
//            var buffer = Reactor.Buffer.Create(Reactor.Web.Resource.Client);

//            context.Response.ContentType = "text/javascript";

//            context.Response.ContentLength = buffer.Length;

//            context.Response.Write(buffer);

//            context.Response.End();
//        }
//    }
//}
