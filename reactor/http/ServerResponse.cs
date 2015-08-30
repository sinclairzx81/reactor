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
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Reactor.Http
{
    public class ServerResponse : IWritable {
        #region State

        /// <summary>
        /// Internal state of this writer.
        /// </summary>
        internal enum State {
            /// <summary>
            /// Indicates that this stream is still writing.
            /// </summary>
            Writing, 

            /// <summary>
            /// Indicates that this stream has ended.
            /// </summary>
            Ended
        }

        #endregion

        private Reactor.Net.HttpListenerResponse response;
        private Reactor.Event                    ondrain;
        private Reactor.Event<Exception>         onerror;
        private Reactor.Event                    onend;
        private Reactor.Streams.Writer           writer;
        private State                            state;

        public ServerResponse(Reactor.Net.HttpListenerResponse response) {
            this.response = response;
            this.response.SendChunked = true;
            this.ondrain  = Reactor.Event.Create();
            this.onerror  = Reactor.Event.Create<Exception>();
            this.onend    = Reactor.Event.Create();
            this.state    = State.Writing;
            this.writer   = Reactor.Streams.Writer.Create(response.OutputStream);
            this.writer.OnDrain (this._Drain);
            this.writer.OnError (this._Error);
            this.writer.OnEnd   (this._End);
        }

        #region Events

        /// <summary>
        /// Subscribes this action to the 'drain' event. The event indicates
        /// when a write operation has completed and the caller should send
        /// more data.
        /// </summary>
        /// <param name="callback"></param>
        public void OnDrain (Reactor.Action callback) {
            this.ondrain.On(callback);
        }

        /// <summary>
        /// Subscribes this action once to the 'drain' event. The event indicates
        /// when a write operation has completed and the caller should send
        /// more data.
        /// </summary>
        /// <param name="callback"></param>
        public void OnceDrain(Reactor.Action callback) {
            this.ondrain.Once(callback);
        }

        /// <summary>
        /// Unsubscribes to from the OnDrain event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveDrain(Reactor.Action callback) {
            this.ondrain.Remove(callback);
        }

        /// <summary>
        /// Subscribes this action to the OnError event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnError (Reactor.Action<Exception> callback) {
            this.onerror.On(callback);
        }

        /// <summary>
        /// Unsubscribes this action from the OnError event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveError (Reactor.Action<Exception> callback) {
            this.onerror.Remove(callback);
        }

        /// <summary>
        /// Subscribes this action to the OnEnd event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnEnd(Reactor.Action callback) {
            this.onend.On(callback);
        }

        /// <summary>
        /// Unsubscribes this action from the OnEnd event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveEnd(Reactor.Action callback) {
            this.onend.Remove(callback);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Writes this buffer to the stream.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="callback">A callback to signal when this data has been written.</param>
        public Reactor.Future Write (Reactor.Buffer buffer) {
            buffer.Locked = true;
            return this.writer.Write(buffer);
        }

        /// <summary>
        /// Flushes this stream.
        /// </summary>
        /// <param name="callback"></param>
        public Reactor.Future Flush () {
            return this.writer.Flush();
        }

        /// <summary>
        /// Ends the stream.
        /// </summary>
        /// <param name="callback">A callback to signal when this stream has ended.</param>
        public Reactor.Future End () {
            return this.writer.End();
        }

        /// <summary>
        /// Forces buffering of all writes. Buffered data will be 
        /// flushed either at .Uncork() or at .End() call.
        /// </summary>
        public void Cork () {
            this.writer.Cork();
        }

        /// <summary>
        /// Flush all data, buffered since .Cork() call.
        /// </summary>
        public void Uncork () {
             this.writer.Uncork();
        }

        #endregion

        #region HttpListenerResponse

        public Encoding ContentEncoding {
            get { return this.response.ContentEncoding; }
            set { this.response.ContentEncoding = value; }
        }

        public long ContentLength {
            get { return this.response.ContentLength64; }
            set { this.response.ContentLength64 = value; }
        }

        public string ContentType {
            get { return this.response.ContentType; }
            set { this.response.ContentType = value; }
        }

        public Reactor.Net.CookieCollection Cookies {
            get { return this.response.Cookies; }
            set { this.response.Cookies = value; }
        }

        public Reactor.Net.WebHeaderCollection Headers {
            get { return this.response.Headers; }
            set { this.response.Headers = value; }
        }

        public bool KeepAlive {
            get { return this.response.KeepAlive; }
            set { this.response.KeepAlive = value; }
        }

        public Version ProtocolVersion {
            get { return this.response.ProtocolVersion; }
            set { this.response.ProtocolVersion = value; }
        }

        public string RedirectLocation {
            get { return this.response.RedirectLocation; }
            set { this.response.RedirectLocation = value; }
        }

        public bool SendChunked {
            get { return this.response.SendChunked; }
            set { this.response.SendChunked = value; }
        }

        public int StatusCode {
            get { return this.response.StatusCode; }
            set { this.response.StatusCode = value; }
        }

        public string StatusDescription {
            get { return this.response.StatusDescription; }
            set { this.response.StatusDescription = value; }
        }

        public void AddHeader(string name, string value) {
            this.response.AddHeader(name, value);
        }

        public void AppendCookie(Reactor.Net.Cookie cookie) {
            this.response.AppendCookie(cookie);
        }

        public void AppendHeader(string name, string value) {
            this.response.AppendHeader(name, value);
        }

        public void Redirect(string url) {
            this.response.Redirect(url);
        }

        public void SetCookie(Reactor.Net.Cookie cookie) {
            this.response.SetCookie(cookie);
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
        public Reactor.Future Write (byte[] buffer, int index, int count) {
            return this.Write(Reactor.Buffer.Create(buffer, 0, count));
        }

        /// <summary>
        /// Writes this data to the stream.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Future Write (byte[] buffer) {
            return this.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Writes this data to the stream.
        /// </summary>
        /// <param name="data"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Future Write (string data) {
            return this.Write(System.Text.Encoding.UTF8.GetBytes(data));
        }

        /// <summary>
        /// Writes this data to the stream.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Future Write (string format, params object[] args) {
            format = string.Format(format, args);
            return this.Write(System.Text.Encoding.UTF8.GetBytes(format));
        }

        /// <summary>
        /// Writes this data to the stream.
        /// </summary>
        /// <param name="data"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Future Write (byte data) {
            return this.Write(new byte[1] { data });
        }

        /// <summary>
        /// Writes a System.Boolean value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Future Write (bool value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.Int16 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Future Write (short value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.UInt16 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Future Write (ushort value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.Int32 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Future Write (int value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.UInt32 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Future Write (uint value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.Int64 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Future Write (long value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.UInt64 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Future Write (ulong value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.Single value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Future Write (float value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.Double value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Future Write (double value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        #endregion

        #region Machine

        /// <summary>
        /// Emits the ondrain event.
        /// </summary>
        private void _Drain() {
            if (this.state != State.Ended) {
                this.ondrain.Emit();
            }
        }

        /// <summary>
        /// Emits the _Error event.
        /// </summary>
        /// <param name="error"></param>
        private void _Error(Exception error) {
            if (this.state != State.Ended) {
                this.onerror.Emit(error);
                this._End();
            }
        }

        /// <summary>
        /// Emits the 'end' event and disposes.
        /// </summary>
        private void _End() {
            if (this.state != State.Ended) {
                this.state = State.Ended;
                this.writer.Dispose();
                this.onend.Emit();
            }
        }

        #endregion
    }
}
