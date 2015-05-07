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

### Contents
* [The Event Loop](#the_event_loop)
	* [Console Applications](#console_applications)
	* [Windows Forms](#windows_forms)
	* [Unity3D](#unity3D)
	* [Threads and Loops](#threads_and_loops)
* [Streams and Buffers](#streams_and_buffers)
	* [Reactor.Buffer] (#streams_buffers)
	* [Reactor.IReadable] (#streams_readables)
	* [Reactor.IWritable](#streams_writeables)
* [Files](#files)
	* [Reactor.File.Reader](#file_readstream)
	* [Reactor.File.Writer](#file_writestream)
* [Stdio](#stdio)
	* [Reactor.Process.Current](#stdio_process)
	* [Reactor.Process.Process](#current_current)	
* [Tcp](#tcp)
	* [Reactor.Tcp.Server](#tcp_server)
	* [Reactor.Tcp.Socket](#tcp_socket)
* [Tls](#tls)
	* [Reactor.Tls.Server](#tls_server)
	* [Reactor.Tls.Socket](#tls_socket)
* [Udp](#udp)
	* [Reactor.Udp.Socket](#udp_socket)
* [Ip](#ip)
	* [Reactor.Ip.Socket](#ip_socket)		
* [Http/Https](#http_https)
	* [Reactor.Http.Server](#http_server)
		* [Reactor.Http.ServerRequest](#http_server_request)
		* [Reactor.Http.ServerResponse](#http_server_response)
	* [requests](#http_requests)
* [timers](#timers)
	* [Reactor.Timeout](#timers_timeout)
	* [Reactor.Interval](#timers_interval)	
* [fibers](#fibers)
	* [creating fibers](#creating fibers)
* [async](#async)
	* [Reactor.Async.Future](#async_future)
	* [Reactor.Async.Event](#async_event)
	* [Reactor.Async.Queue](#async_queue)
	* [Reactor.Async.Racer](#async_racer)
* [enumerables](#enumerables)
	* [Reactor.Enumerable](#enumerables_enumerable)	


<a name='getting_started' />
### Getting Started

The following section outlines getting started with reactor.

<a name='the_event_loop' />
#### Reactor.Loop

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

<a name='console_applications' />
#### Console Applications

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

<a name='windows_forms' />
#### Windows Forms

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

<a name='unity3D' />
#### Unity3D

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

<a name='threads_and_loop' />
### Threads and Loops

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
<a name="streams_and_buffers" />
### Streams and Buffers

At its core, reactor is made up of three simple things, readables, writables and buffers.
Understanding these things will help developers get the most out of the library. 

- buffers  - a container where bytes live.
- readable - a event driven read interface to receive buffers (above)
- writable - a asynchronous write interface to write buffers (above)

The following section goes into detail about these three things.

<a name="streams_buffers" />
#### Reactor.Buffer

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

<a name="streams_readables" />
### Reactor.IReadable

Reactor.IReadable is the interface shared amoung all things that "stream data" (files, tcp sockets, stdio, http requests etc).

Unlike a .net StreamReader, Reactor handles reading data through events, and can be thought of as
a Rx-like observable (Reactor was designed to play nicely with Rx). The following is a example is 
reading a file from disk. (Where a Reactor.File.Reader is a concrete implementation of Reactor.IReadable)

```csharp
var reader = Reactor.File.Reader.Create("myfile.dat");
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
var reader = Reactor.File.Reader.Create("myfile.dat");
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
var reader = Reactor.File.Reader.Create("myfile.dat");
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
var reader = Reactor.File.Reader.Create("myfile.dat");
var writer = Reactor.File.Writer.Create("myfile2.dat");
reader.Pipe(writer);
```
You can watch this copy in progress by attaching some additional events....
```csharp
var reader = Reactor.File.Reader.Create("myfile.dat");
var writer = Reactor.File.Writer.Create("myfile2.dat");
reader.Pipe(writer);
reader.OnRead(buffer => Console.WriteLine("read: {0} bytes", buffer.Length));
reader.OnEnd (()     => Console.WriteLine("read: finished!"));
```
<a name="streams_writables" />
### Reactor.IWritable

The Reactor.IWritable interface is shared among all things that 'write' in a streaming way. (files, tcp sockets, stdio,
http responses etc). 

Reactor.IWritable provides a 'asynchronous' write interface over these streams, but can be called synchronous like way. 
Like node writable streams, Reactor.IWritables will internally 'queue' data submitted to be written, yet provide hooks to the 
caller to indicate 'when exactly' that has been written.

Reactor.IWritables are simple in nature. When you create a instance of one, you are given three methods to work with, these
are.

- Write(buffer) - writes this buffer to the stream.
- Flush()       - Flushes any data residient in the writables internal buffer.
- End()         - Ends the write stream.

The following creates a text file with some contents.

```csharp
var writer = Reactor.File.Writer.Create("myfile.dat");
for(var i = 0; i < 100; i++) {
	writer.Write("this is line {0}\n", i);
}
writer.End();
```

It is important to note, that the above code will likely buffer many of those lines being written (CPU's work faster
than disks as it turns out), and the caller can not expect the data to be written immediately. Because of this, Reactor
writeables all provode Reactor.Async.Future primitive (which is similar in nature to Task<T>) to help the caller learn of 
when this data has been written successfully. 

The following example adds a bit more fluff to the previous example.

```csharp
var writer = Reactor.File.Writer.Create("myfile.dat");
for(var i = 0; i < 100; i++) {
    writer.Write("this is line {0}\n", i)
          .Then(()     => Console.WriteLine("data written"))
		  .Error(error => Console.WriteLine("oh no"));
}
writer.End()
      .Then(() => Console.WriteLine("stream ended"));
	  .Error(error => Console.WriteLine("oh no"));
```
In addition to being able to keep track of single writes (as they happen). IWritable also provides 'events' 
that achieve a similar result, but from top down perspective. The following code will output the exact same thing 
as in the previous example, but we don't learn of which write completed when.

```csharp`
var writer = Reactor.File.Writer.Create("myfile.dat");
for(var i = 0; i < 100; i++) {
	writer.Write("this is line {0}\n", i);
}
writer.End();
/* attach events instead. */
writer.OnDrain(()    => Console.WriteLine("data written"));
writer.OnError(error => Console.WriteLine("oh no"));
writer.OnEnd  (()    => Console.WriteLine("stream ended."));
```

Sometimes, you want to hold off writing on a stream and just let things buffer up. Reactor.IWritable also 
supports the Cork() and Uncork() interfaces found nodes writable streams.

```csharp
var writer = Reactor.File.Writer.Create("c:/input/myfile.dat");
writer.Cork(); // buffer up the writes!!
for(var i = 0; i < 100; i++) {
    writer.Write("this is line {0}\n", i);
}
writer.End();
writer.OnDrain(()    => Console.WriteLine("data written"));
writer.OnError(error => Console.WriteLine("oh no"));
writer.OnEnd  (()    => Console.WriteLine("stream ended."));
Reactor.Timeout.Create(() => {
    writer.Uncork(); // let them fly!!
}, 2000);
```

note: Reactor has a slightly different take on Cork/Uncork. In nodejs, A writable stream is automatically
'uncorked' as soon as writable.end() is called on that stream. In contrast, Reactor requires the caller 
to specifically 'uncork' the stream. The author feels that implicit and automatic behaviour is generally
something to be avoided whereever possible. Remember to Uncork() your streams !!

While Reactor.IWritable streams are very simple in nature, there are some common mistakes that developers may 
face when working against a asynchronous write interface like this. Consider the following example...

```csharp
Reactor.Tcp.Server.Create(socket => {
	var reader = Reactor.File.Reader.Create("EXTREMELY_LARGE_FILE.DAT");
	reader.OnRead (buffer => socket.Write(buffer));
	reader.OnEnd  (()     => socket.End());
});
```

In this scenario, we have a socket (lets assume it originated from mars), and a reader reading from disk. 
The problem here is that this "EXTREMELY_LARGE_FILE.DAT" is going to be "read" MUCH quicker than the socket 
can deliever that data to mars. The end result is that the program is going to "buffer up" the entirety of 
this file in while it slowly pangs packets into space. Obviously this is not desirable.

The author recommends that users Pipe() data in this scenario whenever possible. The following would be 
more appropriate. (and simpler)

```csharp
Reactor.Tcp.Server.Create(socket => {
	var reader = Reactor.File.Reader.Create("LARGEFILE.DAT");
	reader.Pipe(socket);
});
```

Internally, Reactor is interleaving reads and writes. Below is a stock implementation of Reactors
Pipe() function....

```csharp
public Reactor.IReadable Pipe (Reactor.IWritable writable) {
    this.OnRead(data => {
        this.Pause();
        writable.Write(data)
                .Then(this.Resume)
                .Error(this._Error);
    });
    this.OnEnd (() => writable.End());
    return this;
}
```
For reference, this exact Pipe() function is common amoungst all Reactor Streams. The points of 
interest are happening within the OnRead(() => {...}) callback, which can be read as.....

	read() -> pause() -> write() -> resume() -> repeat...

Developers are free to experiment with their own implementations for Pipe(), but the defacto
Pipe() is a pretty vanilla implementation for developers to reference to write specialized pipes 
for their needs...

<a name='files' />
### files

Reactor provides a evented abstraction for the .net type System.IO.FileStream. The following outlines its use.

<a name='files_reader' />
#### Reactor.File.Reader

The following creates a reactor file readstream. The example outputs its contents to the console window.

```csharp
var readstream = Reactor.File.ReadStream.Create("input.txt");

readstream.OnData += (data) => Console.Write(data.ToString("utf8"));

readstream.OnEnd  += ()     => Console.Write("finished reading");
```

<a name='files_writer' />
#### Reactor.File.Writer

The followinf creates a reactor file writestream. The example writes data and ends the stream when complete.

```csharp
var writestream = Reactor.File.WriteStream.Create("output.txt");

writestream.Write("hello world");

writestream.End();

```
<a name='timers' />
### timers

Reactor provides analogous implementations for setInterval(...) and setTimeout(...) found in 
javascript. These are Reactor.Interval and Reactor.Timeout respectively. The following sections 
outline their use.

<a name='timers_timeout' />
#### timeouts

Reactor's timeout implementation has the same characteristics as setTimeout(...). Callers 
can create Timeouts in the following way.

```csharp
Reactor.Timeout.Create(() => {
	Console.WriteLine("buzz");
}, 1000);
```
Like javascript, Timeouts can also be used to recursively loop without fear of busting the function
stack, a common pattern in javascript, consider the following.....

```csharp
Reactor.Action action = null;
action = new Reactor.Action(() => {
    Console.WriteLine("still going?");
	action(); // bang goes the stack
});
action();
```
And a timeout centric approach...

```csharp
Reactor.Action action = null;
action = new Reactor.Action(() => {
	Console.WriteLine("still going?");
	Reactor.Timeout.Create(action);
});
action();
```
Note: The author recommends using Loop.Post(() => {}) over Timeouts in these scenarios. As follows..

Reactor.Action action = null;
action = new Reactor.Action(() => {
    Reactor.Loop.Post(action);
});
action();

<a name='timers_interval' />
#### intervals

Reactor's interval implementation has the same characteristics as setInterval(...). Callers 
can create intervals in the following way.

```csharp
Reactor.Interval.Create(() => {
	Console.WriteLine("loopz");
}, 2000);
```
Additionally, Intervals can be cleared thusly.

```csharp
Reactor.Interval interval = null;
interval = Reactor.Interval.Create(() => { 
	interval.Clear();
}, 2000);
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