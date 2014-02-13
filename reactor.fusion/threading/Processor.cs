/*--------------------------------------------------------------------------

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

namespace Reactor.Fusion
{
    /// <summary>
    /// Fusion Processor. A internal thread that ticks 1 time each millisecond. Sockets
    /// subscribe to the OnTick event, and must unsubscribe when finished.
    /// </summary>
    internal static class Processor
    {
        private static Thread       Thread   { get; set; }

        private static bool         Started  { get; set; }

        #region Events

        private static event Action onTick;

        public  static event Action OnTick   
        { 
            add
            {
                onTick += value;

                if(!Started) {

                    Start();
                }
            }
            remove
            {
                onTick -= value;

                if(onTick == null) {

                    Started = false;
                }
            }
        }

        #endregion

        #region Methods

        private static void Start()
        {
            Started = true;

            Thread  = new Thread(() =>
            {
                while(Started)
                {
                    if(onTick != null) {

                        onTick();
                    }

                    System.Threading.Thread.Sleep(1);
                }
            });

            Thread.Start();
        }

        private static void Stop()
        {
            Started = false;
        }

        #endregion
    }
}
