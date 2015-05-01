using System;
using System.Collections.Generic;
using System.Text;

namespace console.tests
{
    public class UdpTests {
        public static Reactor.Async.Future Create() {
            return new Reactor.Async.Future((resolve, reject) => {
                Reactor.Udp.Stun.Hole
                    .Punch(Reactor.Udp.Socket.Create().Bind(), "stun1.l.google.com:19302")
                    .Then (result => {
                        Console.WriteLine(result);
                        resolve();
                    }).Error(reject);
            });
        }
    }
}
