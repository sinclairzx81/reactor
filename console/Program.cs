
namespace console
{
    class Program
    {
        static void Main(string[] args)
        {
            Reactor.Loop.Start();

            var server = Reactor.Web.Socket.Server.Create(5000);

            server.OnSocket = socket =>
            {
                System.Console.Write(".");

                //Reactor.Interval.Create(() =>
                //{
                //    socket.Send("message");

                //}, 1);

                socket.OnMessage += message =>
                {
                    System.Console.Write("e");

                    socket.Send(message.RawData);
                };

                socket.OnClose += () =>
                {
                    System.Console.WriteLine("close");

                };
            };

            server.OnError = (error) =>
            {
                System.Console.Write(error);

            };
        }
    }
}
