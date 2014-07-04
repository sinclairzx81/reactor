/*--------------------------------------------------------------------------

Reactor

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
using System.Threading;

namespace Reactor
{
    public sealed partial class Loop 
    {
        private static SynchronizationContext SynchronizationContext { get; set; }

        private static Thread                 Thread                 { get; set; }

        private static bool                   Started                { get; set; }

        #region Methods

        /// <summary>
        /// Starts a background thread to process the event loop. Will attempt to use
        /// the current SynchronizationContext if available, otherwise, will run events
        /// in the background thread.
        /// </summary>
        public static void Start()
        {
            Loop.Start(SynchronizationContext.Current);
        }

        /// <summary>
        /// Starts a background thread to process the event loop. The events
        /// will be executed on the supplied SynchronizationContext.
        /// </summary>
        /// <param name="SynchronizationContext">The synchronization context</param>
        public static void Start(SynchronizationContext SynchronizationContext)
        {
            if(!Loop.Started)
            {
                Loop.SynchronizationContext = SynchronizationContext;

                Loop.Thread                 = new Thread(Loop.Processor);

                Loop.Started                = true;

                Loop.Thread.Start();
            }
        }

        /// <summary>
        /// Stop processing the event loop.
        /// </summary>
        public static void Stop()
        {
            Loop.Started = false;
        }

        #endregion

        #region Processor

        private static void Processor()
        {
            while (Loop.Started)
            {
                if(Loop.SynchronizationContext != null)
                {
                    Loop.SynchronizationContext.Post((state) =>
                    {
                        while (Reactor.Loop.Enumerator().MoveNext())
                        {
                            
                        }

                    }, null);
                }
                else
                {
                    while (Reactor.Loop.Enumerator().MoveNext())
                    {
                        
                    }
                }

                Thread.Sleep(1);
            }
        }

        #endregion
    }
}
