
using System;

namespace console
{
    class Program
    {
        static void Main(string[] args) {

            Reactor.Domain.Create(() => {

                var server = Reactor.Web.Server.Create();
                
                server.Get("/", context => {

                    context.Response.Write("hi there");

                    context.Response.End();
                });

                server.Listen(5000);
            });

            Reactor.Domain.Create(() => 
                    
                Reactor.Interval.Create(() => 
                        
                    Console.Write("A"), 1));

            Reactor.Domain.Create(() =>

                Reactor.Interval.Create(() =>

                    Console.Write("B"), 1));

            Reactor.Domain.Create(() =>

                Reactor.Interval.Create(() =>

                    Console.Write("C"), 1));

            Console.ReadLine();
        }
    }
}
