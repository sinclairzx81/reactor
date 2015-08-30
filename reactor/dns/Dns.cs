/*--------------------------------------------------------------------------

Reactor

The MIT License (MIT)

Copyright (c) 2015 Haydn Paterson (sinclair) <haydn.developer@gmail.com>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

---------------------------------------------------------------------------*/

using System;

namespace Reactor {

    /// <summary>
    /// Provides simple domain name resolution.
    /// </summary>
    public static class Dns {

        /// <summary>
        /// Asynchronously returns the internet protocol (IP) address for the specified host.
        /// </summary>
        /// <param name="hostname">The host name or ip address to resolve.</param>
        public static Reactor.Future<System.Net.IPAddress[]> GetHostAddresses(string hostname) {
            if (hostname == "localhost") hostname = "127.0.0.1";
            return new Reactor.Future<System.Net.IPAddress[]>((resolve, reject) => {
                try {
                    System.Net.Dns.BeginGetHostAddresses(hostname, result => {
                        Loop.Post(() => {
                            try {
                                var addresses = System.Net.Dns.EndGetHostAddresses(result);
                                resolve(addresses);
                            }
                            catch (Exception error) {
                                reject(error);
                            }
                        });
                    }, null);
                }
                catch (Exception error) {
                    reject(error);
                }
            });
        }

        /// <summary>
        /// Asynchronously resolves an IP address to a System.Net.IPHostEntry instance.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static Reactor.Future<System.Net.IPHostEntry> GetHostEntry(System.Net.IPAddress address) {
            return new Reactor.Future<System.Net.IPHostEntry>((resolve, reject) => {
                try {
                    System.Net.Dns.BeginGetHostEntry(address, result => {
                        Loop.Post(() => {
                            try {
                                var host_entry = System.Net.Dns.EndGetHostEntry(result);
                                resolve(host_entry);
                            }
                            catch (Exception error) {
                                reject(error);
                            }
                        });
                    }, null);
                }
                catch (Exception error) {
                    reject(error);
                }
            });
        }

        /// <summary>
        /// Asynchronously resolves an IP address to a System.Net.IPHostEntry instance.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static Reactor.Future<System.Net.IPHostEntry> GetHostEntry(string hostNameOrAddress) {
            return new Reactor.Future<System.Net.IPHostEntry>((resolve, reject) => {
                try {
                    System.Net.Dns.BeginGetHostEntry(hostNameOrAddress, result => {
                        Loop.Post(() => {
                            try {
                                var host_entry = System.Net.Dns.EndGetHostEntry(result);
                                resolve(host_entry);
                            }
                            catch (Exception error) {
                                reject(error);
                            }
                        });
                    }, null);
                }
                catch (Exception error) {
                    reject(error);
                }
            });
        }
    }
}
