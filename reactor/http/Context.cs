/*--------------------------------------------------------------------------

Reactor

The MIT License (MIT)

Copyright (c) 2015 Haydn Paterson (sinclair) <haydn.developer@gmail.com>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

---------------------------------------------------------------------------*/

using System.Security.Principal;

namespace Reactor.Http {

    /// <summary>
    /// Encapsultes a reactor http request.
    /// </summary>
    public class Context {
        
        private System.Security.Principal.IPrincipal   user;
        private Reactor.Http.ServerRequest             request;
        private Reactor.Http.ServerResponse            response;
        private Reactor.Http.ServerTransport           transport;

        /// <summary>
        /// Creates a new http context.
        /// </summary>
        /// <param name="context">A context issued from a http listener.</param>
        internal Context(Reactor.Net.HttpListenerContext context) {
            this.user       = context.User;
            this.request    = new ServerRequest   (context.Request);
            this.response   = new ServerResponse  (context.Response);
            this.transport  = new ServerTransport (context.Connection);
        }

        #region Properties

        /// <summary>
        /// Gets or sets the IPrincipal for this context.
        /// </summary>
        public System.Security.Principal.IPrincipal User {
            get { return this.user; }
            set { this.user = value; }
        }

        /// <summary>
        /// The HTTP Request reader.
        /// </summary>
        public Reactor.Http.ServerRequest Request {
            get { return this.request; }
        }

        /// <summary>
        /// The HTTP Response writer.
        /// </summary>
        public Reactor.Http.ServerResponse Response {
            get {
                if (this.transport.InUse)
                    throw new System.Exception("Access to the Response object is disallowed following access to the Transport object.");
                return this.response;
            }
        }

        /// <summary>
        /// Provides direct access to the raw http transport stream for 
        /// this request. This stream is available in a state post reading the 
        /// http headers to initialize this context and prior to writing 
        /// any data to the client. Access to this property will transform
        /// the inuse state of this transport and access to the Response object
        /// may result in undesirable behaviour.
        /// </summary>
        public Reactor.Http.ServerTransport Transport {
            get {
                this.transport.InUse = true;
                return this.transport;
            }
        }

        #endregion
    }
}
