using System;
using System.Collections.Generic;
using System.Text;

namespace Reactor.Tests
{
    public static class Tcp
    {
        public static void TestServerHangup() {

            var server = new Reactor.Tcp.Server((socket) => {

                Console.WriteLine("server got socket");

                Console.WriteLine("server sending message");

                socket.Write("hello there");

                Console.WriteLine("server waiting 2 seconds");

                Reactor.Timeout.Create(() => {

                    Console.WriteLine("server hanging up");

                    socket.End();

                }, 2000);

            }).Listen(5000);

            var client = Reactor.Tcp.Socket.Create(5000);

            client.OnConnect += () => {

                Console.WriteLine("client connected");

                client.OnData += (data) => {

                    Console.WriteLine("client recv: " + data.ToString(Encoding.UTF8));
                };

                client.OnEnd += () => {

                    Console.WriteLine("client was disconnected");
                };    
            };
        }

        public static void TestClientHangup()
        {
            var server = Reactor.Tcp.Server.Create((socket) => {

                Console.WriteLine("server got socket");

                Console.WriteLine("server emitting every 1 second");

                var interval = Reactor.Interval.Create(() => {

                    socket.Write("hello there");

                }, 1000);

                socket.OnEnd += () =>
                {
                    Console.WriteLine("server detected hangup, clearing interval");

                    interval.Clear();
                };

            }).Listen(5000);

            var client = Reactor.Tcp.Socket.Create(5000);

            int count = 0;

            client.OnConnect += () =>
            {
                Console.WriteLine("client connected");
            };

            client.OnData += (data) =>
            {
                Console.WriteLine("client recv: " + data.ToString(Encoding.UTF8));

                count++;

                if (count == 5)
                {

                    Console.WriteLine("client hangin up after 5 requests");

                    client.End();
                }
            };

            client.OnEnd += () =>
            {
                Console.WriteLine("client was disconnected");
            };        
        }

        public static void TestMaxConnections(int numberOfConnections)
        {
            var server = new Reactor.Tcp.Server((socket) =>
            {
                socket.Write("hello there");

                socket.End();

            }).Listen(5000);

            server.OnSocketError += (error) => {

                Console.WriteLine(error);
            };

            for (int i = 0; i < numberOfConnections; i++)
            {
                var client = Reactor.Tcp.Socket.Create(5000);

                client.OnConnect += () =>
                {
                    Console.WriteLine("client connected");

                    client.OnData += (data) => {

                        Console.WriteLine("client recv: " + data.ToString(Encoding.UTF8));
                    };

                    client.OnEnd += () => {

                        Console.WriteLine("client was disconnected");
                    };

                    client.OnSocketError += (error) => {

                        Console.WriteLine(error);
                    };
                };
            }
        }

        public static void Test64kChunkSend(int connections, int multiplier)
        {
            var sendbuffer = new byte[65546];

            Reactor.Tcp.Server.Create((socket) => {

                for (var i = 0; i < multiplier; i++) {

                    socket.Write(sendbuffer);
                }

                socket.End();

            }).Listen(5000);

            int bytes_received = 0;

            int completed = 0;

            var start = DateTime.Now;

            for (var i = 0; i < connections; i++)
            {
                var socket = Reactor.Tcp.Socket.Create(5000);

                socket.OnConnect += () =>
                {
                    Console.WriteLine("connected");

                    socket.OnData += (data) =>
                    {
                        bytes_received += (int)data.Length;
                    };

                    socket.OnEnd += () =>
                    {
                        Console.WriteLine("complete");

                        completed += 1;

                        if (completed == connections)
                        {
                            Console.WriteLine("all clients received {0}", Util.SizeSuffix(bytes_received));

                            Console.WriteLine("completed in {0}", (DateTime.Now - start).TotalSeconds);
                        }
                    };


                };
            }
        }
    }
}
