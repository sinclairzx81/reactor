using System;
using System.Collections.Generic;
using System.Text;

namespace console.tests.web
{
    class WebTest {
        public static void WebServerTest () {
            var server = Reactor.Web.Server.Create();
            server.Get("/", context => {
                Console.Write(".");
                context.Response.Write("hello");
                context.Response.End();
            });
            server.Get("/end", context => {
                context.Request.OnRead(data => {});
                context.Request.OnEnd(() => {
                    context.Response.Write("got on end event.");
                    context.Response.End();
                });
            });
            server.Get("/post", context =>{
                Console.WriteLine      ("GET: /post");
                context.Request.OnRead (data =>  Console.WriteLine(data));
                context.Request.OnEnd  (()  => Console.WriteLine("post: ended"));
                context.Response.ContentType = "text/html";
                var read = Reactor.File.Reader.Create("c:/input/input.html");
                
                read.Pipe(context.Response);                
                read.OnEnd(() => Console.WriteLine("read: finished sending"));
            });
            int count = 0;
            server.Post("/post", context => {
                Console.WriteLine("POST: /post");
                context.Request.OnRead(data =>{
                    count+=data.Length;
                    Console.WriteLine(count);
                });
                context.Request.OnEnd(() => {
                    context.Response.Write("got it");
                    context.Response.End();
                });
            });
            server.Get("/image", context => {
                Console.Write("-");
                context.Response.ContentType = "image/png";
                var read = Reactor.File.Reader.Create("c:/input/input.png");
                read.Pipe(context.Response);
            });
            server.Get("/video", context =>{
                context.Response.ContentType = "video/mp4";
                var read = Reactor.File.Reader.Create("c:/input/input.mp4");
                read.Pipe(context.Response);
            });
            server.Listen(5000);
        }
    }
}
