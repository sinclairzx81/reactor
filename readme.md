# reactor
## asynchronous event driven io for .net

```csharp
Reactor.Loop.Start();

Reactor.Http.Server.Create(context => {
	context.Response.Write("hello world!!");
	context.Response.End();
}).Listen(8080);
```

### overview

Reactor is a event driven, non blocking, asynchronous io and networking 
framework written for Microsoft.Net, Mono, Xamarin, and Unity3D platforms. 
Reactor is heavily influenced by nodejs and libuv and aims to bring 
their event driven io model to .net based applications.

Reactor is specifically written to target .net 2.0 and up. It provides a 
variety of async primitive types for legacy platforms, while still allowing 
for tight integration with modern platform features (such as async await). 
Developers can use Reactor to consume real-time network services, as well 
as expose real-time network services of their own.

[download reactor 0.9.1](https://s3.amazonaws.com/sinclair-code/reactor-0.9.1.zip "download 0.9.1")

### contents
 * [the event loop](#the_event_loop)
	* [console applications](#console_applications)
	* [windows forms](#windows_forms)
	* [unity3D](#unity3D)
	* [posting to the loop](#posting_to_the_loop)
* [streams and buffers](#streams_and_buffers)
    * [buffers] (#streams_buffers)
	* [readble] (#streams_readable)
	* [writable](#streams_writeable)
* [files](#files)
	* [readstream](#file_readstream)
	* [writestream](#file_writestream)
* [stdio](#stdio)
	* [process](#stdio_process)
	* [current](#current_current)	
* [tcp](#tcp)
	* [server](#tcp_server)
	* [socket](#tcp_socket)
* [tls](#tls)
	* [server](#tls_server)
	* [socket](#tls_socket)
* [udp](#udp)
	* [socket](#udp_socket)		
* [http/https](#http_https)
	* [server](#http_server)
		* [request](#http_server_request)
		* [response](#http_server_response)
	* [requests](#http_requests)
* [enumerables](#enumerables)
	* [its a bit like linq](its_a_bit_like_linq)	
* [timers](#timers)
	* [timeout](#timers_timeout)
	* [interval](#timers_interval)
* [fibers](#fibers)
	* [creating fibers](#creating fibers)


<a name='getting_started' />
### getting started

The following section outlines getting started with reactor.

<a name='the_event_loop' />
#### the event loop

Reactor operates via a single event loop which is used internally 
to synchronize IO completion callbacks to a user defined synchronization 
context.

Starting a event loop is simple...

	Reactor.Loop.Start();

From a developers standpoint, once this loop has been started, it is 
ready to start processing IO events on behalf of the reactor API's 
throughout the application. 

Stopping the loop is equally straight forward.

	Reactor.Loop.Stop();

The following sections outline various ways to start the event loop for a
variety of platforms.

<a name='getting_started_console_applications' />
#### console applications

Console applications are the simplist to get going. The following will 
start the event loop in basic console application, followed by starting
a http server on port 5000.

```csharp
class Program {
	static void Main(string [] args) {
		Reactor.Loop.Start();
		
		Reactor.Http.Server.Create(context => {
			context.Response.Write("hello console!!");
			context.Response.End();
		}).Listen(8080);
	}
}
```

<a name='getting_started_windows_forms_applications' />
#### windows forms applications

Windows applications are slightly more involved. Developers familar with WinForms (or
any other UI desktop programming) will note that all threads need to be synchronized 
with the applications UI thread. Reactor provides a convenient means handling this 
synchronization by allowing the user to pass the UI thread SynchronizationContext 
to the loop at start up.

	Reactor.Loop.Start(System.Threading.SynchronizationContext.Current);

The result of this is that all IO events are now synced up with the current UI thread. The 
following illustates a more complete example, Where we start the event loop in this Forms
OnLoad(..) event. Followed by starting a http server attached to a button click event.

```csharp
public partial class Form1 : Form {
	
    public Form1() {
        InitializeComponent();
    }

    protected override void OnLoad(EventArgs e) {
        base.OnLoad(e);
        Reactor.Loop.Start(System.Threading.SynchronizationContext.Current);
    }
	
    private void button1_Click(object sender, EventArgs e) {
		Reactor.Http.Server.Create(context => {
			context.Response.Write("web servers in .net!");
			context.Response.End();
		}).Listen(5000);
    }
}
```

<a name='getting_started_unity3D_applications' />
#### unity3D applications

In Unity3D, asynchronous work is typically handled by way of coroutines. Reactor builds
on this and exposes a loop enumerator that can be passed to Unity's StartCoroutine(..)
method.

The following example starts Reactor within a Unity3D MonoBehaviour. The loop is enumerated
when ever this behaviours 'Update()' is fired. Here, we run a http server for from within
Unity3D.

```csharp
using UnityEngine;
using System.Collections;

public class MyGameObject : MonoBehaviour {
	
	void Start () {
		Reactor.Http.Server.Create(context => {
			context.Response.Write("http servers in unity3D!");
			context.Response.End();
		}).Listen(5000);
	}
	
	void Update () {
		StartCoroutine ( Reactor.Loop.Enumerator() );
	}
}
```

<a name='posting_to_the_loop' />
### posting to the loop

Internally, Reactors API's are posting to the event loop with the following call...
```csharp
Reactor.Loop.Post(() => {
	/* this code is executed in the
	 * the synchronization context
	 * passed at start up. */
});
```
Developers may wish to use the event loop to help synchronize their own threads. This 
is especially useful for UI work where many worker threads need synchronization with the
UI thread. 

Below is is a console application where we create a simple 'ticker'. The ticker is run
on a seperate thread, but is synced back to the caller from via the Loop.Post(...). Had
this example been a WinForms application, the caller (Main() in this instance) can be 
sure the thread is synchronized back accordingly, and its safe for them to display 
some message (perhaps a clock) to the user.

```csharp
class Program {
    static void Ticker(Reactor.Action ontick) {
        new Thread(() => {
            while (true) {
                /* simulate some background work. */
                Thread.Sleep(1000);

                /* call loop post to synchronize back to the caller. */
                Loop.Post(() => {
                    ontick();
                });
            }
        }).Start();
    }

    static void Main(string[] args) {
        Reactor.Loop.Start();
        Ticker(() => {
            Console.WriteLine("tick!");
        });
    }
}
```
<a name="streams_and_buffer" />
### streams and buffers

At its core, reactor is made up of three simple things, readables, writables and buffers.
Understanding these things will help developers get the most out of the library. 

- buffers  - a container where bytes live.
- readable - a event driven read interface to receive buffers (above)
- writable - a asynchronous write interface to write buffers (above)

The following section goes into detail about these three things.

<a name="buffers" />
#### buffers

Reactor.Buffer is reactors the most basic primitive for passing data around. Internally, 
the Reactor.Buffer is a implementation of a classic ring buffer, but with 
added magic such as allowing for dynamic resize. To which the developer has fine level
control.

Note: the Reactor.Buffer is modelled closely on the nodejs buffer and has similar functionality. 
See https://nodejs.org/api/buffer.html for some additional info,

From a developers standpoint, buffers can be thought of as a FIFO queue, where the 
first data written is also the first data read. heres an example.

``` csharp
var buffer = Reactor.Buffer.Create();
buffer.Write("hello world");
byte [] data = buffer.Read(5); // reads 'hello' out of the buffer.
Console.WriteLine(buffer); // prints " world"
```

Additional, buffers can be 'unshifted', the following would put 'hello' back in 
buffer.

```csharp
buffer.Unshift(data);
Console.WriteLine(buffer); // prints "hello world"
```

On top of string shifting, Reactor.Buffer also provides number overloads for writing
native .net value types. The following will write a series of numbers to the buffer
and read them back out in sequence.

```csharp
var buffer = Reactor.Buffer.Create();
for(int i = 0; i < 10; i++){
	buffer.Write(i);
}
while(buffer.Length > 0) {
	Console.Write(buffer.ReadInt32()); // prints "0123456789"
}
```

By default, Reactor will allocate an internal 64k buffer if the caller has not explicitly
said otherwise. You can override this default size as follows....

```csharp
var buffer = Reactor.Buffer.Create(5);
buffer.Write("hello"); // just enough space!
Console.WriteLine(buffer.Length);
Console.WriteLine(buffer.Capacity);
```
However, lets say if you were to continue writing, the buffer will make provisions for you
and resize in 64k chunks.

```csharp
var buffer = Reactor.Buffer.Create(5);
buffer.Write("hello"); // just enough space!
Console.WriteLine(buffer.Length);
Console.WriteLine(buffer.Capacity);
buffer.Write(" world");
Console.WriteLine(buffer.Length);
Console.WriteLine(buffer.Capacity); // whoa, too much!!
```

Of course, you can control the resize if you need, as follows.

```csharp
var buffer = Reactor.Buffer.Create(5, 5); // buffers increment in 5 byte chunks.
buffer.Write("hello"); // just enough space!
Console.WriteLine(buffer.Capacity);
Console.WriteLine(buffer.Length);
buffer.Write(" world");
Console.WriteLine(buffer.Capacity); // 15 byte capacity (all good)
Console.WriteLine(buffer.Length);   // 11 byte capacity
Console.WriteLine(buffer);          // prints "hello world"
```

<a name="readable" />
### readable

Reactor.IReadable is the interface shared amoung all things that "stream data" (files, tcp sockets, stdio, http requests etc).

Unlike a .net StreamReader, Reactor handles reading data through events, and can be thought of as
a Rx-like observable (Reactor was designed to play nicely with Rx). The following is a example is 
reading a file from disk. (Where a Reactor.File.Reader is a concrete implementation of Reactor.IReadable)

```csharp
var reader = Reactor.File.Reader("myfile.dat");
reader.OnRead  (buffer => { /* you just read a buffer from disk */ });
reader.OnError (error  => { /* something went wrong */ });
reader.OnEnd   (()     => { /* you've read all the data! */ });
```
There are a couple caveats to be aware of in the above example.
- reading happens as soon as the caller applies the 'OnRead' callback.
- 'read' events emit a new instance of a Reactor.Buffer on each 'read'.
- 'read' events will continue to read until there is no more data or error.
- if a 'error' events fires, its followed immediately by a 'end' event.
- 'end' events will 'always' fire irrespective of 'how' the stream ended (by error or success)
- 'error' and 'end' events are garenteed to trigger 'once only'.
- readable streams are non seekable, once reading begins, theres no going back!

So the take away from this is, without error, the caller can expect events to be fired in 
the following way...

	read read read read end.
	
However, in case of error, the caller can expect....

	read read read read error end. 
	
Reactor.IReadable borrows on nodejs' readstreams very heavily. And supports the usual suspects, such 
as Pause() and Resume(). The following will read the same file, Pause() for a second, then 
Resume() reading...

```csharp
var reader = Reactor.File.Reader("myfile.dat");
reader.OnRead  (buffer => { 
	reader.Pause(); // no more data please....
	Reactor.Timeout.Create(() => { 
		reader.Resume(); // ok, ready for more!!
	}, 1000);
});
reader.OnError (error  => { /* something went wrong */ });
reader.OnEnd   (()     => { /* you've read all the data! */ });
```

Reactor.IReadable also supports nodejs' streams2 non-flowing interface. The following is a example of 
reading via the 'streams2' 'readable' event.

```csharp
var reader = Reactor.File.Reader("myfile.dat");
reader.OnReadable(() => { // there is some data to be read!
	var buffer = reader.Read();  // read it all!
});
reader.OnError (error  => { /* something went wrong */ });
reader.OnEnd   (()     => { /* you've read all the data! */ });
```

The above example has the similar characteristics as streams2. They are:
- reading will begin as soon as the caller assigns a 'readable' callback.
- the readable event will fire as soon as there is data to be read.
- the readable will not read more data until the caller has "Read()" all the in the readable.
- If the readables internal buffer id drained (by calling Read()), reading will resume 
and the 'readable' event will fire again as soon as more data becomes available.

One last note, Reactor.IReadable also supports pipe-ing data. The following is a example of copying
a file through the Pipe() interface.
```csharp
var reader = Reactor.File.Reader("myfile.dat");
var writer = Reactor.File.Writer("myfile2.dat");
reader.Pipe(writer);
```
You can watch this copy in progress by attaching some additional events....
```csharp
var reader = Reactor.File.Reader("myfile.dat");
var writer = Reactor.File.Writer("myfile2.dat");
reader.Pipe(writer);
reader.OnRead(buffer => Console.WriteLine("read: {0} bytes", buffer.Length));
reader.OnEnd (()     => Console.WriteLine("read: finished!"));
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