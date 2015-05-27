using System;
using System.Collections.Generic;
using System.Net;

namespace Reactor.Examples {

    /// <summary>
    /// Simple TCP Multiplexer. 
    /// </summary>
    public class SimpleMultiplexer {

        /// <summary>
        /// Run at the remote endpoint.
        /// </summary>
        /// <param name="port"></param>
        /// <param name="endpoint"></param>
        static void Remote (int port, System.Net.IPEndPoint endpoint) {
            var sockets = new Dictionary<Guid, Reactor.Tcp.Socket>();
            var buffer  = Reactor.Buffer.Create();
            Reactor.Tcp.Server.Create(proxy => {
                proxy.OnRead(proxy_buffer => {
                    var chks = proxy_buffer.ReadByte();
                    var guid = new Guid(proxy_buffer.ReadBytes(16));
                    var flag = proxy_buffer.ReadByte();
                    Reactor.Tcp.Socket socket = null;
                    if (sockets.ContainsKey(guid)) {
                        socket = sockets[guid];
                    }
                    else {
                        socket = Reactor.Tcp.Socket.Create(endpoint.Address, endpoint.Port);
                        sockets[guid] = socket;
                        socket.OnRead(socket_buffer => {
                            socket.Pause();
                            while (socket_buffer.Length > 0) {
                                buffer.Write(new byte[] { 42 });
                                buffer.Write(guid.ToByteArray());
                                buffer.Write(new byte[] { 1 });
                                buffer.Write(socket_buffer.Read(4096 - 18));
                                proxy.Write(buffer);
                                buffer.Clear();
                            }
                            proxy.OnceDrain(socket.Resume);
                        });
                        socket.OnError(error => {
                            buffer.Write(new byte[] { 42 });
                            buffer.Write(guid.ToByteArray());
                            buffer.Write(new byte[] { 0 });
                            proxy.Write(buffer);
                            sockets.Remove(guid);
                            buffer.Clear();
                        });
                        socket.OnEnd(() => {
                            buffer.Write(new byte[] { 42 });
                            buffer.Write(guid.ToByteArray());
                            buffer.Write(new byte[] { 0 });
                            proxy.Write(buffer);
                            sockets.Remove(guid);
                            buffer.Clear();
                        });
                    }
                    switch (flag) {
                        case 0:
                            sockets.Remove(guid);
                            socket.End();
                            break;
                        case 1:
                            socket.Write(proxy_buffer);
                            break;
                    }
                });
            }).Listen(port);
        }

        /// <summary>
        /// Run at the local endpoint. Connects to the remote endpoint.
        /// </summary>
        /// <param name="port"></param>
        /// <param name="endpoint"></param>
        static void Local  (int port, System.Net.IPEndPoint endpoint) {
            var sockets = new Dictionary<Guid, Reactor.Tcp.Socket>();
            var proxy   = Reactor.Tcp.Socket.Create(endpoint.Address, endpoint.Port);
            var buffer  = Reactor.Buffer.Create();
            proxy.OnConnect(() => {
                Reactor.Tcp.Server.Create(socket => {
                    var connectionid = Guid.NewGuid();
                    sockets[connectionid] = socket;
                    socket.OnRead(socket_buffer => {
                        socket.Pause();
                        while (socket_buffer.Length > 0) {
                            buffer.Write(new byte[] { 42 });
                            buffer.Write(connectionid.ToByteArray());
                            buffer.Write(new byte[] { 1 });
                            buffer.Write(socket_buffer.Read(4096 - 18));
                            proxy.Write(buffer);
                            buffer.Clear();
                        }
                        proxy.OnceDrain(socket.Resume);
                    });
                    socket.OnError(error => {
                        buffer.Write(new byte[] { 42 });
                        buffer.Write(connectionid.ToByteArray());
                        buffer.Write(new byte[] { 0 });
                        proxy.Write(buffer);
                        sockets.Remove(connectionid);
                        buffer.Clear();
                    });
                    socket.OnEnd(() => {
                        buffer.Write(new byte[] { 42 });
                        buffer.Write(connectionid.ToByteArray());
                        buffer.Write(new byte[] { 0 });
                        proxy.Write(buffer);
                        sockets.Remove(connectionid);
                        buffer.Clear();
                    });
                }).Listen(port);

                proxy.OnRead(proxy_buffer => {
                    var chks = proxy_buffer.ReadByte();
                    var guid = new Guid(proxy_buffer.ReadBytes(16));
                    var flag = proxy_buffer.ReadByte();
                    Console.WriteLine(guid);
                    if (sockets.ContainsKey(guid)) {
                        var socket = sockets[guid];
                        switch (flag) {
                            case 0:
                                sockets.Remove(guid);
                                socket.End();
                                break;
                            case 1:
                                socket.Write(proxy_buffer);
                                break;
                        }
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
