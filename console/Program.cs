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
        static void Main(string[] args)
        {
            Reactor.Loop.Start();

            var server = Reactor.Web.Socket.Server.Create(5000);

            server.OnSocket += (socket) =>
            {
                socket.OnMessage += (m) =>
                {
                    Console.Write("["+m.Data.Length+"]");

                    socket.Send(m.Data);
                };

                socket.OnClose += () =>
                {
                    Console.Write("closed");
                };
            };

            while(true)
            {
                Console.ReadLine();

                GC.Collect();
            }

        }
    }
}
