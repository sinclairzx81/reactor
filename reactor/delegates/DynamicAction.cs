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

using System;
using System.Reflection;

namespace Reactor
{
    /// <summary>
    /// DynamicAction: Dynamic actions based on the Type arguments supplied.
    /// </summary>
    internal class DynamicAction
    {
        public Delegate Delegate { get; set; }

        private Reactor.Action<object[]> callback { get; set; }

        public DynamicAction(Type[] types, Reactor.Action<object[]> callback)
        {
            this.callback = callback;

            if (types.Length == 0)
            {
                Type actionType = typeof(Reactor.Action);

                var methodinfo  = this.GetType().GetMethod("Handler0", BindingFlags.NonPublic | BindingFlags.Instance);

                this.Delegate   = Delegate.CreateDelegate(actionType, this, methodinfo);
            }

            if (types.Length == 1)
            {
                Type actionType = typeof(Reactor.Action<>);

                Type genericActionType = actionType.MakeGenericType(new Type[] { types[0] });

                var methodinfo = this.GetType().GetMethod("Handler1", BindingFlags.NonPublic | BindingFlags.Instance);

                var genericMethodInfo = methodinfo.MakeGenericMethod(new Type[] { types[0] });

                this.Delegate = Delegate.CreateDelegate(genericActionType, this, genericMethodInfo);
            }

            if (types.Length == 2)
            {
                Type actionType = typeof(Reactor.Action<,>);

                Type genericActionType = actionType.MakeGenericType(new Type[] { types[0], types[1] });

                var methodinfo = this.GetType().GetMethod("Handler2", BindingFlags.NonPublic | BindingFlags.Instance);

                var genericMethodInfo = methodinfo.MakeGenericMethod(new Type[] { types[0], types[1] });

                this.Delegate = Delegate.CreateDelegate(genericActionType, this, genericMethodInfo);
            }
            if (types.Length == 3)
            {
                Type actionType = typeof(Reactor.Action<,,>);

                Type genericActionType = actionType.MakeGenericType(new Type[] { types[0], types[1], types[2] });

                var methodinfo = this.GetType().GetMethod("Handler3", BindingFlags.NonPublic | BindingFlags.Instance);

                var genericMethodInfo = methodinfo.MakeGenericMethod(new Type[] { types[0], types[1], types[2] });

                this.Delegate = Delegate.CreateDelegate(genericActionType, this, genericMethodInfo);
            }

            if (types.Length == 4)
            {
                Type actionType = typeof(Reactor.Action<,,,>);

                Type genericActionType = actionType.MakeGenericType(new Type[] { types[0], types[1], types[2], types[3] });

                var methodinfo = this.GetType().GetMethod("Handler4", BindingFlags.NonPublic | BindingFlags.Instance);

                var genericMethodInfo = methodinfo.MakeGenericMethod(new Type[] { types[0], types[1], types[2], types[3] });

                this.Delegate = Delegate.CreateDelegate(genericActionType, this, genericMethodInfo);
            }
        }

        #region Methods

        private void Handler0()
        {
            callback(new object[] { });
        }

        private void Handler1<T0>(T0 arg0)
        {
            callback(new object[] { arg0 });
        }

        private void Handler2<T0, T1>(T0 arg0, T1 arg1)
        {
            callback(new object[] { arg0, arg1 });
        }

        private void Handler3<T0, T1, T2>(T0 arg0, T1 arg1, T2 arg2)
        {
            callback(new object[] { arg0, arg1, arg2 });
        }

        private void Handler4<T0, T1, T2, T3>(T0 arg0, T1 arg1, T2 arg2, T3 arg3)
        {
            callback(new object[] { arg0, arg1, arg2, arg3 });
        }

        #endregion

        #region Statics

        /// <summary>
        /// Creates dynamic delegate.
        /// </summary>
        /// <param name="types">The type arguments (typically parsed as a generic argument list)</param>
        /// <param name="callback">The callback to receive results</param>
        /// <returns>A Delegate</returns>
        public static Delegate Create(Type[] types, Reactor.Action<object[]> callback)
        {
            var d = new DynamicAction(types, callback);

            return d.Delegate;
        }

        #endregion
    }
}
