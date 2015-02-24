
using System;

namespace console
{
    class Program
    {
        static void Main(string[] args) {

            Reactor.Loop.Start();

            Reactor.Fusion.Server.Create(socket => {

                socket.Send(System.Text.Encoding.UTF8.GetBytes("hello udp"));

                socket.End();

            }).Listen(5000);

            var client = Reactor.Fusion.Socket.Create(5000);

            client.OnConnect += () => {

                client.OnData += data => Console.WriteLine(data.ToString("utf8"));

                client.OnEnd  += () => Console.WriteLine("client got disconnected");
            };

            Console.ReadLine();
        }
    }
}
