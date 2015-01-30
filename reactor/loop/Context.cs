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

using System.Collections;
using System.Threading;

namespace Reactor
{
    internal partial class Context
    {
        private static object synclock = new object();

        private static volatile Context instance = null;

        public static Context Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (synclock)
                    {
                        if (instance == null)
                        {
                            instance = new Context();
                        }
                    }
                }

                return instance;
            }
        }
    }

    internal partial class Context
    {
        private Runtime  runtime;

        private Messages messages;

        public Context()
        {
            this.messages = new Messages();

            this.runtime  = null;
        }

        public void Post(Reactor.Action action)
        {
            this.messages.Post(action);

            if(this.runtime != null)
            {
                this.runtime.Signal();
            }
        }

        public IEnumerator Enumerator()
        {
            return this.messages.Enumerator();
        }

        public void Start()
        {
            if(this.runtime == null)
            {
                this.runtime = new Runtime(null, messages);

                this.runtime.Start();
            }
        }

        public void Start(SynchronizationContext context)
        {
            if (this.runtime == null)
            {
                this.runtime = new Runtime(context, this.messages);

                this.runtime.Start();
            }
        }

        public void Stop()
        {
            if(this.runtime != null)
            {
                this.runtime.Stop();

                this.runtime = null;
            }
        }
    }
}
