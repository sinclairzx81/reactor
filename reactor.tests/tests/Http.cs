using System;
using System.Collections.Generic;
using System.Text;

namespace Reactor.Tests
{
    public static class Http
    {
        public static void TestHttpChain()
        {
            Reactor.Http.Server.Create((context) => {

                Console.WriteLine("server 5002");

                context.Request.Pipe(context.Response);

            }).Listen(5002);

            Reactor.Http.Server.Create((context) => {

                Console.WriteLine("server 5001");

                var client = Reactor.Http.Request.Create("http://localhost:5002", (response) => {

                    response.Pipe(context.Response);
                });

                client.Method = "POST";

                client.ContentLength = context.Request.ContentLength;

                context.Request.Pipe(client);

            }).Listen(5001);

            var server = Reactor.Http.Server.Create((context) =>
            {
                Console.WriteLine("server 5000");

                var client = Reactor.Http.Request.Create("http://localhost:5001", (response) => {

                    response.Pipe(context.Response);
                });

                client.Method = "POST";

                client.ContentLength = context.Request.ContentLength;

                context.Request.Pipe(client);
            });

            server.OnError += (error) => Console.WriteLine(error);

            server.Listen(5000);

            for (int i = 0; i < 10; i++)
            {
                var request = Reactor.Http.Request.Create("http://localhost:5000", (response) => {

                    response.OnData += (data) => Console.WriteLine("recv: " + data.ToString(Encoding.UTF8));
                });

                byte[] postdata = Encoding.UTF8.GetBytes("hello");

                request.Method = "POST";

                request.ContentLength = postdata.Length;

                request.Write(postdata);

                request.End();

                request.OnError += (error) => Console.WriteLine(    error);
            }
        }
    }
}
