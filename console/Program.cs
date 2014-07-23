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

            Reactor.Http.Server.Create((context) =>
            {
                //var readstreams = Reactor.File.ReadStream.Create("c:/input/image.jpg");

                //context.Response.ContentType = "image/jpg";

                //context.Response.ContentLength = readstreams.Length;

                //readstreams.Pipe(context.Response);

                context.Response.ContentLength = 5;

                context.Response.Write("hello");

                context.Response.End();

            }).Listen(5000);

            while(true)
            {
                Console.ReadLine();

                GC.Collect();

                Console.WriteLine("collected");
            }
        }
    }
}
