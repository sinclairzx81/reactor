using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Reactor.Tests
{
    public static class Udp
    {
        public static void PingPong()
        {
            //----------------------------------------------
            // server
            //----------------------------------------------

            var server = Reactor.Udp.Socket.Create();

            server.Bind(IPAddress.Any, 5000);

            server.OnMessage += (endpoint, data) =>
            {
                Console.WriteLine("> ping");

                server.Send(endpoint, data);
            };


            //----------------------------------------------
            // client
            //----------------------------------------------
            var client = Reactor.Udp.Socket.Create();

            client.Bind(IPAddress.Loopback, 0);

            client.Send(IPAddress.Loopback, 5000, Encoding.UTF8.GetBytes("hello"));

            client.OnMessage += (endpoint, data) =>
            {
                Console.WriteLine("< pong");

                client.Send(endpoint, data);
            };            
       
        }

        public static void StunTest()
        {
            var socket = Reactor.Udp.Socket.Create();

            socket.Bind(IPAddress.Any, 5000);
      
            socket.Stun("stun.l.google.com", 19302, (response) => {

                Console.WriteLine("stun response:");

                Console.WriteLine("NAT: {0}", response.NatType);

                Console.WriteLine("PublicIP: {0} ", response.PublicEndPoint);
            });
        }
    }
}
