### reactor.fusion

reactor.fusion is a experimental UDP/TCP streaming transport. It is designed to be interchangeable with reactor.tcp sockets and enables
unique streaming scenarios where a UDP is favourable over TCP.

#### creating a server

The following will create a reactor server on port 5000

```
Reactor.Fusion.Server.Create(socket => {

	socket.Send(System.Text.Encoding.UTF8.GetBytes("hello udp"));
	
	socket.End();

}).Listen(5000);
```

#### creating a client

The following will create a client socket and connect back on localhost (to the example above)

```
var client = Reactor.Fusion.Socket.Create(5000);

client.OnConnect += () => {
	
	client.OnData += data => Console.WriteLine(data.ToString("utf8"));
	
	client.OnEnd  += () =>  Console.WriteLine("client got disconnected");
};
```

