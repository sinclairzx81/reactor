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

using System.Collections.Generic;
using System.IO;

namespace Reactor {

    /// <summary>
    /// Manages a internal pool of MemoryStreams on behalf of the Buffer class. 
    /// The pool enables the recycling of MemoryStream's across different buffers
    /// which aids in memory performance in Mono based platforms.
    /// </summary>
    internal static class BufferPool {

        internal class Handle {
            public bool         locked;
            public MemoryStream stream;
        }

        private static LinkedList<Handle>  handles = new LinkedList<Handle>();

        /// <summary>
        /// Aquires a new or recycled MemoryStream.
        /// </summary>
        /// <returns>A System.IO.MemoryStream.</returns>
        public static System.IO.MemoryStream Acquire() {
            lock (handles) {
                foreach (var handle in handles) {
                    if (!handle.locked) {
                        handle.locked = true;
                        return handle.stream;
                    }
                }
                var stream = new MemoryStream();
                handles.AddLast(new Handle {
                    locked = true,
                    stream = stream
                });
                return stream;
            }
        }

        /// <summary>
        /// Releases this stream to the pool.
        /// </summary>
        /// <param name="stream">The stream to release.</param>
        public static void Release(System.IO.MemoryStream stream) {
            lock (handles) {
                foreach (var handle in handles) {
                    if (handle.stream == stream) {
                        handle.locked = false;
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Collects any unlocked buffers and disposes of them.
        /// </summary>
        public static void Collect() {
            lock (handles) {
                for (var handle = handles.First; 
                         handle != handles.Last.Next; 
                         handle = handle.Next) {
                    if (!handle.Value.locked) {
                        handle.Value.stream.Dispose();
                        handles.Remove(handle);
                    }
                }
            }
        }
    }
}
