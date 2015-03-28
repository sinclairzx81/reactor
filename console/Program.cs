
using Reactor;
using System;
using System.IO;

namespace console
{
    class Program
    {
        static void Main(string[] args) {

            Reactor.Loop.Start();

            var throttle = new Reactor.Throttle(1);

            throttle.Run<int, int>((arg) => new Future<int>((resolve, reject) => {

                resolve(10);
            
            }), 10);

            var readstream  = new Reactor.File.ReadStream2("c:/input/input.exe");

            var writestream = new Reactor.File.WriteStream2("c:/input/output.exe");

            readstream.Pipe(writestream);

            readstream.Read(buffer => Console.WriteLine(buffer.Length));

            readstream.Pipe(writestream);

            readstream.End(() => Console.WriteLine("end"));
        }
    }
}
