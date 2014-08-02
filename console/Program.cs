using System;
using System.Collections.Generic;
using System.Text;


namespace console
{
    public static class Ext
    {
        public static void Render(this Reactor.Http.ServerResponse response, string filename)
        {
            var template = Reactor.Web.Templates.Template.Create(filename);

            var buffer   = Reactor.Buffer.Create(template.Render());

            response.ContentType = "text/html";

            response.ContentLength = buffer.Length;

            response.Write(buffer);

            response.End();
        }
    }

    class Program
    {
        public static void Do(int n, Reactor.Action<Exception, int> callback)
        {
            var task = Reactor.Async.Task<int, int>((input) =>
            {
                var random = new Random();

                System.Threading.Thread.Sleep(random.Next(100, 101));

                return input;
            });

            task(n, callback);
        }

        static void Main(string[] args)
        {
            Reactor.Loop.Start();
        }
    }
}
