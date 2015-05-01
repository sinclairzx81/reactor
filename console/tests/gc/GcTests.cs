using System;
using System.Collections.Generic;
using System.Text;

namespace console.tests.gc {
    public class GcTests {
        public static void Collect() {
            Reactor.Fibers.Fiber.Create(() => {
                while (true) {
                    Console.ReadLine();
                    Console.WriteLine("collecting...");
                    GC.Collect();
                }
            });
        }
    }
}
