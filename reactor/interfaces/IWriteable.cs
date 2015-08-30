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

namespace Reactor
{
    /// <summary>
    /// A interface for all writable streams.
    /// </summary>
    public interface IWritable {

        /// <summary>
        /// Subscribes this action to the 'drain' event. The event indicates
        /// when a write operation has completed and the caller should send
        /// more data.
        /// </summary>
        /// <param name="callback"></param>
        void OnDrain (Reactor.Action action);

        /// <summary>
        /// Subscribes this action once to the 'drain' event. The event indicates
        /// when a write operation has completed and the caller should send
        /// more data.
        /// </summary>
        /// <param name="callback"></param>
        void OnceDrain(Reactor.Action action);

        /// <summary>
        /// Unsubscribes this action from the OnDrain event.
        /// </summary>
        /// <param name="callback">The callback to remove.</param>
        void RemoveDrain (Reactor.Action action);

        /// <summary>
        /// Subscribes this action to the OnError event.
        /// </summary>
        /// <param name="callback">A callback to receive the error.</param>
        void OnError (Reactor.Action<Exception> action);

        /// <summary>
        /// Unsubscribes this action from the OnError event.
        /// </summary>
        /// <param name="callback">The callback to remove.</param>
        void RemoveError (Reactor.Action<Exception> action);

        /// <summary>
        /// Subscribes this action to the OnEnd event.
        /// </summary>
        /// <param name="callback">A callback to receive the error.</param>
        void OnEnd (Reactor.Action action);

        /// <summary>
        /// Unsubscribes this action from the OnEnd event.
        /// </summary>
        /// <param name="callback">The callback to remove.</param>
        void RemoveEnd (Reactor.Action action);

        /// <summary>
        /// Writes this buffer to the stream. This method returns a Reactor.Future
        /// which resolves once this buffer has been written.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        Reactor.Future Write (Reactor.Buffer buffer);

        /// <summary>
        /// Flushes this stream. This method returns a Reactor.Future which
        /// resolves once the stream has been flushed.
        /// </summary>
        /// <returns></returns>
        Reactor.Future Flush ();

        /// <summary>
        /// Ends and disposes of the underlying resource. This method returns
        /// a Reactor.Future which resolves once this stream has been ended.
        /// </summary>
        /// <returns></returns>
        Reactor.Future End ();

        /// <summary>
        /// Forces buffering of all writes. Buffered data will be 
        /// flushed either at .Uncork() or at .End() call.
        /// </summary>
        void Cork();

        /// <summary>
        /// Flush all data, buffered since .Cork() call.
        /// </summary>
        void Uncork();
    }
}
