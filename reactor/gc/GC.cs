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
using System.Collections.Generic;

namespace Reactor
{
    /// <summary>
    /// GC management utilities
    /// </summary>
    public class GC 
    {
        #region Singleton

        private static object synclock = new object();

        private static volatile GC instance = null;

        private static GC Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (synclock)
                    {
                        if (instance == null)
                        {
                            instance = new GC();
                        }
                    }
                }

                return instance;
            }
        }

        private List<object> pins;
        
        private GC()
        {
            this.pins = new List<object>();
        }

        /// <summary>
        /// Protects this object against GC collection. Callers are expected to call Unpin() when finished.
        /// </summary>
        /// <param name="obj">The object to pin.</param>
        private void PinObject(object obj)
        {
            var collection = (ICollection)this.pins;

            lock (collection.SyncRoot)
            {
                if (!this.pins.Contains(obj))
                {
                    this.pins.Add(obj);
                }
            }
        }

        /// <summary>
        /// Unpins this object. Allowing the GC to collect when ready.
        /// </summary>
        /// <param name="obj">The object to unpin</param>
        private void UnPinObject(object obj)
        {
            var collection = (ICollection)this.pins;

            lock (collection.SyncRoot)
            {
                if (this.pins.Contains(obj))
                {
                    this.pins.Remove(obj);
                }
            }
        }

        #endregion

        #region Statics

        /// <summary>
        /// Protects this object against GC collection. Callers are expected to call Unpin() when finished.
        /// </summary>
        /// <param name="obj">The object to pin.</param>
        public static void Pin(object obj)
        {
            var instance = GC.Instance;

            instance.PinObject(obj);
        }

        /// <summary>
        /// Unpins this object. Allowing the GC to collect when ready.
        /// </summary>
        /// <param name="obj">The object to unpin</param>
        public static void UnPin(object obj)
        {
            var instance = GC.Instance;

            instance.UnPinObject(obj);
        }

        #endregion

        #region Experimental

        /// <summary>
        /// (experimental) Protects this object against GC collection for a set period of time.
        /// </summary>
        /// <param name="obj">The object to pin.</param>
        /// <param name="timeout">The timeout (in milliseconds) in which this object should be pinned.</param>
        public static void Pin(object obj, int timeout)
        {
            var instance = GC.Instance;

            instance.PinObject(obj);

            Reactor.Timeout.Create(() =>
            {
                instance.UnPinObject(obj);

            }, timeout);
        }
        /// <summary>
        /// (experimental) Protects this object against GC collection for a set period of time.
        /// </summary>
        /// <param name="obj">The object to pin.</param>
        /// <param name="timeout">The timeout (in milliseconds) in which this object should be pinned.</param>
        /// <param name="callback">The callback triggered when the timeout completes.</param>
        public static void Pin(object obj, int timeout, Reactor.Action callback)
        {
            var instance = GC.Instance;

            instance.PinObject(obj);

            Reactor.Timeout.Create(() =>
            {
                instance.UnPinObject(obj);

                callback();

            }, timeout);
        }

        #endregion
    }
}
