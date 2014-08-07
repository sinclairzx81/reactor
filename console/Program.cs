using Reactor;
using Reactor.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace console
{
    public static class MediaStream
    {
        /// <summary>
        /// Streams this media out to the http response buffer by analyzing the http request. This is done be checking the
        /// Http Request Content-Range header and checking for the
        /// </summary>
        /// <param name="response"></param>
        /// <param name="request"></param>
        /// <param name="filename"></param>
        public static void Stream(this Reactor.Http.ServerResponse response, Reactor.Http.ServerRequest request, string filename)
        {
            //--------------------------------------------------------
            // if file not found, 404 response.
            //--------------------------------------------------------

            if (!System.IO.File.Exists(filename))
            {
                var buffer = Reactor.Buffer.Create("file not found");

                response.StatusCode = 404;

                response.ContentType = "text/plain";

                response.ContentLength = buffer.Length;

                response.Write(buffer);

                response.End();

                return;
            }

            //--------------------------------------------------------
            // format disposition
            //--------------------------------------------------------

            var disposition = System.IO.Path.GetFileName(filename).Replace(",", string.Empty).Replace(";", string.Empty).Replace("\"", string.Empty)
                                                                  .Replace("'", string.Empty).Replace("+", string.Empty).Replace("\n", string.Empty)
                                                                  .Replace("\t", string.Empty).Replace("@", string.Empty).Replace("$", string.Empty)
                                                                  .Replace("^", string.Empty).Replace("%", string.Empty).Replace("(", string.Empty);

            //--------------------------------------------------------
            // check for content range.
            //--------------------------------------------------------

            var range = request.Headers["Range"];

            if (range == null)
            {
                var readstream = Reactor.File.ReadStream.Create(filename, FileMode.OpenOrCreate, FileShare.ReadWrite);

                response.ContentType = Reactor.Web.Media.Mime.Lookup(filename);

                response.ContentLength = readstream.Length;

                response.StatusCode = 200;

                response.AppendHeader("Content-Disposition", "inline; filename=" + disposition);

                response.AddHeader("Cache-Control", "public");

                readstream.Pipe(response);

                return;
            }

            //-----------------------------------------------
            // if ranged, compute parameters.
            //-----------------------------------------------

            var info  = new System.IO.FileInfo(filename);

            var split = range.Replace("bytes=", "").Split(new char[] { '-' });

            var start = long.Parse(split[0]);

            var end = info.Length - 1;

            if (split[1] != "")
            {
                end = long.Parse(split[1]);
            }

            //---------------------------------------------
            // stream ranged request
            //---------------------------------------------

            response.ContentType   = Reactor.Web.Media.Mime.Lookup(filename);

            response.ContentLength = (end - start) + 1;

            response.StatusCode    = 206;

            response.AppendHeader("Content-Disposition", "inline; filename=" + disposition);

            response.AddHeader ("Content-Range", string.Format("bytes {0}-{1}/{2}", start, end, info.Length));

            response.AddHeader("Cache-Control", "public");

            var streamed = Reactor.File.ReadStream.Create(filename, start, (end - start) + 1, FileMode.OpenOrCreate, FileShare.ReadWrite);

            streamed.Pipe(response);
        }


        public static void Stream(this Reactor.Http.ServerRequest request, string filename, Reactor.Action<Exception> callback)
        {

        }
    }

    class Program
    {
        static void Server()
        {
            var server = Reactor.Web.Server.Create();

            server.Get("/reactor.js", context => {

                Reactor.Web.Media.Streams.ClientScript(context);
            });

            server.Get("/download", context => {

                Reactor.Web.Media.Streams.Download(context, "c:/input/upload.mp4");
            });

            server.Post("/upload", context => {

                Reactor.Web.Media.Streams.Upload(context, "c:/input/upload.mp4", (exception) => {

                    if (exception != null) {

                        context.Response.StatusCode = 500;

                        context.Response.Write(exception.Message);

                        context.Response.End();

                        return;
                    }

                    context.Response.StatusCode = 200;

                    context.Response.Write("ok");

                    context.Response.End();
                });
            });

            #region OldCode

            //server.Post("/upload", context => {

            //    Reactor.Action<string> error = (message) => {

            //        context.Response.StatusCode = 500;

            //        context.Response.ContentType = "text/plain";

            //        var buffer = Reactor.Buffer.Create(message);

            //        context.Response.Write(buffer);

            //        context.Response.End();
            //    };

            //    Reactor.Action<string> success = (message) => {

            //        context.Response.StatusCode = 200;

            //        context.Response.ContentType = "text/plain";

            //        var buffer = Reactor.Buffer.Create(message);

            //        context.Response.Write(buffer);

            //        context.Response.End();
            //    };

            //    string filename = "c:/input/upload.mp4";

            //    long   offset = 0;

            //    Console.WriteLine(context.Request.Headers["Content-Range"]);

            //    //------------------------------------
            //    // compute range
            //    //------------------------------------
            //    if(context.Request.Headers["offset"] != null) {

            //        if(!long.TryParse(context.Request.Headers["offset"], out offset)) {

            //            error("unable to parse offset");

            //            return;
            //        }
            //    }

            //    //------------------------------------
            //    // validate range
            //    //------------------------------------

            //    if (offset > 0)
            //    {
            //        if (System.IO.File.Exists(filename))
            //        {
            //            var info = new System.IO.FileInfo(filename);

            //            if (offset > info.Length)
            //            {
            //                error("offset exceeds the range of the file.");

            //                return;
            //            }
            //        }
            //        else
            //        {
            //            error("offset exceeds the range of the file.");

            //            return;
            //        }
            //    }

            //    //------------------------------------
            //    // create writestream
            //    //------------------------------------

            //    Reactor.File.WriteStream writestream;

            //    if (offset == 0)  {

            //        if (System.IO.File.Exists(filename))
            //        {
            //            writestream = Reactor.File.WriteStream.Create(filename, offset, FileMode.Truncate, FileShare.ReadWrite);
            //        }
            //        else
            //        {
            //            writestream = Reactor.File.WriteStream.Create(filename, offset, FileMode.Create, FileShare.ReadWrite);
            //        }
                    
            //    }
            //    else {

            //        writestream = Reactor.File.WriteStream.Create(filename, offset, FileMode.OpenOrCreate, FileShare.ReadWrite);
            //    }

            //    //------------------------------------
            //    // stream request
            //    //------------------------------------

            //    context.Request.Pipe(writestream);

            //    context.Request.OnData += (d) => Console.Write(".");
                
            //    context.Request.OnEnd  += () => {

            //        Console.Write("e");

            //        success("ok");
            //    };
            //});

            #endregion

            server.Get("/", context =>
            {
                var readstream = Reactor.File.ReadStream.Create("c:/input/bstream/index.html");

                context.Response.ContentType = "text/html";

                context.Response.ContentLength = readstream.Length;

                readstream.Pipe(context.Response);
            });

            server.Listen(5000);
        }

        static void Main(string[] args)
        {
            Reactor.Loop.Start();

            Server();

            


            

        }
    }
}
