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

using System;
using System.Globalization;
using System.Text;

namespace Reactor.Http {

    /// <summary>
    /// Reactor HTTP Server Response.
    /// </summary>
    public class ServerResponse : Reactor.IWritable {
        
        #region Channels

        public class Channels {

            public Reactor.Async.Event<Exception> Error {  get; set; }
        }

        #endregion

        private Reactor.Tcp.Socket    socket;
        private Reactor.Http.Headers  headers;
        private Reactor.Http.Cookies  cookies;
        private Version               version;
        private string                connection;
        private string                cache_control;
        private Encoding              content_encoding;
        private long                  content_length;
        private string                content_type;
        private int                   status_code;
        private string                status_description;
        private string                location;
        private bool                  header_sent;

        public ServerResponse(Reactor.Tcp.Socket socket) {

            this.socket             = socket;
            this.headers            = new Reactor.Http.Headers();
            this.cookies            = new Reactor.Http.Cookies();
            this.version            = new Version(1, 1);
            this.status_code        = 200;
            this.connection         = "close";
            this.cache_control      = "no-cache"; 
            this.content_encoding   = Encoding.Default;
            this.status_description = "OK";
            this.content_type       = string.Empty;
            this.location           = string.Empty;
            this.header_sent        = false;
        }

        #region Properties

        public Reactor.Http.Headers Headers           
        {
            get
            {
                return this.headers;
            }
        }

        public Reactor.Http.Cookies Cookies           
        {
            get
            {
                return this.cookies;
            }
        }

        public string               Connection        
        {
            get
            {
                return this.connection;
            }
            set
            {
                this.connection = value;
            }
        }

        public Encoding             ContentEncoding   
        {
            get
            {
                return this.ContentEncoding;
            }
            set
            {
                this.ContentEncoding = value;
            }
        }

        public long                 ContentLength     
        {
            get 
            {
                return this.content_length; 
            }
            set 
            {
                this.content_length = value;
            }
        }

        public string               ContentType       
        {
            get
            {
                return this.content_type;
            }
            set
            {
                this.content_type = value;
            }
        }

        public int                  StatusCode        
        {
            get
            {
                return this.status_code;
            }
            set
            {
                this.status_code = value;
            }
        }

        public string               StatusDescription 
        {
            get
            {
                return this.status_description;
            }
            set
            {
                this.status_description = value;
            }
        }

        #endregion

        #region IWritable

        public Reactor.Async.Future Write (Reactor.Buffer buffer) {
            if (!this.header_sent) {
                this.header_sent = true;
                this._WriteHeaders();
            }
            //-----------------------------------
            // transfer encoding chunked
            //-----------------------------------
            if (this.content_length == 0) {
                socket.Write(String.Format("{0:x}\r\n", buffer.Length));
                buffer.Write(Encoding.ASCII.GetBytes("\r\n"));
            }

            return this.socket.Write(buffer);
        }

        public Reactor.Async.Future Flush() {
            if (!this.header_sent) {
                this.header_sent = true;
                this._WriteHeaders();
            }
            return this.socket.Flush();
        }

        public Reactor.Async.Future End () {
            if (!this.header_sent) {
                this.header_sent = true;
                this._WriteHeaders();
            }
            //-----------------------------------
            // chunked
            //-----------------------------------
            if (this.content_length == 0) {          
                this.socket.Write("0\r\n");
                this.socket.Write("\r\n");
            }
            return this.socket.End();
        }

        public void OnError     (Reactor.Action<Exception> callback)
        {
            this.socket.OnError(callback);
        }

        public void RemoveError (Reactor.Action<Exception> callback) {

            this.socket.RemoveError(callback);
        }

        #endregion

        #region Channels

        
        #endregion

        #region Internal

        private void _WriteHeaders() {

            var culture_info = CultureInfo.InvariantCulture;

            //-------------------------------------
            // server:
            //-------------------------------------
            if (headers["Server"] == null) {

                headers.Set_Internal("Server", "Reactor-HTTP/0.9");
            }

            //-------------------------------------
            // connection:
            //-------------------------------------
            if (this.connection.Length > 0) {

                headers.Set_Internal("Connection", this.connection);
            }

            //-------------------------------------
            // cache-control:
            //-------------------------------------
            if (this.cache_control.Length > 0) {

                headers.Set_Internal("Cache-Control", this.cache_control);
            }

            //-------------------------------------
            // date:
            //-------------------------------------

            if (headers["Date"] == null) {

                headers.Set_Internal("Date", DateTime.UtcNow.ToString("r", culture_info));
            }

            //-------------------------------------
            // content-type:
            //-------------------------------------
            if (this.content_type.Length > 0) {

                headers.Set_Internal("Content-Type", content_type);
            }

            //-------------------------------------
            // content-length | transfer-encoding
            //-------------------------------------
            if (this.content_length == 0) {

                headers.Set_Internal("Transfer-Encoding", "chunked");
            }
            else {

                headers.Set_Internal("Content-Length", content_length.ToString(culture_info));
            }
            
            //-------------------------------------
            // location:
            //-------------------------------------
            if (this.location.Length > 0) {

                headers.Set_Internal("Location", location);
            }

            //-------------------------------------
            // cookies:
            //-------------------------------------
            foreach (Cookie cookie in cookies) {

                headers.Set_Internal("Set-Cookie", cookie.ToClientString());
            }

            //-------------------------------------
            // write:
            //-------------------------------------

            var buffer = Reactor.Buffer.Create(128);

            buffer.Write("HTTP/{0} {1} {2}\r\n", version, status_code, status_description);

            buffer.Write(this.headers.ToString());

            this.socket.Write(buffer);
        }

        #endregion

        #region Buffer

        /// <summary>
        /// Writes this data to the stream.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (byte[] buffer, int index, int count) {
            return this.Write(Reactor.Buffer.Create(buffer, 0, count));
        }

        /// <summary>
        /// Writes this data to the stream.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (byte[] buffer) {
            return this.Write(Reactor.Buffer.Create(buffer));
        }

        /// <summary>
        /// Writes this data to the stream.
        /// </summary>
        /// <param name="data"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (string data) {
            return this.Write(System.Text.Encoding.UTF8.GetBytes(data));
        }

        /// <summary>
        /// Writes this data to the stream.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (string format, params object[] args) {
            format = string.Format(format, args);
            return this.Write(System.Text.Encoding.UTF8.GetBytes(format));
        }

        /// <summary>
        /// Writes this data to the stream.
        /// </summary>
        /// <param name="data"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (byte data) {
            return this.Write(new byte[1] { data });
        }

        /// <summary>
        /// Writes a System.Boolean value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (bool value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.Int16 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (short value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.UInt16 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (ushort value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.Int32 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (int value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.UInt32 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (uint value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.Int64 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (long value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.UInt64 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (ulong value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.Single value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (float value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.Double value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (double value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        #endregion

        public void OnDrain(Action callback)
        {
            this.socket.OnDrain(callback);
        }

        public void OnceDrain(Action callback)
        {
            this.socket.OnceDrain(callback);
        }

        public void RemoveDrain(Action callback)
        {
            this.socket.RemoveDrain(callback);
        }

        public void OnEnd(Action callback)
        {
            this.socket.OnEnd(callback);
        }

        public void RemoveEnd(Action callback)
        {
            this.socket.RemoveEnd(callback);
        }



    }
}
