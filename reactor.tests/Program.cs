using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Reactor.Tests
{
    class Program
    {


        static void Main(string [] args) 
        {
            Console.BufferHeight = 4000;

            Reactor.Loop.Start();

            long count = 0;

            var client = Reactor.Fusion.Socket.Create("192.168.1.8", 5000);

            client.OnData += (data) =>
            {
                Console.WriteLine(count + ":" + data.Length);
                count++;
                
            };

            client.OnEnd += () => Console.WriteLine(count);




        }
    }
}
