
namespace console
{
    class Program
    {
        static void Main(string[] args)
        {
            Reactor.Loop.Start();

            var server = Reactor.Web.Server.Create();

            server.Get("/reactor.js", context => {

                Reactor.Web.Media.Stream.Script(context);
            });

            server.Get("/download", context => {

                Reactor.Web.Media.Stream.From(context, "c:/input/upload.mp4");
            });

            server.Post("/upload", context => {

                Reactor.Web.Media.Stream.To(context, "c:/input/upload.mp4", (exception) => {

                    if (exception != null) {

                        context.Response.StatusCode = 500;

                        context.Response.Write(exception.Message);

                        context.Response.End();

                        return;
                    }

                    context.Response.StatusCode = 200;

                    context.Response.Write("ok");

                    context.Response.End();
                });
            });

            server.Get("/", context =>
            {
                var readstream = Reactor.File.ReadStream.Create("c:/input/bstream/index.html");

                context.Response.ContentType = "text/html";

                context.Response.ContentLength = readstream.Length;

                readstream.Pipe(context.Response);
            });

            server.Listen(5000);
        }
    }
}
