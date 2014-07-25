using System;
using System.Collections.Generic;
using System.Text;


namespace console
{
    public static class Ext
    {
        public static void Render(this Reactor.Http.ServerResponse response, string filename)
        {
            var template = Reactor.Web.Templates.Template.Create(filename);

            var buffer   = Reactor.Buffer.Create(template.Render());

            response.ContentType = "text/html";

            response.ContentLength = buffer.Length;

            response.Write(buffer);

            response.End();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Reactor.Loop.Start();

            var http = Reactor.Http.Server.Create().Listen(5000);

            var web  = Reactor.Web.Server.Create(http);

            web.Get("/", context => {

                context.Response.Render("c:/input/templates/c.html");
            });

            web.Get("/other", context =>
            {
                var readstream = Reactor.File.ReadStream.Create(System.IO.Directory.GetCurrentDirectory() + "/image.jpg");

                context.Response.ContentType = "image/jpeg";

                context.Response.ContentLength = readstream.Length;

                readstream.Pipe(context.Response);
            });

            while(true)
            {
                Console.ReadLine();
                
                GC.Collect();
            }
        }
    }
}
