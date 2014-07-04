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

namespace Reactor.Web.Sockets
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
        private Reactor.Web.Sockets.Transport         channel;

        public Reactor.Web.Sockets.Context      Context;

        public SocketState                        State { get; set; }

        public event Reactor.Action                   OnOpen;

        public event Reactor.Action<Message>          OnMessage;

        public event Reactor.Action<Exception>        OnError;

        public event Reactor.Action                   OnClose;

        #region Constructors

        internal Socket (Reactor.Web.Sockets.Transport channel)
        {
            this.channel = channel;

            this.State   = SocketState.Open;

            this.channel.OnOpen += () =>
            {
                if(this.OnOpen != null)
                {
                    this.OnOpen();
                }
            };

            this.channel.OnError += (exception) =>
            {
                if (this.OnError != null)
                {
                    this.OnError(exception);
                }
            };

            this.channel.OnClose += () =>
            {
                this.State = SocketState.Closed;

                if (this.OnClose != null)
                {
                    this.OnClose();
                }
            };

            this.channel.OnMessage += (message) =>
            {
                if(this.OnMessage != null)
                {
                    this.OnMessage(message);
                }
            };
        }

        public   Socket (string url, Dictionary<string, string> Headers)
        {
            var request     = WebSocketRequest.Create(url);

            request.Headers = Headers;

            request.GetResponse((exception, response) =>
            {
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

                this.channel           = new Transport(response.Socket);

                //---------------------------------------
                // emit open
                //---------------------------------------

                if (this.OnOpen != null)
                {
                    this.OnOpen();
                }

                this.channel.OnError += (error) =>
                {
                    if (this.OnError != null)
                    {
                        this.OnError(error);
                    }
                };

                this.channel.OnClose += () =>
                {
                    this.State = SocketState.Closed;

                    if (this.OnClose != null)
                    {
                        this.OnClose();
                    }
                };

                this.channel.OnMessage += (message) =>
                {
                    if(this.OnMessage != null)
                    {
                        this.OnMessage(message);
                    }
                };

                //--------------------------------------------
                // accept any frames passed on the response.
                //--------------------------------------------

                foreach (var frame in response.Frames)
                {
                    this.channel.AcceptFrame(frame);
                }
            });
        }

        #endregion

        #region Send

        public void Send(string message, Action<Exception> complete)
        {
            if (this.channel != null)
            {
                this.channel.Send(message, complete);
            }
        }

        public void Send(string message)
        {
            if (this.channel != null)
            {
                this.channel.Send(message, (exception) => {

                });
            }
        }

        public void Send(byte[] message, Action<Exception> complete)
        {
            if (this.channel != null)
            {
                this.channel.Send(message, complete);
            }
        }

        public void Send(byte [] message)
        {
            if (this.channel != null)
            {
                this.channel.Send(message, (exception) => { });
            }
        }

        public void Close(Action<Exception> callback)
        {
            if (this.channel != null)
            {
                this.channel.Close(callback);
            }
        }

        public void Close()
        {
            if (this.channel != null)
            {
                this.channel.Close((exvception) => { });
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
