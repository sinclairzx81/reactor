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

namespace Reactor {

    /// <summary>
    /// A interface for all readable streams.
    /// </summary>
    public interface IReadable {

        /// <summary>
        /// OnReadable is raised when this stream has data.
        /// </summary>
        /// <param name="callback">Callback to be notified of data.</param>
        void              OnReadable      (Reactor.Action callback);

        /// <summary>
        /// Reads all data from the stream buffer. Calling this 
        /// method resume the stream in 'non flowing' mode.
        /// </summary>
        /// <returns>A Reactor Buffer</returns>
        Reactor.Buffer    Read            ();

        /// <summary>
        /// Reads this many bytes from the stream buffer. If reading
        /// past the end of the stream buffer, will truncate the resulting
        /// buffer to match the stream buffer size. Once all data has
        /// been read from the stream buffer, reading will resume in
        /// 'non flowing' mode.
        /// </summary>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns></returns>
        Reactor.Buffer    Read            (int count);

        /// <summary>
        /// Moves this buffer back to the readable stream.
        /// </summary>
        /// <param name="buffer"></param>
        void              Unshift         (Reactor.Buffer buffer);

        /// <summary>
        /// OnRead is raised when data is received. Calling this method will 
        /// begin reading the stream and switch the stream mode to 'flowing'.
        /// </summary>
        /// <param name="callback">Callback to receive data.</param>
        void              OnRead          (Reactor.Action<Reactor.Buffer> callback);

        /// <summary>
        /// OnError is raised when a error has occured on this stream. Will
        /// immediately be followed by a OnEnd event.
        /// </summary>
        /// <param name="callback">A callback to receive the error.</param>
        void              OnError         (Reactor.Action<Exception>      callback);

        /// <summary>
        /// OnEnd is raised when reaching the end of this stream.
        /// </summary>
        /// <param name="callback">A callback to be notified.</param>
        void              OnEnd           (Reactor.Action                 callback);

        /// <summary>
        /// Removes the OnReadable callback
        /// </summary>
        /// <param name="callback">The callback to remove</param>
        void              RemoveReadable  (Reactor.Action callback);

        /// <summary>
        /// Removes this OnRead callback.
        /// </summary>
        /// <param name="callback">The callback to remove</param>
        void              RemoveRead      (Reactor.Action<Reactor.Buffer> callback);

        /// <summary>
        /// Removes the OnError callback.
        /// </summary>
        /// <param name="callback">The callback to remove.</param>
        void              RemoveError     (Reactor.Action<Exception> callback);

        /// <summary>
        /// Removes the OnEnd callback.
        /// </summary>
        /// <param name="callback">The callback to remove.</param>
        void              RemoveEnd       (Reactor.Action callback);

        /// <summary>
        /// Pauses the stream from reading when in 'flowing' mode.
        /// </summary>
        void              Pause           ();

        /// <summary>
        /// Resumes the stream. Calling this method results in the stream
        /// entering 'flowing' mode.
        /// </summary>
        void              Resume          ();

        /// <summary>
        /// Pipes data from this readstream to this writestream.
        /// </summary>
        /// <param name="writeable">The stream to pipe to.</param>
        /// <returns>This stream.</returns>
        Reactor.IReadable Pipe            (Reactor.IWritable writeable);
    }
}
