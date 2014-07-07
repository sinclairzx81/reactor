# reactor web sockets

The reactor web sockets project is a RFC6455 compatible web socket transport for both clients and servers. This page
documents its use.

This page outlines its use.

* [creating a web socket server](#creating_a_web_socket_server)

* [creating a web socket server over http](#creating_a_web_socket_server_over_http)

* [authorizing web socket connections](#authorizing_web_socket_connections)

* [creating a web socket client (csharp)](#creating_a_web_socket_client_csharp)

* [creating a web socket client (javascript)](#creating_a_web_socket_client_javascript)

* [creating a web socket client (nodejs)](#creating_a_web_socket_client_nodejs)

<a name="creating_a_web_socket_server" />
### creating a web socket server

The following code will create a web socket server. The web socket server will be accessible at on  
ws://localhost:5000 on the local machine. 

```csharp
var wsserver = Reactor.Web.Sockets.Server.Create(5000, (socket) =>
{
	Console.WriteLine("server got connection");

	socket.Send("hello from server");

	socket.OnMessage += (message) =>
	{
		Console.WriteLine("server received: " + message.Data);

		socket.Close();
	};
});
```

<a name="creating_a_web_socket_server_over_http" />
### creating a web socket server over http

The web socket server is built on top of the reactor http stack, and as such, it is possible bind a web socket server
over a standard reactor.Http.Server. In the example below, we create a http server on port 5000 which will respond 
with index.html.

```csharp
var server = Reactor.Http.Server.Create((context) =>
{
	context.Response.ContentType = "text/html";

	var readstream = Reactor.File.ReadStream.Create("index.html");

	readstream.Pipe(context.Response);
});

server.Listen(5000);
```
Below is setting up the web socket server to extend the http server above. The following code will set up a web socket
endpoint accessible at http://localhost:5000/mywebsocket

```csharp
//-----------------------------------------
// setup web socket server
//-----------------------------------------
var wsserver = Reactor.Web.Sockets.Server.Create(server, "/mywebsocket", (socket) =>
{
	Console.WriteLine("server got connection");

	socket.Send("hello from server");

	socket.OnMessage += (message) =>
	{
		Console.WriteLine("server received: " + message.Data);

		socket.Close();
	};
});
```

### authorizing web socket connection
<a name="authorizing_web_socket_connections" />

The web socket server exposes the web socket handshake as a callback. Developer can use this to authenticate or reject
new web socket connections. Below is a example of setting up a auth handshake for this socket. The callback will pass the
underlying http web context and a callback delegate. Passing false on the callback results in a 401 unauthorized http response
from the web socket endpoint.

```csharp

//-----------------------------------------
// run web socket server on http server.
//-----------------------------------------

var wsserver = Reactor.Web.Sockets.Server.Create(5000, (socket) => {

	Console.WriteLine("server got connection");

	socket.Send("hello from server");

	socket.OnMessage += (message) =>
	{
		Console.WriteLine("server received: " + message.Data);

		socket.Close();
	};
});

//-----------------------------------------
// callback to authorize request.
//-----------------------------------------

wsserver.OnHandshake = (context, next) => {

	Console.WriteLine("authorizing web socket connection");

	bool authorized = true;

	if (authorized)
	{
		next(true, null);

		return;
	}

	next(false, "unable to authorize request");
};
```

<a name="creating_a_web_socket_client_csharp" />

### creating a web socket client (csharp)

The following will create a web socket client which will connect to the server created above.

```csharp
//-----------------------------------------
// create web socket client
//-----------------------------------------

var client = Reactor.Web.Sockets.Socket.Create("http://localhost:5000/");

client.OnOpen += () =>
{
	Console.WriteLine("client connected");
	
	client.OnMessage += (message) =>
	{
		Console.WriteLine("client received: " + message.Data);

		client.Send("hello from client");
	};
};

client.OnError += (err) => Console.WriteLine(err);

client.OnClose += ()    => Console.WriteLine("client disconnected");
```
<a name="creating_a_web_socket_client_javascript" />
### creating a web socket client (javascript)

The following will create a web socket client which will connect to the server created above.

```javascript
<!DOCTYPE html>
<html>
	
	<head>
	
		<script type="text/javascript">

		var socket = new WebSocket('ws://localhost:5000');

		socket.onopen = function() {
			
			socket.onmessage = function(e) {
			
				console.log("client received: " + e.data);
			
				socket.Send("hello from client");
			}
			
			socket.send('hello from client')
		}

		socket.onclose = function() {
			
			console.log('client disconnected')
		}

		socket.onerror = function(e) {
			
			console.log(e)
		}
		</script>
		
	</head>
	
	<body></body>
	
</html>
```

<a name="creating_a_web_socket_client_nodejs" />
### creating a web socket client (nodejs)

The following will create a web socket client which will connect to the server created above. We use the ws module 
for the client.

To install
```
npm install ws
```

```javascript

var WebSocket = require('ws')

var socket    = new WebSocket('ws://localhost:5000/')

socket.on('open', function() {

	socket.on('message', function(message) {
	
		console.log('the client received: %s' + message);
		
		socket.send('hello from client')
		
	});
});

socket.on('close', function() {

	console.log('client disconnected')
})

socket.on('error', function(e) {

	console.log(e)
})

```