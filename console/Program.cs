
using Reactor.Async;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;



namespace console {

    class Program {

        static void Collect () {
            Reactor.Fibers.Fiber.Create(() => {
                while (true) {
                    Console.ReadLine();
                    Console.WriteLine("collecting...");
                    GC.Collect();
                }
            });
        }

        static void GenerateTestFile() {
            var writer = Reactor.File.Writer.Create("c:/input/input.txt", FileMode.OpenOrCreate | FileMode.Truncate);
            for (int i = 0; i < 100000; i++) {
                Console.WriteLine("write " + i);
                writer.Write("line {0}: this is a test line. this is a test line.\n", i);
            }
            writer.End();
            var start = DateTime.Now;
            writer.OnError(error => Console.WriteLine(error));
            writer.OnEnd(() => Console.WriteLine("file finished"));
            writer.OnEnd(() => Console.WriteLine(DateTime.Now - start));

        }

        static void Compare (string src, string dst) {
            Reactor.Fibers.Fiber.Create(() => {
                var hash0 = "";
                var hash1 = "";
                using (var md5 = MD5.Create()) {
                    using (var stream = File.OpenRead(src)) {
                        var info = new FileInfo(src);
                        Console.WriteLine("--------------------------");
                        Console.WriteLine("src:    " + src);
                        Console.WriteLine("length: " + info.Length);
                        hash0 = System.Convert.ToBase64String(md5.ComputeHash(stream));
                        Console.WriteLine("hash:   " + hash0);
                    }
                }
                using (var md5 = MD5.Create()) {
                    using (var stream = File.OpenRead(dst)) {
                        var info = new FileInfo(dst);
                        Console.WriteLine("--------------------------");
                        Console.WriteLine("dst:    " + dst);
                        Console.WriteLine("length: " + info.Length);
                        hash1 = System.Convert.ToBase64String(md5.ComputeHash(stream));
                        Console.WriteLine("hash:   " + hash1);
                    }
                }
                if(hash0 != hash1) throw new Exception("not the same.");
            }).Error(Console.WriteLine);
        }

        static void StunTest() {

            Reactor.Udp.Stun.Hole
                .Punch(Reactor.Udp.Socket.Create().Bind(), "stun1.l.google.com:19302")
                .Then (result => {
                    Console.WriteLine(result);
                });
        }

        static void WebTest () {
            var server = Reactor.Web.Server.Create();
            server.Get("/", context => {
                Console.Write(".");
                context.Response.Write("hello");
                context.Response.End();
            });
            server.Get("/post", context =>{
                Console.WriteLine("GET: /post");
                context.Response.ContentType = "text/html";
                var read = Reactor.File.Reader.Create("c:/input/input.html");
                read.Pipe(context.Response);                
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
                //var writer = Reactor.File.Writer.Create("c:/input/output_11.exe");
                //context.Request.Pipe(writer);
                //context.Request.OnEnd(() => {
                //    context.Response.Write("got it");
                //    context.Response.End();
                //});
                //writer.OnEnd(() => {
                //    Console.WriteLine("writer ended");
                //});
            });
            server.Get("/image", context => {
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

        static void CopyTest () {
            var src = "c:/input/input.exe";
            var dst = "c:/input/output_10.exe";
            var reader = Reactor.File.Reader.Create(src);
            var writer = Reactor.File.Writer.Create(dst, FileMode.Truncate);
            int received = 0;
            reader.Pipe(writer);
            reader.OnRead  (data  => {received += data.Length; });
            reader.OnRead  (data  => Console.WriteLine("reader: " + received + " - " + data.Length));
            reader.OnError (error => Console.WriteLine("reader: " + error));
            reader.OnEnd   (()    => Console.WriteLine("reader: ended"));
            writer.OnDrain (()    => Console.WriteLine("writer: drained"));
            writer.OnError (error => Console.WriteLine("writer: " + error));
            writer.OnEnd   (()    => Console.WriteLine("writer: ended"));
            writer.OnEnd(() => {
                Compare(src, dst);
            });
            
        }

        static void StreamTestServer(string filename) {
            var server = Reactor.Tcp.Server.Create(socket => {
                var reader = Reactor.File.Reader.Create(filename);
                reader.Pipe(socket);
                reader.OnRead  (data  => Console.WriteLine("server: reader: " + data.Length));
                reader.OnEnd   (()    => Console.WriteLine("server: reader: ended"));
                reader.OnError (error => Console.WriteLine("server: reader: " + error));
                socket.OnEnd   (()    => Console.WriteLine("server: socket: ended"));
                socket.OnDrain (()    => Console.WriteLine("server: socket: drained"));
                socket.OnError (error  => Console.WriteLine("server: socket: " + error));
            }); server.Listen(5001);
            
            

            
        }

        static void StreamTestClient(string filename, Reactor.Action complete) {
            var socket = Reactor.Tcp.Socket.Create("localhost", 5001);
            var writer = Reactor.File.Writer.Create(filename, FileMode.Truncate);
            //socket.Pause();
            socket.Pipe(writer);
            //Console.WriteLine("pausing for 5");
            //Reactor.Timeout.Create(() => {
            //    socket.Resume();
            //}, 1);


            int received = 0;
            socket.OnRead  (data  => {received += data.Length; });
            socket.OnRead  (data  => Console.WriteLine("client: socket: " + received + " - " + data.Length));
            socket.OnError (error => Console.WriteLine("client: socket: " + error));
            socket.OnEnd   (()    => Console.WriteLine("client: socket: ended"));
            writer.OnDrain (()    => Console.WriteLine("client: writer: drained"));
            writer.OnError (error => Console.WriteLine("client: writer: " + error));
            writer.OnEnd   (()    => Console.WriteLine("client: writer: ended"));
            writer.OnEnd   (complete);
        }

        static void StreamTest () {
            StreamTestServer("c:/input/input.exe");
            StreamTestClient("c:/input/output_8.exe", () => {
                Console.WriteLine("test: ended");
                Compare("c:/input/input.exe", "c:/input/output_8.exe");
            });
        }


        static void Main(string[] args) {

            Reactor.Loop.Start();
            //GenerateTestFile();
            ///CopyTest();
            //StreamTest();
            WebTest();

            var queue = Reactor.Async.Queue.Create(1);
            
            

            //var file = Reactor.File.Reader.Create("c:/input/input.txt");
            //var count = 0;
            //var received = 0;
            //file.OnReadable(() => {
            //    Console.WriteLine("data");
            //    var b = file.Read();
            //    received += b.Length;
                
            //    Console.WriteLine(b.Length);
                
                
            //    if (count < 1) { file.Unshift("1"); count++;}
            //    //file.Read(1);
                
            //});
            //file.OnEnd(() => Console.WriteLine("end"));
            //file.OnEnd(() => Console.WriteLine(received));
            
            

        }
    }
}
