
namespace console
{
    class Program
    {
        static void Main(string[] args)
        {
            Reactor.Loop.Start();

            var redirect = Reactor.Divert.Capture.Create((packet, next) => {
                
                System.Console.WriteLine("{0}: {1} -> {2} - {3}", packet.Type, packet.Source, packet.Destination, packet.Data.Length);

                next(packet);

            }).Start();

            System.Console.ReadLine();

            redirect.Stop();

            Reactor.Loop.Stop();
        }
    }
}
