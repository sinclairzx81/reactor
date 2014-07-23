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

            Reactor.Web.Socket.Server.Create(5000, "/", socket =>
            {
                Console.WriteLine("have socket");

                socket.OnMessage += (message) =>
                {
                    Console.WriteLine(message.Data);
                };
            });

            var client = Reactor.Web.Socket.Socket.Create("ws://localhost:5000/");

            client.OnOpen += () =>
            {
                Reactor.Interval.Create(() =>
                {
                    client.Send("hello there");

                }, 1);
            };
        }
    }
}
