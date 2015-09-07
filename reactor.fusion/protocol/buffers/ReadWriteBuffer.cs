/*--------------------------------------------------------------------------

Reactor.Fusion

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
using System.Threading;

namespace Reactor.Fusion.Protocol {

    /// <summary>
    /// ReadWriteBuffer: a generalized synchronous write / asynchronous read buffer.
    /// </summary>
    /// <typeparam name="T">The type to buffer</typeparam>
    public class ReadWriteBuffer<T> {

        #region Internals
        public class AsyncResult<T>: IAsyncResult {
            public T          Result                 {  get; private set; }
            public object     AsyncState             {  get; private set; }
            public WaitHandle AsyncWaitHandle        {  get; private set; }
            public bool       CompletedSynchronously {  get; private set; }
            public bool       IsCompleted            {  get; private set; }
            public AsyncResult(T          result,
                               object     asyncState,
                               WaitHandle waitHandle,
                               bool       completedSynchronously,
                               bool       isCompleted) {
                this.Result                 = result;
                this.AsyncState             = asyncState;
                this.AsyncWaitHandle        = waitHandle;
                this.CompletedSynchronously = completedSynchronously;
                this.IsCompleted            = IsCompleted;
            }
        }
        #endregion

        private ManualResetEvent reset;
        private Queue<T>         buffer;
        private Reactor.Queue    queue;

        public ReadWriteBuffer() {
            this.queue  = new Reactor.Queue(1);
            this.buffer = new Queue<T>();
            this.reset  = new ManualResetEvent(false);
        }

        /// <summary>
        /// Writes a element to this buffer.
        /// </summary>
        /// <param name="item"></param>
        public void Write(T item) {
            lock (this.buffer) {
                this.buffer.Enqueue(item);
                this.reset.Set();
            }
        }

        /// <summary>
        /// Reads synchronously from this buffer. 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public int Read(T [] data) {
            lock (this.buffer) {
                var index = 0;
                while ((this.buffer.Count > 0) && (index < data.Length)) {
                    data[index] = this.buffer.Dequeue();
                    index++;
                }
                return index;
            }
        }

        /// <summary>
        /// Begins a asynchronous read operation from this buffer. If no data is present,
        /// the callback will resume once data has been written.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public IAsyncResult BeginRead(T [] buffer, AsyncCallback callback, object state) {
            this.queue.Run(next => {
                if (this.buffer.Count > 0) {
                    this.reset.Reset();
                    var read = Read(buffer);
                    callback(new AsyncResult<int>(
                        result    : read,
                        asyncState: state,
                        waitHandle: null,
                        completedSynchronously: true,
                        isCompleted:true
                    )); next();
                } else {
                    ThreadPool.QueueUserWorkItem(state2 => {
                        this.reset.WaitOne();
                        this.reset.Reset();
                        var read = Read(buffer);
                        callback(new AsyncResult<int>(
                            result    : read,
                            asyncState: state2,
                            waitHandle: null,
                            completedSynchronously: false,
                            isCompleted:true
                        )); next();
                    }, state);
                }
            });
            return null;
        }

        /// <summary>
        /// Ends a asynchronous operation.
        /// </summary>
        /// <param name="asyncResult"></param>
        /// <returns></returns>
        public int EndRead(IAsyncResult asyncResult) {
            var result = asyncResult as AsyncResult<int>;
            return result.Result;
        }
    }
}
