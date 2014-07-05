# reactor
## asynchronous evented io for .net

```csharp
Reactor.Loop.Start();

var server = Reactor.Http.Server.Create(context => {

	context.Response.Write("hello world!!");

	context.Response.End();
	
}).Listen(8080);
```

### overview

Reactor is a evented, asynchronous io and networking framework written for the Microsoft.Net, Mono, Xamarin, and Unity3D
platforms. Reactor is heavily influenced by libuv and nodejs, and aims to both mirror both their feature set, and aims
to provide easy interoperability between .net applications and real-time network services.

Reactor is specifically written to target .net applications running versions of .net as low as 2.0. Developers can 
leverage Reactor to both consume realtime network services, as well as expose realtime services of their own.

### contents

* [getting started](#getting_started)
	* [the event loop](#getting_started_event_loop)
	* [console applications](#getting_started_console_applications)
	* [windows forms applications](#getting_started_windows_forms_applications)
	* [unity3D applications](#getting_started_unity3D_applications)
* [timers](#timers)
	* [timeout](#timers_timeout)
	* [interval](#timers_interval)
* [buffers](#buffers)
* [streams](#streams)
	* [readstream](#streams_readstream)
	* [writestream](#streams_writestream)
* [files](#files)
	* [readstream](#file_readstream)
	* [writestream](#file_writestream)
* [http](#http)
	* [server](#http_server)
		* [request](#http_server_request)
		* [response](#http_server_response)
	* [requests](#http_requests)
* [tcp](#tcp)
	* [server](#tcp_server)
	* [socket](#tcp_socket)
* [udp](#udp)
	* [socket](#udp_socket)

<a name='getting_started' />
### getting_started

The following section describes setting up a Reactor application.

<a name='getting_started_event_loop' />
#### the event loop

At its core, reactor requires that users start an event loop. The reactor event loop internally synchronizes and serializes asynchronous
operations back on the main thread. The following describes recommended approaches to starting the loop.

<a name='getting_started_console_applications' />
#### console applications

The following is the recommended approach for starting the reactor event loop in a typical console application. Calling Reactor.Loop.Start()
will begin a background thread which will enumerate reactors internal event queue, and dispatch asynchronous completion callbacks to the caller. 
The in example below, we start the loop, make a request to google, and stop the loop. 

```csharp
class Program 
{
	static void Main(string [] args)
	{
		Reactor.Loop.Start();

        Reactor.Http.Request.Get("http://google.com", (exception, buffer) => {

			Console.WriteLine(buffer.ToString("utf8"));

			Reactor.Loop.Stop(); // optional
        });
	}
}
```

<a name='getting_started_windows_forms_applications' />
#### windows forms applications

When developing UI applications, handling asynchronous callbacks typically require the user to manage 
synchronization back on applications UI thread. Reactor allows developers to specify a SynchronizationContext 
when starting the event loop. With this, you can specify the UI's synchronization context, ensuring all 
asynchronous completes are synchronized back on application UI thread. The following demonstrates a simple
setup.

```csharp
public partial class Form1 : Form
{
    public Form1()
    {
        InitializeComponent();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        Reactor.Loop.Start(System.Threading.SynchronizationContext.Current);
    }

    private void button1_Click(object sender, EventArgs e)
    {
        Reactor.Http.Request.Get("http://google.com", (exception, buffer) => {

			this.textbox1.Text = buffer.ToString("utf8");
        });
    }
}
```

<a name='getting_started_unity3D_applications' />
#### unity3D applications

In Unity3D, a SynchronizationContext is not available to developers. Instead, Unity3D requires developers to 
leverage Cooroutines to orchestrate asynchrony. In these scenarios, Reactor provides a Reactor.Loop.Enumerator() 
that can be passed as a argument to StartCoroutine(). Unity3D will enumerate the Reactor event loop, achieving
the same result as running the event loop in a seperate thread.

```csharp
using UnityEngine;
using System.Collections;

public class MyGameObject : MonoBehaviour {
	
	void Start () {
		
        Reactor.Http.Request.Get("http://google.com", (exception, buffer) => {

			// ready to go!!
        });
	}
	
	void Update () {

		StartCoroutine ( Reactor.Loop.Enumerator ());
	}
}
```

<a name='timers' />
### timers

Reactor comes bundled with two timing primitives, Timeouts and Intervals. These are
fashioned after javascript setTimeout() and setInterval() respectively. 

<a name='timers_timeout' />
#### timeouts

Use the Timeout class set a delay.

```csharp
Reactor.Timeout.Create(() => {

	Console.WriteLine("this code will be run in 1 second");

}, 1000);
```

<a name='timers_interval' />
#### intervals

Use the Interval class setup a repeating interval.

```csharp
Reactor.Interval.Create(() => {

	Console.WriteLine("this code will be run every 2 seconds");

}, 2000);
```

Additionally, Intervals can be cleared...

```csharp
Reactor.Interval interval = null;

interval = Reactor.Interval.Create(() => {

	Console.WriteLine("this code will be run once");

	interval.Clear();
	
}, 2000);
```
<a name='buffers' />
### buffers

Reactor has a single buffer primitive which is used to buffer data in memory, and to act as
a container for data transmitted via a stream. The buffer contains read and write operations, 
and is type passed back on all OnData events.

```csharp
Reactor.Tcp.Server.Create(socket => {
	
	socket.OnData += data => {
	
		// data is of type Reactor.Buffer
	};

    var buffer = Reactor.Buffer.Create();

    buffer.Write(10.0f);

    buffer.Write(20.0f);

    buffer.Write(30.0f);

    socket.Write(buffer);

}).Listen(5000);
```

<a name='streams' />
### streams

Reactor aligns closely with the evented io model found in nodejs. Reactor implements IReadable, 
IWriteable, or IDuplexable interfaces across file io, tcp, http request / response, stdio etc, with 
the intent of enabling effecient, evented piping of data across transports.

```csharp
Reactor.Http.Server.Create(context => {

	var readstream = Reactor.File.ReadStream.Create("c:/video.mp4");

    context.Response.ContentLength = readstream.Length;

    context.Response.ContentType = "video/mp4";

    readstream.Pipe(context.Response);

}).Listen(8080);
```

<a name='streams_readstream' />
#### IReadable

Supports OnData, OnEnd and OnError events. As well as Pause(), Resume() and Pipe().

The following demonstrates opening a file as a readstream.

```csharp
var readstream = Reactor.File.ReadStream.Create("myfile.txt");

readstream.OnData += (data) => { 
	
	// fired when we have read data from the file system.
};

readstream.OnEnd += () => { 

	// fired when we have read to the end of the file.
};

readstream.OnError += (error) => { 
	
	// fired on error. error is of type System.Exception.
};
```

<a name='streams_writestream' />
#### IWriteable

Supports Write(), Flush() and End() operations on a underlying stream.

```csharp
var writestream = Reactor.File.WriteStream.Create("myfile.txt");

writestream.Write("hello");

writestream.Write(123);

writestream.Write(new byte[] {0, 1, 2, 3});

writestream.End();
```

<a name='files' />
### files

Reactor provides a evented abstraction for the .net type System.IO.FileStream. The following outlines its use.

<a name='files_readstream' />
#### readstream

The following creates a reactor file readstream. The example outputs its contents to the console window.

```csharp
var readstream = Reactor.File.ReadStream.Create("input.txt");

readstream.OnData += (data) => Console.Write(data.ToString("utf8"));

readstream.OnEnd  += ()     => Console.Write("finished reading");
```

<a name='files_writestream' />
#### writestream

The followinf creates a reactor file writestream. The example writes data and ends the stream when complete.

```csharp
var writestream = Reactor.File.WriteStream.Create("output.txt");

writestream.Write("hello world");

writestream.End();

```

<a name='http' />
### http

Reactor provides a evented abstraction over the http bcl classes.

<a name='http_server' />
#### server

The following will create a simple http server and listen on port 8080.

```csharp
var server = Reactor.Http.Server.Create(context => {

    context.Response.Write("hello world");

    context.Response.End();

}).Listen(8080);
```

The reactor http server passes a 'context' for each request. The context object contains Request, Response objects, which 
are in themselves, implementations of IReadable and IWritable respectively.

<a name='http_request' />
#### request

Reactor provides a evented abstraction over both HttpWebRequest and HttpWebResponse classes. 

Make a GET request.
```csharp
var request = Reactor.Http.Request.Create("http://domain.com", (response) => {

	response.OnData += (data) => Console.WriteLine(data.ToString(Encoding.UTF8));

	response.OnEnd += ()      => Console.WriteLine("the response has ended");

});

request.End(); // signals to make the request.
```
Make a POST request

```csharp
var request = Reactor.Http.Request.Create("http://domain.com", (response) => {

    response.OnData += (data) => Console.WriteLine(data.ToString(Encoding.UTF8));
        
});

byte[] postdata = System.Text.Encoding.UTF8.GetBytes("this is some data");

request.Method         = "POST";

request.ContentLength  = postdata.Length;

request.Write(postdata);

request.End();
```


<a name='tcp' />
### tcp

Reactor provides a evented abstraction over the System.Net.Sockets.Socket TCP socket.

<a name='tcp_server' />
#### server

Create a tcp socket server. The following example emits the message "hello world" to a connecting client, 
then closes the connection with End().

```csharp
Reactor.Tcp.Server.Create(socket => {

	socket.Write("hello there");
	
	socket.End();

}).Listen(5000);
```

<a name='tcp_socket' />
#### socket

Reactor tcp sockets are evented abstractions over System.Net.Socket.Socket. Tcp sockets are implementations
of type IDuplexable. The following code connects to the server in the previous example. (assumed to be both on localhost)

```csharp
var client = Reactor.Tcp.Socket.Create(5000);

client.OnConnect += () => { // wait for connection.

    client.OnData += (d) => Console.WriteLine(d.ToString("utf8"));

    client.OnEnd  += ()  => Console.WriteLine("tcp transport closed");
};
```

<a name='udp' />
### udp

Reactor provides a evented abstraction over a System.Net.Sockets.Socket for UDP sockets. The following 
demonstrates setting up two udp endpoints, and exchanging messages between both.

<a name='udp_socket' />
#### socket

The following demonstrates setting up two sockets, one to connect to the other.

```csharp
//--------------------------------------------------
// socket a: create a udp socket and bind to port. 
// on receiving a message. print to console.
//--------------------------------------------------

var a = Reactor.Udp.Socket.Create();
            
a.Bind(System.Net.IPAddress.Any, 5000);
           
a.OnMessage += (remote_endpoint, message) => {

    Console.WriteLine(System.Text.Encoding.UTF8.GetString(message));
};

//--------------------------------------------------
// socket b: create a udp socket and send message
// to port localhost on port 5000.
//--------------------------------------------------

var b = Reactor.Udp.Socket.Create();

b.Send(IPAddress.Loopback, 5000, System.Text.Encoding.UTF8.GetBytes("hello from b"));

```

<a name='threads' />
### threads

Reactor has the ability to execute threads within the applications thread pool.

<a name='threads_worker' />
#### async tasks

In the following example, a 'task' is created which accepts an integer argument, and returns a integer. Inside the 
body of the task, Thread.Sleep() is invoked to emulate some long running computation.

```csharp
var task = Reactor.Async.Task<int, int>(interval => {

    Thread.Sleep(interval); // run computation here !

    return 0;
});
```
Once the task has been created, the user can invoke the process with the following.

```csharp
task(10000, (error, result) => {

    Console.WriteLine(result);
});
```