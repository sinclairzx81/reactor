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

            var http = Reactor.Http.Server.Create().Listen(5000);

            var web  = Reactor.Web.Server.Create(http);

            web.Get("/", context => {

                var readstream = Reactor.File.ReadStream.Create(System.IO.Directory.GetCurrentDirectory() + "/index.html");

                context.Response.ContentType   = "text/html";

                context.Response.ContentLength = readstream.Length;

                readstream.Pipe(context.Response);
            });

            web.Get("/other", context =>
            {
                

                var readstream = Reactor.File.ReadStream.Create(System.IO.Directory.GetCurrentDirectory() + "/image.jpg");

                context.Response.ContentType = "image/jpeg";

                context.Response.ContentLength = readstream.Length;

                readstream.Pipe(context.Response);
            });



            var sockets = new List<Reactor.Web.Socket.Socket>();

            var ws = Reactor.Web.Socket.Server.Create(http, "/", socket => {
                
                sockets.Add(socket);

                Console.WriteLine("connected");

                socket.OnClose += () => {

                    Console.WriteLine("disconnected");

                    sockets.Remove(socket);
                };
            });


            var x = 0.0;

            var y = 0.0;

            var angle = 0.0;

            Reactor.Interval.Create(() =>
            {
                angle += 1;

                x = Math.Cos(angle * Math.PI / 180.0);

                y = Math.Sin(angle * Math.PI / 180.0);

                var state = "{'x': " + x + ", 'y': " + y + "}";

                state = state.Replace("'", "\"");

                foreach(var socket in sockets)
                {
                    socket.Send(state);
                }

            }, 1000 / 60);
        }
    }
}
