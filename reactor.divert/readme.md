## reactor.divert

### network packet redirect library for .net layering WinDivert.

```csharp
Reactor.Loop.Start();

Reactor.Divert.Capture.Create((packet, next) => {
	
	//------------------------------------------
	// inspect the packet
	//------------------------------------------
    System.Console.WriteLine("{0}: {1} -> {2} - {3}", packet.Type, 
												      packet.Source, 
													  packet.Destination, 
													  packet.Data.Length);
	//------------------------------------------
	// optionally call next() to reinject packet
	//------------------------------------------

    next(packet);

}).Start();
```

reactor.divert is a experimental .net binding over WinDivert user-mode packet capture-and-divert library for windows.
The library provides a C# interop to the windivert.dll, as well as lightweight wrapper to capture, filter, and optionally
forward packets fired up from kernel space, all synchronized with the reactor event loop.

### notes

- This library does not provide the windivert.dll, implementors are expected to compile this themselves, refer to https://github.com/basil00/Divert for details.
- On windows, applications leveraging this library will need to be run as administrator.
