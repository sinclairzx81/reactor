## reactor.divert

### network packet routing library for .net layering WinDivert.

```csharp
Reactor.Loop.Start();

var socket = Reactor.Divert.Socket.Create("(inbound or outbound) and tcp");

socket.Read(packet => {
	
	socket.Write(packet);
});
```

reactor.divert is a experimental .net binding over WinDivert user-mode packet capture-and-divert library for windows.
The library provides a C# interop to the windivert.dll, as well as lightweight wrapper to capture, filter, and optionally
forward packets fired up from kernel space, all synchronized with the reactor event loop.

### parsing packets

Reactor.Divert provides utilities for parsing packet headers. 

```csharp
Reactor.Loop.Start();

var socket = Reactor.Divert.Socket.Create("(inbound or outbound) and tcp");

socket.Read(packet => {
	
    var ip  = Reactor.Divert.Parsers.IpHeader.Create(data);

    var tcp = Reactor.Divert.Parsers.TcpHeader.Create(ip);

    Console.WriteLine("{0}:{1} -> {2}:{3}", ip.SourceAddress,
 
                                            tcp.SourcePort,

                                            ip.DestinationAddress,

                                            tcp.DestinationPort);

    socket.Write(data);
});
```

### notes

- This library does not provide the windivert.dll, implementors are expected to compile this themselves, refer to https://github.com/basil00/Divert for details.
- On windows, applications leveraging this library will need to be run as administrator.
