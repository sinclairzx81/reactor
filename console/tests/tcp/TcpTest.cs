using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace console.tests {
    public static class TcpTest {
        public static void IrcProxyTest() {
            Reactor.Tcp.Server.Create(socket => {
                var remote = Reactor.Tcp.Socket.Create("irc.freenode.org", 6667);
                remote.OnRead  (data  => socket.Write(data));
                remote.OnRead  (Console.WriteLine);
                remote.OnError (error => socket.End());
                remote.OnEnd   (()    => socket.End());
                socket.OnRead  (data  => remote.Write(data));
                socket.OnRead  (Console.WriteLine);
                socket.OnError (error => remote.End());
                socket.OnEnd   (()    => remote.End());
            }).Listen(6667);
        }

        public static void StreamTestServer(string filename) {
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

        public static void StreamTestClient(string filename, Reactor.Action complete) {
            var socket = Reactor.Tcp.Socket.Create("localhost", 5001);
            var writer = Reactor.File.Writer.Create(filename, FileMode.Truncate);
            socket.Pipe(writer);
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

        public static void StreamTest () {
            StreamTestServer("c:/input/input.exe");
            StreamTestClient("c:/input/output_8.exe", () => {
                Console.WriteLine("test: ended");
                console.tests.FileTests.Compare("c:/input/input.exe", "c:/input/output_8.exe");
            });
        }
    }
}
