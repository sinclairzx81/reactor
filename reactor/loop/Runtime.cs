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

using System.Threading;

namespace Reactor
{
    internal class Runtime
    {
        private ManualResetEvent       manualresetevent;

        private SynchronizationContext synchronizationcontext;

        private Messages               messages;

        private Thread                 thread;

        private bool                   started;

        public Runtime(SynchronizationContext synchronizationcontext, Messages messages)
        {
            this.manualresetevent       = new ManualResetEvent(false);

            this.synchronizationcontext = synchronizationcontext;

            this.messages               = messages;

            this.started                = false;

            this.thread                 = new Thread(this.Process);
        }

        public void Signal()
        {
            this.manualresetevent.Set();
        }

        public void Start()
        {
            if (!this.started)
            {
                this.started = true;

                this.thread.Start();
            }
        }

        public void Stop()
        {
            this.started = false;

            this.Signal();
        }

        private void Process()
        {
            while (this.started)
            {
                if (this.synchronizationcontext != null)
                {
                    this.synchronizationcontext.Post((state) =>
                    {
                        while (this.messages.Enumerator().MoveNext())
                        {

                        }

                    }, null);
                }
                else
                {
                    while (this.messages.Enumerator().MoveNext())
                    {

                    }
                }
                
                this.manualresetevent.WaitOne();

                this.manualresetevent.Reset();
            }
        }
    }
}
