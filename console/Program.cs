
using System;

namespace console {

    class Program {
        static void Main(string[] args) {
            Reactor.Loop.Start();
            Reactor.Loop.Catch(Console.WriteLine);
            var reader = Reactor.File.Reader.Create("c:/input/video.mp4");
            var writer = Reactor.File.Writer.Create("c:/input/video2.mp4");
            writer.OnError(Console.WriteLine);
            reader.OnError(Console.WriteLine);
            reader.Pipe(writer);
            reader.OnData(data => {
                Console.WriteLine(data.Length);
            });
        }
    }
}
