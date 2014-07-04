using System;
using System.Collections.Generic;
using System.Text;

namespace console
{
    class Program
    {
        static void Main(string[] args)
        {
            Reactor.Loop.Start();

            var server = Reactor.Web.Server.Create();

            server.Get("/", (context) => {

                context.Response.Write("hello world");

                context.Response.End();
            });

            server.Listen(5000);

        }
    }
}
