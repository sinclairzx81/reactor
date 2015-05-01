
using Reactor;
using Reactor.Async;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace console {

    class Program {
 
        static void Main(string[] args) {

            Reactor.Loop.Start();

            var writer = Reactor.File.Writer.Create("c:/input/not-here.txt", FileMode.OpenOrCreate);
        }
    }
}
