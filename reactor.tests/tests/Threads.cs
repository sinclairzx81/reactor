using System;
using System.Collections.Generic;
using System.Text;

namespace Reactor.Tests
{
    public static class Threads
    {
        class PrimeResult
        {
            public long N;
            public long P;
        }

        public static void ComputeTheNthPrime(int N)
        {
            //------------------------------
            // worker to compute nth prime
            //------------------------------
            var nth_prime = Reactor.Threads.Worker.Create<long, PrimeResult>((long n) =>
            {
                int count = 0;
                long a = 2;
                while (count < n)
                {
                    long b = 2;
                    int prime = 1;
                    while (b * b <= a)
                    {
                        if (a % b == 0)
                        {
                            prime = 0;
                            break;
                        }
                        b++;
                    }
                    if (prime > 0)
                        count++;
                    a++;
                }
                var result = new PrimeResult();
                result.N = n;
                result.P = --a;
                return result;
            });

            for (int i = 0; i < N; i++)
            {
                nth_prime(i, (exception, result) =>
                {

                    Console.WriteLine("the {0} prime is {1}", result.N, result.P);
                });
            }
        }
    }
}
