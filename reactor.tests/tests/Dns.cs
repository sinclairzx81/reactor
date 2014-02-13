using System;
using System.Collections.Generic;
using System.Text;

namespace Reactor.Tests
{
    public static class Dns
    {
        public static void LookupDomain(string hostname)
        {
            Reactor.Net.Dns.GetHostAddresses(hostname, (exception, addresses) => {

                if(exception != null) {

                    Console.WriteLine(exception);

                    return;
                }

                foreach(var n in addresses) {

                    Console.WriteLine(n);
                }
            });
        }

    }
}
