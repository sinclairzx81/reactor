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

using System.Collections;
using System.Collections.Generic;

namespace Reactor
{
    /// <summary>
    /// The reactor event loop.
    /// </summary>
    public sealed partial class Loop
    {
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

        private object                SyncLock;

        private Queue<Reactor.Action> Actions;

        private Loop()
        {
            this.SyncLock = new object();

            this.Actions  = new Queue<Reactor.Action>();
        }

        #region Internals

        /// <summary>
        /// Posts an action to the event loop.
        /// </summary>
        /// <param name="action">The action to run.</param>
        public static void            Post       (Reactor.Action action)
        {
            lock (Loop.Instance.SyncLock)
            {
                Loop.Instance.Actions.Enqueue(action);
            }
        }

        /// <summary>
        /// Dequeues the next action on the event loop. if no actions, will return null.
        /// </summary>
        /// <param name="action">The action to run.</param>
        internal static Action          Pop        ()
        {
            lock (Loop.Instance.SyncLock)
            {
                if (Loop.Instance.Actions.Count > 0)
                {
                    return Loop.Instance.Actions.Dequeue();
                }
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
                lock (Loop.Instance.SyncLock)
                {
                    action();
                }
                yield return 0;

                action = Reactor.Loop.Pop();
            }
        }

        #endregion
    }
}
