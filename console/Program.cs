using System;
using System.Net;
using System.Net.Sockets;

namespace console {

    class Program {

        static void Main(string[] args) {
            
            Reactor.Loop.Start();
            Reactor.Http.Server.Create(context => {
                context.Response.Write("hello world");
                context.Response.End();
            }).Listen(5000);
        }
    }
}
