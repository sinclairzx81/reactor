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

namespace Reactor {

    /// <summary>
    /// The reactor event loop.
    /// </summary>
    /// <example><![CDATA[
    ///     // Console based application will typically start
    ///     // the reactor event loop with the following call..
    /// 
    ///     Reactor.Loop.Start();
    /// 
    ///     // callers can schedule actions on the event loop with
    ///     // the following call.
    /// 
    ///     Reactor.Loop.Post(() => {
    ///         // this work is scheduled.
    ///     });
    /// 
    ///     // whenever a action is scheduled the loop, it will
    ///     // be added to a queue and executed in order. Once
    ///     // the action is ready to execute, it will be synchronized
    ///     // with a synchronization context.
    /// 
    ///     // the following will start the loop with a default 
    ///     // synchronization context.
    ///     
    ///     Reactor.Loop.Start(); 
    /// 
    ///     // the following will start the loop using the given 
    ///     // synchronization context. (typical for win forms)
    ///     Reactor.Loop.Start(System.Threading.SynchronizationContext.Current);
    /// ]]>
    /// </example>  
    /// <example><![CDATA[
    ///     // in environments that do not support .net thread
    ///     // synchronization, reactor allows callers to manually 
    ///     // enumerate actions on the loop.
    ///     
    ///     foreach(var action in Reactor.Loop.Enumerator()) {
    ///         action(); // execute the action.
    ///     }
    /// ]]>
    /// </example>    
    /// <example><![CDATA[
    ///     // it is possible to catch exceptions occuring on 
    ///     // the event loop. The reactor loop contains the
    ///     // Catch(...) event handler to obtain these exceptions.
    ///     // by subscribing to the catch handler, the event
    ///     // loop will push the exception up, and give the 
    ///     // caller the option to throw. note: the default
    ///     // behaviour is to have exceptions thrown.
    ///     
    ///     Reactor.Loop.Catch(exception => {
    ///         // handle 
    ///     });
    /// 
    ///     Reactor.Loop.Post(() => {
    ///         throw new Exception();
    ///     }); 
    /// 
    ///     // for users without access to a synchronization context
    ///     // the following will have a similar result.
    /// 
    ///     foreach(var action in Reactor.Loop.Enumerator()) {
    ///         try {
    ///             action(); // run the action on the loop.
    ///         } catch(Exception e) {
    ///             // handle 
    ///         }
    ///     }
    /// ]]>
    /// </example>       
    public static class Loop {

        #region Fields

        private static Reactor.Event<System.Exception>         onerror;
        private static Reactor.ConcurrentQueue<Reactor.Action> queue;
        private static System.Threading.ManualResetEvent       reset;
        private static System.Threading.Thread                 thread;
        private static System.Threading.SynchronizationContext context;
        private static System.Boolean                          started;
        private static bool intercept_errors;

        #endregion

        #region Constructor

        static Loop() {
            queue   = new Reactor.ConcurrentQueue<Reactor.Action>();
            onerror = new Reactor.Event<System.Exception>(false);
            intercept_errors = false;
        }

        #endregion

        #region Events

        /// <summary>
        /// Attaches a handler to catch exception on the event loop.
        /// By subscribing, the event loop will run each action 
        /// in a try/catch and emit the exception to the caller.
        /// </summary>
        /// <param name="action"></param>
        public static void Catch(Action<System.Exception> action) {
            intercept_errors = true;
            onerror.On(action);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Posts this action to the event loop.
        /// </summary>
        /// <param name="action">The action to run.</param>
        public static void Post(Reactor.Action action) {
            queue.Enqueue(action);
            if (reset != null)
                reset.Set();
        }

        /// <summary>
        /// Returns a IEnumerable set of actions within the event loop. Actions
        /// which are returned are dequeued from the event loop.
        /// </summary>
        /// <returns></returns>
        public static System.Collections.Generic.IEnumerable<Reactor.Action> Enumerator() {
            while (queue.Count > 0) {
                Reactor.Action action = null;
                if (queue.TryDequeue(out action))
                    yield return action;
            }
        }

        /// <summary>
        /// Starts processing actions on the event loop with the given synchronization context.
        /// </summary>
        /// <param name="synchronizationContext">A synchronization context to execute each action on.</param>
        public static void Start(System.Threading.SynchronizationContext synchronizationContext) {
            if (!started) {
                Loop.started = true;
                Loop.context = synchronizationContext;
                Loop.reset   = new System.Threading.ManualResetEvent(true);
                Loop.thread  = new System.Threading.Thread(() => {
                    while (started) {
                        Loop.reset.Reset();
                        Loop.reset.WaitOne();
                        foreach (var action in Enumerator()) {
                            Loop.context.Post(s => {
                                if (intercept_errors) {
                                    try {
                                        action();
                                    } catch(System.Exception exception) {
                                        onerror.Emit(exception);
                                    }
                                } else action();
                            }, null);
                        }
                    }
                }); Loop.thread.Start();
            }
        }

        /// <summary>
        /// Starts processing actions on the event loop with a new synchronization context.
        /// </summary>
        public static void Start() {
            Start(new System.Threading.SynchronizationContext());
        }

        /// <summary>
        /// Stops the loop.
        /// </summary>
        public static void Stop() {
            started = false;
            reset.Close();
            reset = null;
        }

        #endregion
    }
}
