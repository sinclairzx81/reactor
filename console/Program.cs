
using System;

namespace console
{
    class Program
    {
        static void RemoteServer()
        {
            Reactor.Fusion.Server.Create(fusion => {

                var tcp    = Reactor.Tcp.Socket.Create("domain.com", 80);

                var buffer = Reactor.Buffer.Create();

                fusion.OnData += buffer.Write;
                
                tcp.OnConnect += () => {

                    fusion.OnData -= buffer.Write;

                    tcp.Write(buffer);

                    fusion.OnData += tcp.Write;

                    fusion.OnEnd  += tcp.End;

                    tcp.OnData    += data => fusion.Send(data.ToArray());

                    tcp.OnEnd     += fusion.End;
                };

            }).Listen(5010);            
        }

        static void LocalServer()
        {
            Reactor.Tcp.Server.Create(tcp => {

                var fusion    = Reactor.Fusion.Socket.Create(5010);

                var buffer = Reactor.Buffer.Create();

                tcp.OnData       += buffer.Write;

                fusion.OnConnect += () =>
                {
                    tcp.OnData    -= buffer.Write;

                    fusion.Send(buffer.ToArray());

                    fusion.OnData += data => tcp.Write(data);

                    fusion.OnEnd  += ()   => tcp.End();

                    tcp.OnData    += data => fusion.Send(data.ToArray());

                    tcp.OnEnd     += ()   => fusion.End();
                };

            }).Listen(5001);            
        }
        static void Main(string[] args) {

            Reactor.Loop.Start();

            RemoteServer();

            LocalServer();
        }
    }
}
