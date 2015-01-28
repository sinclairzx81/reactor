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

using Reactor.Web.Socket.Protocol;
using System;

namespace Reactor.Web.Socket
{
    //----------------------------------------
    // class to help with byte[] operations. 
    //----------------------------------------
    internal class ByteData 
    {
        public static byte[] Join(byte[] a, byte[] b)
        {
            var c = new byte[a.Length + b.Length];

            System.Buffer.BlockCopy(a, 0, c, 0, a.Length);

            System.Buffer.BlockCopy(b, 0, c, a.Length, b.Length);

            return c;
        }
       
        public static byte[] Unshift(byte [] a, int num)
        {
            var b = new byte[a.Length - num];

            System.Buffer.BlockCopy(a, num, b, 0, b.Length);

            return b;
        }
    }

    public class Transport
    {
        private Reactor.IDuplexable      duplexable;

        public Reactor.Action            OnOpen    { get; set; }

        public Reactor.Action<Message>   OnMessage { get; set; }

        public Reactor.Action<Exception> OnError   { get; set; }

        public Reactor.Action            OnClose   { get; set; }

        private byte[]               unprocessed;

        #region Constructor

        internal Transport(Reactor.IDuplexable duplexable)
        {
            this.duplexable = duplexable;

            this.duplexable.OnData += (buffer) =>
            {
                //-------------------------------------------------
                // WEB SOCKET FRAME PARSER
                //-------------------------------------------------

                var data = buffer.ToArray();

                //-------------------------------------------------
                // if we have any leftovers...join to data..
                //-------------------------------------------------

                if(this.unprocessed != null) {

                    data = ByteData.Join(this.unprocessed, data);

                    this.unprocessed = null;
                }

                //-------------------------------------------------
                // read in first frame...
                //-------------------------------------------------

                try
                {
                    var frame = Frame.Parse(data, true);

                    this.AcceptFrame(frame);

                    data = ByteData.Unshift(data, (int)frame.FrameLength);
                }
                catch
                {
                    // todo: catch multiple failed exceptions... or fix Frame.Parse

                    this.unprocessed = data;

                    return;
                }

                //-----------------------------------------------
                // read in additional frames...
                //-----------------------------------------------

                while (data.Length > 0)
                {
                    try
                    {
                        var frame = Frame.Parse(data, true);

                        this.AcceptFrame(frame);

                        data = ByteData.Unshift(data, (int)frame.FrameLength);
                    }
                    catch
                    {
                        // todo: catch multiple failed exceptions... or fix Frame.Parse

                        this.unprocessed = data;

                        return;
                    }
                }
            };

            this.duplexable.OnEnd += () =>
            {
                if(this.OnClose != null)
                {
                    this.OnClose();
                }
            };

            var readable = duplexable as IReadable;

            readable.OnError += (exception) =>
            {
                if(this.OnError != null)
                {
                    this.OnError(exception);
                }
            };

            var writeable = duplexable as IWriteable;

            writeable.OnError += (exception) => {

                if (this.OnError != null)
                {
                    this.OnError(exception);
                }
            };

            if (this.OnOpen != null) {

                this.OnOpen();
            }
        }

        #endregion

        #region Methods

        internal void AcceptFrame(Frame frame)
        {
            //-----------------------------------
            // if close, terminate connection
            //-----------------------------------
            
            if (frame.IsClose) {

                this.duplexable.End(exception => {

                    if (exception != null) {

                        if (this.OnError != null) {

                            this.OnError(exception);
                        }
                    }

                    this.Close();
                });

                return;
            }

            //-----------------------------
            // emit message...
            //-----------------------------

            if (this.OnMessage != null)
            {
                var message = new Message(frame);

                this.OnMessage(message);
            } 
            
        }

        #endregion

        #region IChannel

        public void Send(string message, Action<Exception> callback)
        {
            var data   = System.Text.Encoding.UTF8.GetBytes(message);

            //-------------------------------------------------------
            // ensure the buffer does not exceed (16k - 4) for
            // the frame header. currently no support for fragmented
            // sends.
            //-------------------------------------------------------

            if(data.Length > (16384 - 4)) 
            {
                callback(new WebSocketException("message is too large. maximum size is set to 16k (16384 - 4) bytes."));

                return;
            }

            var frame  = Frame.CreateFrame(Fin.Final, Opcode.TEXT, Mask.Unmask, data, false);

            var buffer = Reactor.Buffer.Create(frame.ToByteArray());

            this.duplexable.Write(buffer, exception => {

                if (callback != null) {

                    callback(exception);
                }
            });
        }

        public void Send(byte[] message, Action<Exception> callback)
        {
            var frame  = Frame.CreateFrame(Fin.Final, Opcode.BINARY, Mask.Unmask, message, false);

            var buffer = Reactor.Buffer.Create(frame.ToByteArray());

            this.duplexable.Write(buffer, exception => {

                if (callback != null) {

                    callback(exception);
                }
            });
        }

        public void Send(string message)
        {
            this.Send(message, exception =>  { });
        }

        public void Send(byte[] message)
        {
            this.Send(message, exception => { });
        }

        public void Close(Action<Exception> callback)
        {
            var frame  = Frame.CreateCloseFrame(Mask.Unmask, CloseStatusCode.Normal, "");

            var buffer = Reactor.Buffer.Create(frame.ToByteArray());

            //---------------------------
            // write close to stream
            //---------------------------

            this.duplexable.Write(buffer, exception => {

                //---------------------------
                // end the stream
                //---------------------------

                this.duplexable.End();

                //---------------------------
                // callback
                //---------------------------

                callback(exception);
            });
        }

        public void Close()
        {
            this.Close(exception => { });
        }

        #endregion

        #region Statics

        public static Transport Create(Reactor.IDuplexable duplexable)
        {
            return new Transport(duplexable);
        }

        #endregion
    }
}