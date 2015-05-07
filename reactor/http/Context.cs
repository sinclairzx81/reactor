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

namespace Reactor.Http {

    /// <summary>
    /// Encapsulates HTTP request response.
    /// </summary>
    public class Context {

        /// <summary>
        /// The incoming http request.
        /// </summary>
        public Reactor.Http.ServerRequest      Request  { get; private set; }

        /// <summary>
        /// The outgoing http response.
        /// </summary>
        public Reactor.Http.ServerResponse     Response { get; private set; }

        #region Constructors

        /// <summary>
        /// Creates a new HTTP context.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        internal Context(Reactor.Http.ServerRequest request, Reactor.Http.ServerResponse response) {
            this.Request  = request;
            this.Response = response;
        }

        #endregion

        #region Statics

        /// <summary>
        /// Returns a new Context.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        internal static Context Create(Reactor.Http.ServerRequest request, Reactor.Http.ServerResponse response) {
            return new Context(request, response);
        }

        #endregion
    }
}
