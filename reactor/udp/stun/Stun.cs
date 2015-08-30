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
using System.Net;

namespace Reactor.Udp.Stun {
    
    /// <summary>
    /// Utility for Hole Punching a UDP Socket.
    /// </summary>
    public static class Hole {
        #region Supports

        /// <summary>
        /// Specifies UDP network type.
        /// </summary>
        public enum  NatType {
            /// <summary>
            /// UDP is always blocked.
            /// </summary>
            UdpBlocked,
            /// <summary>
            /// No NAT, public IP, no firewall.
            /// </summary>
            OpenInternet,
            /// <summary>
            /// No NAT, public IP, but symmetric UDP firewall.
            /// </summary>
            SymmetricUdpFirewall,
            /// <summary>
            /// A full cone NAT is one where all requests from the same internal IP address and port are
            /// mapped to the same external IP address and port. Furthermore, any external host can send
            /// a packet to the internal host, by sending a packet to the mapped external address.
            /// </summary>
            FullCone,
            /// <summary>
            /// A restricted cone NAT is one where all requests from the same internal IP address and
            /// port are mapped to the same external IP address and port. Unlike a full cone NAT, an external
            /// host (with IP address X) can send a packet to the internal host only if the internal host
            /// had previously sent a packet to IP address X.
            /// </summary>
            RestrictedCone,
            /// <summary>
            /// A port restricted cone NAT is like a restricted cone NAT, but the restriction
            /// includes port numbers. Specifically, an external host can send a packet, with source IP
            /// address X and source port P, to the internal host only if the internal host had previously
            /// sent a packet to IP address X and port P.
            /// </summary>
            PortRestrictedCone,
            /// <summary>
            /// A symmetric NAT is one where all requests from the same internal IP address and port,
            /// to a specific destination IP address and port, are mapped to the same external IP address and
            /// port. If the same host sends a packet with the same source address and port, but to
            /// a different destination, a different mapping is used. Furthermore, only the external host that
            /// receives a packet can send a UDP packet back to the internal host.
            /// </summary>
            Symmetric
        }

        /// <summary>
        /// Stun Response.
        /// </summary>
        public class Result {
            public NatType    Nat      { get; set; }
            public IPEndPoint EndPoint { get; set; }

            #region ToString

            public override string ToString() {
                return string.Format("{0}: {1}", this.Nat, this.EndPoint);
            }

            #endregion
        }

        #endregion

        #region Request

        private static Reactor.Future<Reactor.Udp.Stun.Packet> Request (Reactor.Udp.Socket socket, System.Net.IPEndPoint endpoint, Reactor.Udp.Stun.Packet request) {
            return new Reactor.Future<Packet>((resolve, reject) => {
                var racer = new Reactor.Racer();
                /* onread */
                Action<Reactor.Udp.Message> onread = null; onread = message => {
                    racer.Set(() => {
                        socket.RemoveRead(onread);
                        try {
                            var response = new  Reactor.Udp.Stun.Packet();
                            response.Parse(message.Buffer.ToArray());
                            resolve(response);
                        } catch(Exception error) {
                            reject(error);
                        }
                    });
                };
                /* timeout */
                Reactor.Timeout.Create(() => {
                    racer.Set(() => {
                        socket.RemoveRead(onread);
                        reject(new Exception("request timed out"));
                    });
                }, 4000);

                //------------------------------------
                // request:
                //------------------------------------
                socket.OnRead (onread);
                socket.Send (Reactor.Udp.Message.Create(endpoint, request.ToArray()));
            });
        }

        #endregion

        #region Tests

        private static Reactor.Future<Reactor.Udp.Stun.Hole.Result> Test0 (Reactor.Udp.Socket socket, System.Net.IPEndPoint endpoint) {
            return new Reactor.Future<Reactor.Udp.Stun.Hole.Result>((resolve, reject) => {
                Hole.Request(socket, endpoint, new Packet { 
                    Type = PacketType.BindingRequest 
                })
                .Then(result => {
                    if (socket.LocalEndPoint.Equals(result.MappedAddress)) {
                        Test1(socket, endpoint, result.MappedAddress)
                            .Then(resolve)
                            .Error(reject);
                    } else {
						Test2(socket, endpoint, result.MappedAddress)
							.Then(resolve)
							.Error(reject);						
					}

                }).Error(error => resolve(new Result { Nat = NatType.UdpBlocked}));
            });
        }

        private static Reactor.Future<Reactor.Udp.Stun.Hole.Result> Test1 (Reactor.Udp.Socket socket, System.Net.IPEndPoint endpoint, System.Net.IPEndPoint mapped) {
            return new Reactor.Future<Reactor.Udp.Stun.Hole.Result>((resolve, reject) => {
                Hole.Request(socket, endpoint, new Packet { 
                    Type          = PacketType.BindingRequest,
                    ChangeRequest = new ChangeRequest {
                        ChangeIP   = true,
                        ChangePort = true
                    }
                })
                .Then(result => resolve(new Result { Nat = NatType.OpenInternet, EndPoint = mapped }))
                .Error(error => resolve(new Result { Nat = NatType.SymmetricUdpFirewall, EndPoint = mapped }));
            });
        }

        private static Reactor.Future<Reactor.Udp.Stun.Hole.Result> Test2 (Reactor.Udp.Socket socket, System.Net.IPEndPoint endpoint, System.Net.IPEndPoint mapped) {
            return new Reactor.Future<Reactor.Udp.Stun.Hole.Result>((resolve, reject) => {
                Hole.Request(socket, endpoint, new Packet { 
                    Type           = PacketType.BindingRequest,
                    ChangeRequest  = new ChangeRequest {
                        ChangeIP   = true,
                        ChangePort = true
                    }
                })
                .Then(result => resolve(new Result { Nat = NatType.FullCone, EndPoint = mapped }))
                .Error(error => Test3(socket, endpoint, mapped).Then(resolve).Error(reject));
            });
        }

        private static Reactor.Future<Reactor.Udp.Stun.Hole.Result> Test3    (Reactor.Udp.Socket socket, System.Net.IPEndPoint endpoint, System.Net.IPEndPoint mapped) { 
            return new Reactor.Future<Reactor.Udp.Stun.Hole.Result>((resolve, reject) => {
                Hole.Request(socket, endpoint, new Packet { 
                    Type           = PacketType.BindingRequest
                })
                .Then(result => Test3(socket, endpoint, mapped).Then(resolve).Error(reject))
                .Error(error => resolve(new Result { Nat = NatType.UdpBlocked, EndPoint = mapped }));
            });          
        }

        private static Reactor.Future<Reactor.Udp.Stun.Hole.Result> Test4    (Reactor.Udp.Socket socket, System.Net.IPEndPoint endpoint, System.Net.IPEndPoint mapped) {
            return new Reactor.Future<Reactor.Udp.Stun.Hole.Result>((resolve, reject) => {
                Hole.Request(socket, endpoint, new Packet { 
                    Type           = PacketType.BindingRequest,
                    ChangeRequest  = new ChangeRequest {
                        ChangeIP   = false,
                        ChangePort = true
                    }
                })
                .Then(result => resolve(new Result { Nat = NatType.RestrictedCone, EndPoint = mapped }))
                .Error(error => resolve(new Result { Nat = NatType.PortRestrictedCone, EndPoint = mapped }));
            });
        }

        #endregion

        #region Punch
        
        /// <summary>
        /// Attempts to punch a hole through NAT.
        /// </summary>
        /// <param name="socket">The UDP socket to punch.</param>
        /// <param name="endpoint">The STUN endpoint.</param>
        /// <param name="callback">A callback containing the results of the holepunch</param>
        public static Reactor.Future<Reactor.Udp.Stun.Hole.Result> Punch     (Reactor.Udp.Socket socket, System.Net.IPEndPoint endpoint) {
            return Test0(socket, endpoint);
        }

        /// <summary>
        /// Attempts to punch a hole through NAT.
        /// </summary>
        /// <param name="socket">The UDP socket to punch.</param>
        /// <param name="endpoint">The STUN endpoint.</param>
        /// <param name="callback">A callback containing the results of the holepunch</param>
        public static Reactor.Future<Reactor.Udp.Stun.Hole.Result> Punch (Reactor.Udp.Socket socket, string endpoint) {
            return new Reactor.Future<Reactor.Udp.Stun.Hole.Result>((resolve, reject) => {
                endpoint = endpoint.ToLower().Replace("stun:", string.Empty);
                var split = endpoint.Split(new char[] {':'});
                if (split.Length != 2) {
                    resolve(new Result { Nat = NatType.UdpBlocked} );
                    return;
                }
                var hostname = split[0];
                int port     = 0;
                if (!int.TryParse(split[1], out port)) {
                    resolve(new Result { Nat = NatType.UdpBlocked} );
                    return;
                }
                Reactor.Dns.GetHostAddresses(hostname).Then(addresses => {
                    if (addresses.Length == 0) {
                        resolve(new Result { Nat = NatType.UdpBlocked} );
                        return;
                    }
                    Punch(socket, new IPEndPoint(addresses[0], port))
                        .Then(resolve)
                        .Error(reject);
                }).Error(error => {
                    resolve(new Result { Nat = NatType.UdpBlocked} );
                });
            });
        }

        #endregion
    }
}