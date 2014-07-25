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

            var headers = new Dictionary<string, string>();

            headers["Origin"] = "http://www.websocket.org";

            var socket = Reactor.Web.Socket.Socket.Create("http://echo.websocket.org", headers);

            socket.OnOpen += () =>
            {

                Console.WriteLine("connected");

                Reactor.Interval.Create(() =>
                {
                    socket.Send("heelo");

                }, 100);

                


            };

            socket.OnMessage += (m) =>
            {
                Console.WriteLine(m.Data);

            };

            socket.OnError += (exception) =>
            {
                Console.WriteLine(exception);

            };

        }
    }
}
