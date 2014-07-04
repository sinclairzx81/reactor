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

Reactor is a evented, asynchronous io and networking framework written for the Microsoft.Net, Mono, and Unity3D
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

The following is the recommended approach for starting the event loop in a console application. 
Note, by calling the loop, the application will not close at the end of Main(). Calling Start() on the
event loop will internally spawn a single thread which will iterate over the loop (as above).

```csharp
class Program 
{
	static void Main(string [] args)
	{
		Reactor.Loop.Start();

        Reactor.Http.Request.Create("http://google.com", (response) => {

            response.OnData += (data) => {
			
                 Console.WriteLine(data.ToString(Encoding.UTF8));
            };

        }).End();


	}
}
```

<a name='getting_started_windows_forms_applications' />
#### windows forms applications

Reactor is capible of returning all events back to the main UI thread. This is important
for windows forms applications as asynchronous events typically need invoke some action
within the UI. Returning control to the UI thread is achieved by way of the SynchronizationContext.
The following demonstrates setting up in a basic form. The demonstration assumes one textbox and
one button. Clicking on the button will asynchronously download html from the google homepage and
display the result in the textbox.

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
        Reactor.Http.Request.Create("http://google.com", (response) => {

            response.OnData += (data) => {

                this.textBox1.Text += data.ToString(Encoding.UTF8);
            };

        }).End();
    }
}
```

<a name='getting_started_unity3D_applications' />
#### unity3D applications

Reactor integrates well within Unity3D. Reactor simplifies asynchronous programming on the Unity3D platform
by taking care of returning asynchronous control back to the main UI thread. The following demonstrates
setting up reactor in a MonoBehaviour.

```csharp
using UnityEngine;
using System.Collections;

public class MyGameObject : MonoBehaviour {
	
	void Start () {
		
        Reactor.Http.Request.Create("http://google.com", (response) => {
			
            response.OnData += (data) => {

                // event fired on the main UI thread.
            };

        }).End();
	}
	
	void Update () {
		
		//-----------------------------------------------		
		// run the event loop as a coroutine on update.
		//-----------------------------------------------

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
Reactor.Loop.Start();

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
and is type passed back on OnData events.

Buffers can be used to ensure that data being written as a block, and prevents fragmentation
of data. Consider the following.....

```csharp
// server
Reactor.Tcp.Server.Create((socket) => {
    var buffer = Reactor.Buffer.Create();
    buffer.Write(10.0f);
    buffer.Write(20.0f);
    buffer.Write(30.0f);
    socket.Write(buffer);
}).Listen(5000);

// client
var client = Reactor.Tcp.Socket.Create(5000);
client.OnData += (data) => { // data is of type 'Buffer'
    var x = data.ReadSingle();
    var y = data.ReadSingle();
    var z = data.ReadSingle();
    Console.WriteLine("{0} {1} {2}", x, y, z);
};
```

note: replace the server code with the following to see fragmentation...

```csharp
Reactor.Tcp.Server.Create((socket) => {
    socket.Write(10.0f);
    socket.Write(20.0f);
    socket.Write(30.0f);
}).Listen(5000);
```

note: the error when attempting to read 'y' from the buffer.

<a name='streams' />
### streams

All IO bound objects in Reactor are implementations of streams, and they are either Readable, Writeable 
or both. Reactor borrows heavily from libuv / nodejs in this regard. 

<a name='streams_readstream' />
#### readstream

Readstreams support OnData, OnEnd, OnClose and OnError events. In addition, readstreams also
support Pipe(), Pause() and Resume() operations. These functions mirror nodejs streams.

The following demonstrates opening a file as a readstream.

```csharp

var readstream = Reactor.File.ReadStream.Create("myfile.txt");

// fired when the stream has data
readstream.OnData += (data) => { };

// fired when the stream has reached the end
readstream.OnEnd += () => { };

// fired when the stream has closed
readstream.OnClose += () => { };

// fired if there was an error reading from the stream.
readstream.OnError += (exception) => { };

```

<a name='streams_writestream' />
#### writestream

Writestreams support writing data on a underlying stream. Reactor writestreams are
fashioned after nodejs writable streams, and support similar operations. 

The following demonstrates opening a file as a writestream and writing things to it. note that
data written to the stream is written sequentially. 

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

Creating a file readstream. (copy one file to another)

```csharp
Reactor.Loop.Start();

var readstream = Reactor.File.ReadStream.Create("input.txt");

var writestream = Reactor.File.WriteStream.Create("output.txt");

readstream.Pipe(writestream);
```

<a name='files_writestream' />
#### writestream

Creating a file writestream. (copy one file to another)

```csharp
Reactor.Loop.Start();

var readstream = Reactor.File.ReadStream.Create("input.txt");

var writestream = Reactor.File.WriteStream.Create("output.txt");

readstream.Pipe(writestream);
```

<a name='http' />
### http

Reactor provides a abstraction over the BCL http classes. The following outlines their use.



<a name='http_server' />
#### server

The following will create a simple http server and listen on port 5000.

note: windows users may need to run their applications with elevated privilges as the HttpListener class will complain with Access denied errors in .NET. 
Users may need to set access rights for the current user with the following netsh http add urlacl url=http://[host_port]:[your port]/ user=MACHINENAME/USERNAME.

```csharp

Reactor.Loop.Start();

Reactor.Http.Server.Create((context) => {

    context.Response.ContentType = "text/html";

    context.Response.Write("hello world");

    context.Response.End();

}).Listen(5000);
```

<a name='http_server_request' />
##### request

Reactor provides a evented abstraction over the type System.Net.HttpListenerRequest. The request is
a implementation of a readable stream.

```csharp
Reactor.Loop.Start();

Reactor.Http.Server.Create((context) => {

    context.Response.Pipe(context.Request);

}).Listen(5000);
```

<a name='http_server_response' />
##### response

Reactor provides a evented abstraction over the type System.Net.HttpListenerResponse The response is
a implementation of a writeable stream.

```csharp
Reactor.Loop.Start();

Reactor.Http.Server.Create((context) => {

    context.Response.Write("hello world");

    context.Response.End();     

}).Listen(5000);
```

<a name='http_request' />
#### request

Reactor provides a evented abstraction over both HttpWebRequest and HttpWebResponse classes. The request is a implementation of a writeable stream, and 
the response is a implementation of a readable stream. The following demonstrates making a GET and POST with Reactor.
request.

Make a GET request.

```csharp

Reactor.Loop.Start();

var request = Reactor.Http.Request.Create("http://domain.com", (response) => {

	response.OnData += (data) => Console.WriteLine(data.ToString(Encoding.UTF8));

	response.OnEnd += () => Console.WriteLine("the response has ended");

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

Reactor provides a abstraction over the System.Net.Sockets.Socket TCP socket. The following demonstrates
its use.

<a name='tcp_server' />
#### server

Create a tcp socket server. 

```csharp
Reactor.Tcp.Server.Create((socket) => {

	Console.WriteLine("we have a socket");

	Reactor.Interval.Create(() => {

		socket.Write("hello there");
	
	}, 1000);

}).Listen(5000);

```

<a name='tcp_socket' />
#### socket

Reactor sockets are evented abstractions over System.Net.Socket.Socket. Tcp sockets are implementations
of both readable and writable streams. The following code connects to the server in the previous example.

```csharp
var client = Reactor.Tcp.Socket.Create(System.Net.IPAddress.Loopback, 5000);

client.OnConnect += () => {

	client.OnData += (data) => Console.WriteLine(data.ToString(Encoding.UTF8));
};
```

<a name='udp' />
### udp

Reactor provides a abstraction over a System.Net.Sockets.Socket UDP socket. The following demonstrates
its use.

<a name='udp_socket' />
#### socket

The following demonstrates setting up two sockets, one to connect to the other.

```csharp
// setup socket A (listen for B)
var socketA = Reactor.Udp.Socket.Create();
socketA.Bind(System.Net.IPAddress.Any, 5000);
socketA.OnMessage += (endpoint, data) => {
	Console.WriteLine(endpoint);
	Console.WriteLine(System.Text.Encoding.UTF8.GetString(data));	
};

// setup socket B (send to A)
var socketB = Reactor.Udp.Socket.Create();
socketB.Bind(System.Net.IPAddress.Any, 5001);
socketB.Send(IPAddress.Loopback, System.Text.Encoding.UTF8.GetBytes("hello from socket B"));

```

<a name='threads' />
### threads

Reactor provides a mechansism for developers to create threaded processes which can be called out
to and returned back on the main thread once they complete. This functionality was intended for 
Unity3D developers who may need to process work out on a background thread. 

<a name='threads_worker' />
#### worker

Workers can be created in the following way. Below, the worker is returned back as a delegate.

```csharp
//--------------------------------------------------------------
// a simple worker, accepts a integer, and returns a string
//--------------------------------------------------------------
var worker = Reactor.Threads.Worker.Create<int, string>((int a) => {
	
	return a.ToString();	

});
```
note: the parameterized type is of the form. <input, output>.

The following will call the worker.

```csharp

worker(10, (exception, result) => {

	Console.WriteLine(result); // will print "10"

});
```

<a name='crypto' />
### crypto

Reactor provides a mechanism to encrypt and decrypt data passed down readable and writeable interfaces by building on .net's ICryptoTransform
interface.

<a name='crypto_transform' />
#### crypto_transform

Reactor abstracts the ICryptoTransform interface to allow for the encrypting and decrypting of streams. The Reactor.Crypto.Transform class
is both a readable and writeable stream implementation, and can be used to pipe data from a to b. 

example 1: encrypting a string.

```csharp
//-------------------------------------------
// setup simple encryptor
//-------------------------------------------
var rijndael = Rijndael.Create();

var encryptor = Reactor.Crypto.Transform.Create( rijndael.CreateEncryptor(rijndael.Key, rijndael.IV) );

encryptor.OnData += (data) => {

	Console.WriteLine("encrypted: ");

    Console.WriteLine( data.ToString(Encoding.UTF8) ); 
};

//-------------------------------------------
// encrypt data
//-------------------------------------------

encryptor.Write("this is a string to encrypt");

```
example 2: encrypt and decrypt

```csharp
//-------------------------------------------
// setup encryptor and decryptor
//-------------------------------------------
var rijndael = Rijndael.Create();

var encryptor = Reactor.Crypto.Transform.Create(rijndael.CreateEncryptor(rijndael.Key, rijndael.IV));

var decryptor = Reactor.Crypto.Transform.Create(rijndael.CreateDecryptor(rijndael.Key, rijndael.IV));

encryptor.OnData += (data) => {

    Console.WriteLine("encrypted: ");

    Console.WriteLine(data.ToString(Encoding.UTF8));

    decryptor.Write(data); // decrypt
};

decryptor.OnData += (data) => {

    Console.WriteLine("decrypted: ");

    Console.WriteLine(data.ToString(Encoding.UTF8));
};

//-------------------------------------------
// encrypt data
//-------------------------------------------

encryptor.Write("this is a string to encrypt");

```
example 3: encrypting a file (use pipe)

```csharp
var filereadstream     = Reactor.File.ReadStream.Create("myfile.txt");

var encryptor	       = Reactor.Crypto.Transform.Create( rijndael.CreateEncryptor(rijndael.Key, rijndael.IV));

var encrypted          = Reactor.File.WriteStream.Create("encrypted.txt");

input.Pipe(encryptor).Pipe(encrypted);
```
example 4: encrypting and decrypting a file

```csharp
var rijndael = Rijndael.Create();

//-----------------------
// encrypt
//-----------------------

var readstream         = Reactor.File.ReadStream.Create("myfile.txt");

var encryptor          = Reactor.Crypto.Transform.Create( rijndael.CreateEncryptor(rijndael.Key, rijndael.IV));

var encrypted          = Reactor.File.WriteStream.Create("encrypted.txt");

readstream.Pipe(encryptor).Pipe(encrypted);

encryptor.OnEnd += () => { // wait for encryptor
	
	//-----------------------
	// decrypt
	//-----------------------

    readstream    = Reactor.File.ReadStream.Create("encrypted.txt");

    var decryptor = Reactor.Crypto.Transform.Create(rijndael.CreateDecryptor(rijndael.Key, rijndael.IV));

    var decrypted = Reactor.File.WriteStream.Create("decrypted.txt");

    readstream.Pipe(decryptor).Pipe(decrypted);
}; 
```