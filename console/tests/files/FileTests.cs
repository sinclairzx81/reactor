using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace console.tests
{
    public static class FileTests {

        public static Reactor.Async.Future Compare (string src, string dst) {
            return Reactor.Fibers.Fiber.Create(() => {
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
                if (hash0 != hash1) Console.WriteLine("not the same.");
            });
        }

        public static Reactor.Async.Future GenerateTestFile (string filename, int lines) {
            return new Reactor.Async.Future((resolve, reject) => {
                var writer = Reactor.File.Writer.Create(filename, FileMode.OpenOrCreate | FileMode.Truncate);
                for (int i = 0; i < lines; i++) {
                    Console.WriteLine("write " + i);
                    writer.Write("<img src='/image?i={0}' width='32' height='32' />\n", i);
                }
                writer.End();
                var start = DateTime.Now;
                writer.OnError(reject);
                writer.OnError(error => Console.WriteLine(error));
                writer.OnEnd(resolve);
                writer.OnEnd(() => Console.WriteLine("file finished"));
                writer.OnEnd(() => Console.WriteLine(DateTime.Now - start));
            });
        }

        public static Reactor.Async.Future UnshiftFileData (string filename) {
            return Reactor.Async.Future.Create((resolve, reject) => {
                var read = Reactor.File.Reader.Create(filename);
                read.OnReadable(() => {
                    Console.WriteLine("readable");
                    Console.WriteLine(read.Read().Length);
                    read.Unshift("hello");
                });
                read.OnReadable(() => {
                    Console.WriteLine(read.Read().Length);
                    Console.WriteLine("readable 2");
                });
                read.OnEnd(() => Console.WriteLine("ended"));
                read.OnError(reject);
                read.OnEnd(resolve);
            });
        }

        public static Reactor.Async.Future CopyTest (string src, string dst) {
            return new Reactor.Async.Future((resolve, reject) => {
                var reader = Reactor.File.Reader.Create(src);
                var writer = Reactor.File.Writer.Create(dst, FileMode.OpenOrCreate  );
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
                    Compare(src, dst).Then(resolve).Error(reject);
                });
            });

        }
    }
}
