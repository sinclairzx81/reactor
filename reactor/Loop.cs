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

using System.Collections;
using System.Collections.Generic;

namespace Reactor
{
    /// <summary>
    /// The reactor event loop.
    /// </summary>
    public sealed partial class Loop
    {
        #region Singleton

        private static object         synclock = new object();

        private static volatile Loop  instance = null;

        private static Loop           Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (synclock)
                    {
                        if (instance == null)
                        {
                            instance = new Loop();
                        }
                    }
                }

                return instance;
            }
        }

        #endregion

        private Reactor.ConcurrentQueue<Reactor.Action> Actions;

        private Loop()
        {
            this.Actions = new Reactor.ConcurrentQueue<Reactor.Action>();
        }

        #region Internals

        public static void            Post       (Reactor.Action action)
        {
            Loop.Instance.Actions.Enqueue(action);
        }

        internal static Action          Pop        ()
        {
            Reactor.Action action;

            if(Loop.Instance.Actions.TryDequeue(out action))
            {
                return action;
            }

            return null;
        }

        #endregion

        #region Publics

        /// <summary>
        /// Returns a message loop enumerator.
        /// </summary>
        /// <returns>The message loop enumerator.</returns>
        public static IEnumerator Enumerator()
        {
            Action action = Reactor.Loop.Pop();

            while (action != null)
            {
                LastAction = action;

                action();
              
                yield return 0;

                action = Reactor.Loop.Pop();
            }
        }

        #endregion

        #region Diagnostics

        public static Action LastAction { get; set; }

        public static int PendingCount
        {
            get
            {
                return Loop.Instance.Actions.Count;
            }
        }

        public static List<Reactor.Action> PendingList
        {
            get
            {
                var ret = new List<Reactor.Action>();

                foreach (var a in Loop.Instance.Actions)
                {
                    ret.Add(a);
                }

                return ret;
            }
        }

        #endregion
    }
}