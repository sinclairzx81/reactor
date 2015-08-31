using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace console {

    class ChunkReader {
        private int headersize;
        private Reactor.Buffer buffer;
        private Reactor.Event<Reactor.Buffer> onread;
        private Reactor.Func<Reactor.Buffer, int> onheader;
        public ChunkReader(int headersize, Reactor.Func<Reactor.Buffer, int> onheader) {
            this.headersize = headersize; 
            this.onheader   = onheader;
            this.buffer     = Reactor.Buffer.Create();
            this.onread     = Reactor.Event.Create<Reactor.Buffer>();
        }

        public void Write(Reactor.Buffer buffer) {
            lock (this.buffer) {
                this.buffer.Write(buffer);
                this.Process();
            }
        }

        public void OnRead(Reactor.Action<Reactor.Buffer> callback){
            this.onread.On(callback);
        }


        private void Process() {
            while (this.buffer.Length > this.headersize) {
                var count = this.buffer.ReadInt32();
                if(this.buffer.Length >= count) {
                    var content = this.buffer.Read(count);
                    this.onread.Emit(Reactor.Buffer.Create(content));
                }
                else {
                    this.buffer.Unshift(count);
                    break;
                }
            }
        }
    }

    class Program {

        static Reactor.Buffer CreateChunk(Reactor.Buffer buffer) {
            var result = Reactor.Buffer.Create();
            result.Write(buffer.Length);
            result.Write(buffer);
            return result;
        }
        

        static void Main(string[] args) {

            Reactor.Loop.Start();
            var reader = new ChunkReader(4, header => {
                return header.ReadInt32();
            });


            reader.OnRead(data => {
                var t = Reactor.Tuple.Create(10, data);
                Console.WriteLine(t);
            });
            reader.Write(CreateChunk(Reactor.Buffer.Create("this is chunk 1")));
            var second = CreateChunk(Reactor.Buffer.Create("this is chunk 2"));

            while (second.Length > 0) {
                reader.Write(Reactor.Buffer.Create(second.Read(1)));
            }
            
        }
    }
}
