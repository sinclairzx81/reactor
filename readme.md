![](https://raw.githubusercontent.com/sinclairzx81/reactor/reactor-next/reactor/reactor.png)
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
* [Getting Started](#getting_started)
* [Event Loop](#loop)
	* [Starting and Stopping](#loop_starting_and_stopping)
	* [Console Applications](#loop_console_applications)
	* [Windows Forms](#loop_windows_forms)
	* [Unity3D](#loop_unity3D)
	* [Threads and Loops](#loop_threads_and_loops)
* [Working with Streams](#streams)
	* [Reactor.Buffer](#reactor_buffer)
	* [Reactor.IReadable](#reactor_ireadable)
	* [Reactor.IWritable](#reactor_iwritable)
	* [Reactor.IDuplexable](#reactor_iduplexable)
* [Reading and Writing Files](#files)
	* [Reactor.File.Reader](#reactor_file_reader)
	* [Reactor.File.Writer](#reactor_file_writer)
* [Working with Stdio](#stdio)
	* [Reactor.Process.Current](#reactor_process_current)
	* [Reactor.Process.Process](#reactor_process_process)	
* [Tcp Sockets](#tcp)
	* [Reactor.Tcp.Server](#reactor_tcp_server)
	* [Reactor.Tcp.Socket](#reactor_tcp_socket)
* [Tls Sockets](#tls)
	* [Reactor.Tls.Server](#reactor_tls_server)
	* [Reactor.Tls.Socket](#reactor_tls_socket)
* [Udp Sockets](#udp)
	* [Reactor.Udp.Socket](#reactor_udp_socket)
* [Ip Sockets](#ip)
	* [Reactor.Ip.Socket](#reactor_ip_socket)		
* [Http/Https Servers](#http_https)
	* [Reactor.Http.Server](#reactor_http_server)
		* [Reactor.Http.ServerRequest](#reactor_http_serverrequest)
		* [Reactor.Http.ServerResponse](#reactor_http_serverresponse)
	* [Reactor.Http.Request](#reactor_http_request)
* [Dns](#dns)
	* [Reactor.Dns](#reactor_dns)
* [Timers](#timers)
	* [Reactor.Timeout](#reactor_timeout)
	* [Reactor.Interval](#reactor_interval)	
* [Fibers and the ThreadPool](#fibers)
	* [Reactor.Fibers.Fiber](#reactor_fibers_fiber)
* [Async Primitives](#async)
	* [Reactor.Async.Future](#reactor_async_future)
	* [Reactor.Async.Event](#reactor_async_event)
	* [Reactor.Async.Queue](#reactor_async_queue)
	* [Reactor.Async.Racer](#reactor_async_racer)
* [Enumerables and LINQ](#enumerables)
	* [Reactor.Enumerable](#reactor_enumerable)	


<a name='getting_started' />
### Getting Started

The following section outlines getting started with reactor.

<a name='loop' />
### Event Loop

Reactor operates via a single event loop which is used internally 
to synchronize IO completion callbacks. Callers may select the synchronization
context to which all IO events are returned on. 

<a name='loop_starting_and_stopping' />
#### Starting and Stopping.

Starting a Reactor event loop is simple...the following is all that is required
to start a Reactor event loop.

	Reactor.Loop.Start();

Once this loop has been started, it is ready to start processing IO events 
on behalf of the reactor API's throughout the application. 

Stopping the loop is equally straight forward.

	Reactor.Loop.Stop();

The following sections outline various ways to start the event loop for a
variety of environments.

<a name='loop_console_applications' />
#### Console Applications

Console applications are the simplist to get going. The following will 
start the event loop in a basic console application, followed by starting
a http server on port 5000.

```csharp
class Program {
	static void Main(string [] args) {
		Reactor.Loop.Start();
		
		Reactor.Http.Server.Create(context => {
			context.Response.Write("hello console!!");
			context.Response.End();
		}).Listen(5000);
	}
}
```

<a name='loop_windows_forms' />
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

<a name='loop_unity3D' />
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
Note: Be aware that by default, if Unity currently doesn't have the users window focus, it
will stop firing MonoBehaviour updates. This will have a direct consequence on being able
to process events. Users of Unity may locate the "Run in background" option as a work 
around.

<a name='loop_threads_and_loops' />
### Threads and Loops

Internally, Reactors API's are 'posting' to the event loop with the following call...
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

                /* post back to the caller. */
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
<a name="streams" />
### Working with Streams

At its core, Reactor streams are made up of only two interfaces and a buffer 
passed around between them. These types are the Reactor.IReadable, Reactor.IWritable and 
the Reactor.Buffer. Understanding these types will help developers get the 
most out of the library. Below is a brief summary of what these are: 

- Reactor.Buffer    - a byte buffer.
- Reactor.IReadable - a event driven read interface to receive buffers (above)
- Reactor.IWritable - a asynchronous write interface to write buffers (above)

there is one other interface which Reactor defines which is.

- Reactor.IDuplexable - whose behaviour is that of IReadable and IWritable.

The sections below outline these types in details.

<a name="reactor_buffer" />
#### Reactor.Buffer

Reactor.Buffer is reactors the primary primitive for passing data around. Internally, 
the Reactor.Buffer is actually a implementation of a classic ring buffer, but with 
a few bits of added magic such as allowing for the buffer to resize. 

Note: the Reactor.Buffer is modelled closely on the nodejs buffer and has similar functionality. 
See https://nodejs.org/api/buffer.html for some additional info,

From a developers standpoint, buffers can be thought of as a FIFO queue, where the 
first data written is also the first data read. here's an simple example.

``` csharp
var buffer = Reactor.Buffer.Create();
buffer.Write("hello world");
byte [] data = buffer.Read(5); // reads 'hello' out of the buffer.
Console.WriteLine(buffer); // prints " world"
```

In addition, buffers can be 'unshifted', the following would put 'hello' back in 
buffer.

```csharp
buffer.Unshift(data);
Console.WriteLine(buffer); // prints "hello world"
```

On top of string shifting, Reactor.Buffer also provides number overloads for writing
native .net value types. The following will write a series of 32bit integers to the buffer
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
Note: most implementations of Reactor.IWritable also share these overloads for convenience.

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

<a name="reactor_ireadable" />
### Reactor.IReadable

Reactor.IReadable is the interface shared amoung all things that "stream data" (files,
 tcp sockets, stdio, http requests etc).

Unlike a .net StreamReader, Reactor handles reading data through events, and can be 
thought of as a Rx-like observable (Reactor was designed to play nicely with Rx). 
The following is a example is reading a file from disk. (Where a Reactor.File.Reader 
is a concrete implementation of Reactor.IReadable)

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

<a name="reactor_iwritable" />
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

<a name="reactor_iduplexable" />
### Reactor.IDuplexable: Reactor.IReadable, Reactor.IWritable

A Reactor.IDuplexable is a combiniation of a Reactor.IReadable and Reactor.IWritable. The 
Reactor.Tcp.Socket and Reactor.Tls.Socket are both concrete implementation of this interface.

In this light, developers "should" perceive duplex streams have having the characteristics
and behaviours of both readable and writable streams simultaneously.  

<a name='files' />
### Reading and Writing Files.

Internally, Reactor is layering the Reactor.IReadable and Reactor.IWritable interfaces over 
System.IO.FileStream.

<a name='reactor_file_reader' />
#### Reactor.File.Reader : Reactor.IReadable

Reactor.File.Reader is a event driven read abstraction over the System.IO.FileStream. The following
code will open a file for reading and process all data from this file.

```csharp
var reader = Reactor.File.Reader.Create("myfile.txt");
reader.OnRead  (buffer => Console.WriteLine(buffer));
reader.OnError (error  => Console.WriteLine(error));
reader.OnEnd   (()     => Console.WriteLine("end"));
```
Reactor.File.Reader supports seeking into a file and reading byte ranges... the following will skip the 
first 10 bytes of the file, and take 100 bytes. When 100 bytes are read, this stream will emit a 'end' 
event and dispose immediately.

```csharp
var reader = Reactor.File.Reader.Create("myfile.txt", 10, 100); // skip 10, take 100
reader.OnRead  (buffer => Console.WriteLine(buffer));
reader.OnError (error  => Console.WriteLine(error));
reader.OnEnd   (()     => Console.WriteLine("end"));
```

In addition, Reactor.File.Read also provides overloads to pass through FileMode and FileShare options.
The following will pass the FileMode 'open and create' and FileShare option 'read/write' (indicating
that that this file can be written to simulatiously).

```csharp
var reader = Reactor.File.Reader.Create("myfile.txt", 10, 100, 
										FileMode.OpenOrCreate, 
										FileShare.ReadWrite);
reader.OnRead  (buffer => Console.WriteLine(buffer));
reader.OnError (error  => Console.WriteLine(error));
reader.OnEnd   (()     => Console.WriteLine("end"));
```

<a name='reactor_file_writer' />
#### Reactor.File.Writer : Reactor.IWritable

Reactor.File.Writer is a asynchronous write abstraction over the System.IO.FileStream. The following
code will open a file for writing, write some text, then close.

```csharp
var writer = Reactor.File.Writer.Create("myfile.txt");
writer.Write("hello");
writer.Write("world");
writer.End();
```
In the code above, the stream is disposed of as soon as the call to "End()" has completed.

Like the Reactor.File.Reader, the Reactor.File.Writer supports seeking into the file to begin
writing. The following example will skip 1000 bytes of this file and begin writing.

```csharp
var writer = Reactor.File.Writer.Create
		("myfile.txt", 1000); 
writer.Write("hello");
writer.Write("world");
writer.End();
```

Overloads for FileMode and FileShare exist also. The following will create a writer with
a FileMode of 'Truncate' and a FileShare or 'Write'

```csharp
var writer = Reactor.File.Writer.Create("myfile.txt", 1000, 
                                        FileMode.Truncate, 
										FileShare.Write);
writer.Write("hello");
writer.Write("world");
writer.End();
```

<a name='tcp' />
### Tcp Sockets

Reactor Tcp Sockets provide a event driven socket 'accept' interface over System.Net.TcpListener as well
as a bidirectional async and event driven interface over a TCP System.Net.Socket by way of 
System.Net.NetworkStream. 

<a name='reactor_tcp_server' />
#### Reactor.Tcp.Server

The following example will create a Reactor.Tcp.Server and start it listening on port 5000. In this
example, we specify a handler to receive any incoming sockets. 

```csharp
var server = Reactor.Tcp.Server.Create(socket => {
	/* manage the socket here */
});
server.Listen(5000);
```
The Reactor.Tcp.Server does not keep any internal list of sockets it has accepted. This responsibility 
is delegated to the implementor. The following code example demonstrates how a implementor might
go about managing a pool of socket connections. In this example, we have some code in our 
tcp server to push new sockets to a socket pool. If any of those sockets disconnect, we 
simply remove them from this list.

```csharp
var sockets = new List<Reactor.Tcp.Socket>();

Reactor.Tcp.Server.Create(socket => {
    sockets.Add(socket);
    socket.OnEnd(() => sockets.Remove(socket));
}).Listen(5000);

/* broadcast! */
Reactor.Interval.Create(() => {
    foreach(var socket in sockets) 
        socket.Write(DateTime.Now.ToString());
}, 1000);

```  

<a name='reactor_tcp_socket' />
#### Reactor.Tcp.Socket : Reactor.IDuplexable

The Reactor.Tcp.Socket provides an event driven abstraction over a System.Net.NetworkStream. Reactor.Tcp.Socket implements 
Reactor.IDuplexable, and offers a bidirectional read / write interface with shared characteristics of a Reactor.IReadable
Reactor.IWritable. 

The following source code connects to the star wars telnet server and streams some star wars.

```csharp
/* star wars!! */
var socket = Reactor.Tcp.Socket.Create("towel.blinkenlights.nl", 23);
socket.OnRead   (data  => Console.WriteLine(data));
socket.OnError  (error => Console.WriteLine(error));
socket.OnEnd    (()    => Console.WriteLine("disconnected"));
```

This second example expands on the first, and does a very raw http request out to google.

```csharp
var socket = Reactor.Tcp.Socket.Create("google.com", 80);
socket.OnRead   (data  => Console.WriteLine(data));
socket.OnError  (error => Console.WriteLine(error));
socket.OnEnd    (()    => Console.WriteLine("disconnected"));
socket.Write("GET / HTTP/1.0\r\n\r\n"); 
```

A Reactor.Tcp.Socket has a entirely 'optional' OnConnect event. Clients can use this event to signal when
the client socket has connected.

```csharp
var socket = Reactor.Tcp.Socket.Create("google.com", 80);
socket.OnConnect(() => {
	Console.WriteLine("connected");
	socket.OnRead   (data  => Console.WriteLine(data));
	socket.OnError  (error => Console.WriteLine(error));
	socket.OnEnd    (()    => Console.WriteLine("disconnected"));
	socket.Write("GET / HTTP/1.0\r\n\r\n");
});
```
Note: sockets emitted from the Reactor.Tcp.Server 'DO NOT' receive the 'connect' event. Server side sockets are
already assumed connected once are passed through to the socket handler.

Disconnections can be tricky. When developing with Reactor.Tcp.Socket, callers may terminate a TCP connection at either 
side by simply calling "End()" on their respective sockets. The other side will be sent a socket shutdown signal, and 
both sides "should" shutdown gracefully (firing a 'end' event at either side). 

The following example helps to demonstrate this behaviour.

```csharp
static void Main(string[] args) {
    Reactor.Loop.Start();

    Reactor.Tcp.Server.Create(s => {
        Console.WriteLine("server: connection");
        s.OnRead  (data  => Console.WriteLine("server: " + data));
        s.OnError (error => Console.WriteLine("server: " + error));
        s.OnEnd   (()    => Console.WriteLine("server: disconnection"));
    	// Reactor.Timeout.Create(s.End, 2000);  // try here
    }).Listen(5000);

    var socket = Reactor.Tcp.Socket.Create(5000);
    socket.OnConnect(() => {
        Console.WriteLine("client: connection");
        socket.OnRead  (data  => Console.WriteLine("client: " + data));
        socket.OnError (error => Console.WriteLine("client: " + error));
        socket.OnEnd   (()    => Console.WriteLine("client: disconnection"));
    });

    /* disconnect socket in 2000ms */
    Reactor.Timeout.Create(socket.End, 2000);
}
```

There are instances however where a graceful shutdown is not possible (typically in the genuine net 
drops), but there is also a application scenario where a graceful shut down cannot happen..... 

If you try commenting out the OnRead at either side, you notice that the side that 'isn't' reading does
not receive gracefully 'end'. In this scenario, the socket falls to receive its shutdown signal from the
disconnecting side because it was never 'reading' from the socket to receive the message. Be mindful
of this behavior!!

It is possible to stream files over a TCP socket with relative ease. The following example sets up a
server to stream the file 'FILE.DAT' over a TCP socket.

```csharp
Reactor.Tcp.Server.Create(socket => {
	var reader = Reactor.File.Reader.Create("FILE.DAT");
	reader.Pipe(socket);
}).Listen(5000);
```
A connecting party can easily connect up and download this file with the following...

```csharp
var client = Reactor.Tcp.Socket.Create(5000);
var writer = Reactor.File.Write("SAVED.DAT");
client.Pipe(writer);
```


<a name='timers' />
### timers

Reactor provides similar implementations for setInterval(...) and setTimeout(...) found in 
javascript. These are Reactor.Interval and Reactor.Timeout respectively. The following sections 
outline their use.

<a name='reactor_timeout' />
#### Reactor.Timeout

Reactor's Timeout implementation has the same characteristics as setTimeout(...). Internally, 
Reactor.Timeout is layering this functionality over a System.Timers.Timer.

Timeouts can be created in the following way.

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

Note: The author of this library recommends using Loop.Post(() => {}) over Timeouts in these scenarios. 

As follows..

```csharp
Reactor.Action action = null;
action = new Reactor.Action(() => {
    Reactor.Loop.Post(action);
});
action();
```

<a name='reactor_interval' />
#### Reactor.Interval

Reactor's interval implementation has the same characteristics as javascripts setInterval(...). Internally,
Reactor.Interval is layering this functionality over System.Timers.Timer. 

Intervals can be created in the following way.

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