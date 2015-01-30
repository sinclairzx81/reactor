
using System;

namespace console
{
    class Program
    {
        static void Main(string[] args) {

            Reactor.Loop.Start();

            var socket = Reactor.Divert.Socket.Create("(inbound or outbound) and ip");
            
            socket.Read(data => {

                var ip  = Reactor.Divert.Parsers.IpHeader.Create(data);

                var tcp = Reactor.Divert.Parsers.TcpHeader.Create(ip);

                Console.WriteLine("{0}:{1} -> {2}:{3}", ip.SourceAddress,
 
                                                        tcp.SourcePort,

                                                        ip.DestinationAddress,

                                                        tcp.DestinationPort);

                socket.Write(data);
            });

            Reactor.Timeout.Create(() =>
            {
                Console.WriteLine("ended");

                socket.End();

            }, 10000);
            

            Console.ReadLine();
        }
    }
}
