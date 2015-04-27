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
        /// Subscribes this action to the OnDrain event.
        /// </summary>
        /// <param name="callback">A callback to receive the error.</param>
        void OnDrain   (Reactor.Action callback);

        /// <summary>
        /// Unsubscribes this action from the OnDrain event.
        /// </summary>
        /// <param name="callback">The callback to remove.</param>
        void RemoveDrain (Reactor.Action callback);

        /// <summary>
        /// Subscribes this action to the OnError event.
        /// </summary>
        /// <param name="callback">A callback to receive the error.</param>
        void OnError (Reactor.Action<Exception> callback);

        /// <summary>
        /// Unsubscribes this action from the OnError event.
        /// </summary>
        /// <param name="callback">The callback to remove.</param>
        void RemoveError (Reactor.Action<Exception> callback);

        /// <summary>
        /// Subscribes this action to the OnEnd event.
        /// </summary>
        /// <param name="callback">A callback to receive the error.</param>
        void OnEnd (Reactor.Action callback);

        /// <summary>
        /// Unsubscribes this action from the OnEnd event.
        /// </summary>
        /// <param name="callback">The callback to remove.</param>
        void RemoveEnd (Reactor.Action callback);

        /// <summary>
        /// Writes this buffer to the stream.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="callback">A callback indicating when this buffer has been writen.</param>
        Reactor.Async.Future Write (Reactor.Buffer buffer);

        /// <summary>
        /// Flushes this stream.
        /// </summary>
        /// <param name="callback">A callback indicating when this stream has been flushed.</param>
        Reactor.Async.Future Flush ();

        /// <summary>
        /// Ends this stream. 
        /// </summary>
        /// <param name="callback">A callback indicating when this stream has ended.</param>
        Reactor.Async.Future End ();
    }
}
