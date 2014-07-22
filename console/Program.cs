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

            var subset = Reactor.Async.Filter(new int[] { 0, 1, 2, 3, 4 }, (item, index, list) => {
                
                return item % 2 == 0;
            });

            Reactor.Http.Server.Create(context =>
            {
                context.Response.Write("hello world");

                context.Response.End();
            
            }).Listen(5000);
        }
    }
}
