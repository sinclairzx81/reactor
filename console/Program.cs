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

            var readstream = Reactor.File.ReadStream.Create("c:/input/input.mp4");

            var writestream = Reactor.File.WriteStream.Create("c:/input/output.mp4");

            readstream.Pipe(writestream);

            readstream.OnData += (d) =>
            {
                Console.Write(".");

            };

            readstream.OnEnd += () =>
            {
                Console.WriteLine("end");

            };
        }
    }
}
