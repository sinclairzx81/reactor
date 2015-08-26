using System;
using System.Collections.Generic;
using System.Net;

namespace Reactor.Examples {

    /// <summary>
    /// Simple TCP Multiplexer. 
    /// </summary>
    public class SimpleMultiplexer {

        static void Remote (int port, System.Net.IPEndPoint endpoint) {
            var sockets = new Dictionary<Guid, Reactor.Tcp.Socket>();
            Reactor.Tcp.Server.Create(tunnel => {
                tunnel.OnRead(tunnel_buffer => {
                    while (tunnel_buffer.Length > 0) {
                        var chks = tunnel_buffer.ReadByte();
                        var guid = new Guid(tunnel_buffer.ReadBytes(16));
                        var flag = tunnel_buffer.ReadByte();
                        var len  = tunnel_buffer.ReadInt32();
                        if(tunnel_buffer.Length >= len) {
                            var data   = tunnel_buffer.Read(len);
                            Reactor.Tcp.Socket socket = null;
                            if (sockets.ContainsKey(guid)) {
                                socket = sockets[guid];
                            }
                            else {
                                socket = Reactor.Tcp.Socket.Create(endpoint);
                                sockets[guid] = socket;
                                socket.OnRead(socket_buffer => {
                                    while (socket_buffer.Length > 0) {
                                        var segment = socket_buffer.Read(512 - 22);
                                        var frame   = Reactor.Buffer.Create();
                                        frame.Write(new byte[] { 42 });
                                        frame.Write(guid.ToByteArray());
                                        frame.Write(new byte[] { 1 });
                                        frame.Write(segment.Length);
                                        frame.Write(segment);
                                        tunnel.Write(frame);
                                   }
                                });
                                socket.OnError(error => {
                                    var frame = Reactor.Buffer.Create();
                                    frame.Write(new byte[] { 42 });
                                    frame.Write(guid.ToByteArray());
                                    frame.Write(new byte[] { 0 });
                                    frame.Write(0);

                                    tunnel.Write(frame);
                                    sockets.Remove(guid);
                                });
                                socket.OnEnd(() => {
                                    var frame = Reactor.Buffer.Create();
                                    frame.Write(new byte[] { 42 });
                                    frame.Write(guid.ToByteArray());
                                    frame.Write(new byte[] { 0 });
                                    frame.Write(0);

                                    tunnel.Write(frame);
                                    sockets.Remove(guid);
                                });
                            }
                            switch (flag) {
                                case 0:
                                    sockets.Remove(guid);
                                    socket.End();
                                    break;
                                case 1:
                                    socket.Write(data);
                                    break;
                            }
                        } else break;
                    }

                });
            }).Listen(port);
        }

        static void Local (int port, System.Net.IPEndPoint endpoint) {
            var sockets = new Dictionary<Guid, Reactor.Tcp.Socket>();
            var tunnel  = Reactor.Tcp.Socket.Create(endpoint);
            tunnel.OnConnect(() => {
                Reactor.Tcp.Server.Create(socket => {
                    var connectionid = Guid.NewGuid();
                    sockets[connectionid] = socket;
                    socket.OnRead(socket_buffer => {
                        while (socket_buffer.Length > 0) {
                            var segment = socket_buffer.Read(512 - 22);
                            var frame = Reactor.Buffer.Create();
                            frame.Write(new byte[] { 42 });
                            frame.Write(connectionid.ToByteArray());
                            frame.Write(new byte[] { 1 });
                            frame.Write(segment.Length);
                            frame.Write(segment);
                            tunnel.Write(frame);
                        }
                    });
                    socket.OnError(error => {
                        var frame = Reactor.Buffer.Create();
                        frame.Write(new byte[] { 42 });
                        frame.Write(connectionid.ToByteArray());
                        frame.Write(new byte[] { 0 });
                        frame.Write((int)0);
                        tunnel.Write(frame);
                        sockets.Remove(connectionid);
                    });
                    socket.OnEnd(() => {
                        var frame = Reactor.Buffer.Create();
                        frame.Write(new byte[] { 42 });
                        frame.Write(connectionid.ToByteArray());
                        frame.Write(new byte[] { 0 });
                        frame.Write((int)0);
                        tunnel.Write(frame);
                        sockets.Remove(connectionid);
                    });
                }).Listen(port);

                tunnel.OnRead(tunnel_buffer => {
                    while (tunnel_buffer.Length > 0) {
                        var chks = tunnel_buffer.ReadByte();
                        var guid = new Guid(tunnel_buffer.ReadBytes(16));
                        var flag = tunnel_buffer.ReadByte();
                        var len  = tunnel_buffer.ReadInt32();
                        if (tunnel_buffer.Length >= len) {
                            var data   = tunnel_buffer.Read(len);
                            if (sockets.ContainsKey(guid)) {
                                var socket = sockets[guid];
                                switch (flag) {
                                    case 0:
                                        sockets.Remove(guid);
                                        socket.End();
                                        break;
                                    case 1:
                                        socket.Write(data);
                                        break;
                                }
                            }
                        } else break;
                    }
                });
            });
        }

        public static void Run()
        {
            Reactor.Domain.Create(() => {
                Remote (5001, new IPEndPoint(IPAddress.Parse("0.0.0.0"), 1234));
                Local  (5000, new IPEndPoint(IPAddress.Loopback, 5001));
                Reactor.Fibers.Fiber.Create(() => {
                    while (true) {
                        Console.ReadLine();
                        GC.Collect();
                        Console.WriteLine("collecting GC");
                    }
                });
            });
        }
    }
}
