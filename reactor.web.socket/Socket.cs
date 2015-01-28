/*--------------------------------------------------------------------------

Reactor.Web.Sockets

The MIT License (MIT)

Copyright (c) 2014 Haydn Paterson (sinclair) <haydn.developer@gmail.com>

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

using System;
using System.Collections.Generic;

namespace Reactor.Web.Socket
{
    public enum SocketState
    {
        Connecting,

        Open,

        Closing,

        Closed
    }

    public class Socket
    {
        private Reactor.Web.Socket.Transport transport;

        public Reactor.Web.Socket.Context Context   { get; set; }

        public SocketState                State     { get; set; }

        public Reactor.Action             OnOpen    { get; set; }

        public Reactor.Action<Message>    OnMessage { get; set; }

        public Reactor.Action<Exception>  OnError   { get; set; }

        public Reactor.Action             OnClose   { get; set; }

        #region Constructors

        internal Socket(Reactor.Web.Socket.Transport transport)
        {
            this.transport = transport;

            this.State = SocketState.Open;

            this.transport.OnOpen = () =>
            {
                if (this.OnOpen != null)
                {
                    this.OnOpen();
                }
            };

            this.transport.OnError = (exception) =>
            {
                if (this.OnError != null)
                {
                    this.OnError(exception);
                }
            };

            this.transport.OnClose = () =>
            {
                this.State = SocketState.Closed;

                this.Close();

                if (this.OnClose != null)
                {
                    this.OnClose();
                }
            };

            this.transport.OnMessage = (message) =>
            {
                if (this.OnMessage != null)
                {
                    this.OnMessage(message);
                }
            };
        }

        internal Socket(string url, Dictionary<string, string> Headers)
        {
            var request = WebSocketRequest.Create(url);

            request.Headers = Headers;

            request.GetResponse((exception, response) => {

                //---------------------------------------
                // check for handshake error
                //---------------------------------------

                if (exception != null)
                {
                    if (this.OnError != null)
                    {
                        this.OnError(exception);
                    }

                    return;
                }

                //---------------------------------------
                // check for non upgrade errors
                //---------------------------------------

                if (response.StatusCode != 101)
                {
                    if (this.OnError != null)
                    {
                        this.OnError(new Exception("server rejected connection"));
                    }

                    return;
                }

                //---------------------------------------
                // configure events
                //---------------------------------------

                this.transport = new Transport(response.Socket);

                //---------------------------------------
                // emit open
                //---------------------------------------

                if (this.OnOpen != null)
                {
                    this.OnOpen();
                }

                this.transport.OnError += (error) =>
                {
                    if (this.OnError != null)
                    {
                        this.OnError(error);
                    }
                };

                this.transport.OnClose += () =>
                {
                    this.State = SocketState.Closed;

                    if (this.OnClose != null)
                    {
                        this.OnClose();
                    }
                };

                this.transport.OnMessage += (message) =>
                {
                    if (this.OnMessage != null)
                    {
                        this.OnMessage(message);
                    }
                };

                //--------------------------------------------
                // accept any frames passed on the response.
                //--------------------------------------------

                foreach (var frame in response.Frames)
                {
                    this.transport.AcceptFrame(frame);
                }
            });
        }

        #endregion

        #region Send

        public void Send(string message, Action<Exception> complete)
        {
            if (this.transport != null)
            {
                this.transport.Send(message, complete);
            }
        }

        public void Send(string message)
        {
            if (this.transport != null)
            {
                this.transport.Send(message, exception => { });
            }
        }

        public void Send(byte[] message, Action<Exception> complete)
        {
            if (this.transport != null)
            {
                this.transport.Send(message, complete);
            }
        }

        public void Send(byte[] message)
        {
            if (this.transport != null)
            {
                this.transport.Send(message, exception => { });
            }
        }

        public void Close()
        {
            if (this.transport != null)
            {
                this.transport.Close(exception => { });
            }
        }

        #endregion

        #region Statics

        public static Socket Create(string url, Dictionary<string, string> headers)
        {
            return new Socket(url, headers);
        }

        public static Socket Create(string url)
        {
            return new Socket(url, new Dictionary<string, string>());
        }

        #endregion
    }
}
