using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace console
{
    class Program
    {
        static Task<string> Download(string url)
        {
            var tsc = new TaskCompletionSource<string>();
            
            Reactor.Http.Request.Get(url, (exception, buffer) =>
            {
                if (exception != null)
                {
                    tsc.SetException(exception);

                    return;
                }
                tsc.SetResult(buffer.ToString("utf8"));
            });

            return tsc.Task;
        }

        static void Main(string[] args)
        {
            Reactor.Loop.Start();

            Reactor.Http.Server.Create((context) =>
            {
                var readstreams = Reactor.File.ReadStream.Create("c:/input/image.jpg");

                context.Response.ContentType = "image/jpg";

                context.Response.ContentLength = readstreams.Length;

                readstreams.Pipe(context.Response);



            }).Listen(5000);
        }
    }
}
