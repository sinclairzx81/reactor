## Reactor Web

The Reactor Web library is a lightweight, nodejs express / connect / Sinatra inspired web framework built on top of the
Reactor.Http.Server class. It provides a higher level abstraction over the basic http listener, and supports rich routing, 
and application middleware applied at the server or http route level. This page serves to document its use.

### creating a web server

The following code will create a new Reactor web server. This server will listen on port 5000.

```csharp

var server = Reactor.Web.Server.Create();

server.Listen(5000);

```

### routing

The Reactor web server allows developers to create application routes. The following illustrates how.

```csharp

var server = Reactor.Web.Server.Create();

// create a http get route to the url
// http://localhost:5000/
server.Get("/", (context) => {

	context.Response.Write("home");
	
	context.Response.End();
});

// create a http post route to the url 
// http://localhost:5000/submit
server.Post("/submit", (context) => {

	context.Request.OnData += (data) => {
		
		Console.WriteLine(data.ToString("utf8"));
	};

	context.Request.End += () => {
		
		context.Response.Write("finished reading post");
		
		context.Response.End();
	};
});

server.Listen(5000);

```

## routing with parameters

It is possible to have define routes with parameters, the following describes how.

```csharp

var server = Reactor.Web.Server.Create();

server.Get("/", (context) => {

	context.Response.Write("home");
	
	context.Response.End();
});

// create a route which accepts a id. this
// will map all requests to http://localhost:5000/users/* 
// where * will be the id passed in the url.
server.Get("/users/:id", (context) => {

	context.Response.Write(context.Params["id"]);
	
	context.Response.End();
});

server.Listen(5000);

```

## middleware

The Reactor web server supports application middleware. Middleware are functions that are
run prior to the actual request. They can be used to preform actions such as authenticating
and authorizing a request, logging, or serving content from cache. The following demonstrates
their use.


```csharp
var server = Reactor.Web.Server.Create();

// apply middleware at the server level. This
// means that "all" routes will have this method
// invoked on every request.
server.Use((context, next) => {

	Console.WriteLine("we are inside the middleware");

	next();
});

server.Get("/", (context) => {

	Console.WriteLine("We are inside the handler");

	context.Response.Write("home");
	
	context.Response.End();
});



// here we create a action to serve as middleware. 
Reactor.Action custom = (context, next) => {

	Console.WriteLine("We are inside custom middleware");
	
	next();

};

// here we apply the custom middleware to the route. The 
// middleware will only be run for this route.
server.Get("/other", [custom], (context) => {

	Console.WriteLine("We are inside the handler");

	context.Response.Write("home");
	
	context.Response.End();
});

server.Listen(5000);
```

note: when using middleware, you "must" call next(). This passes control to the next middleware in the stack,
or to the route handling the request. If a middleware cannot call next (for example, a request couldn't authenticate
in the middleware), then you "must" handle the request in the middleware itself.




