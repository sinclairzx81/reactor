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
using System.Collections.Generic;

namespace Reactor.Async {

    /// <summary>
    /// Publish / Subscribe messaging channel.
    /// </summary>
    public class Event : IDisposable {
        
        #region Callback

        /// <summary>
        /// Event Callback.
        /// </summary>
        internal class Callback {
            public bool           once;
            public Reactor.Action action;
        }

        #endregion

        internal class Fields {
            public bool multicast;
            public List<Callback> callbacks;
            public Fields() {
                this.multicast = true;
                this.callbacks = new List<Callback>();
            }
        } private Fields fields;

        #region Constructors

        /// <summary>
        /// Creates a new Event.
        /// </summary>
        /// <param name="multicast"></param>
        public Event(bool multicast) {
            this.fields = new Fields();
            this.fields.multicast = multicast;
        }

        /// <summary>
        /// Creates a new multicast event.
        /// </summary>
        public Event() : this(true) { }

        #endregion

        #region Properties

        /// <summary>
        /// Indicates if this event is multicast.
        /// </summary>
        public bool Multicast {
            get {  lock(this.fields) return this.fields.multicast; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Subscribes this action to this event.
        /// </summary>
        /// <param name="action"></param>
        public void On (Reactor.Action action) {
            lock (this.fields) {
                var callback = new Callback{action = action, once = false};
                if (this.fields.multicast) {
                    this.fields.callbacks.Add(callback);
                }
                else {
                    if (this.fields.callbacks.Count == 0) {
                        this.fields.callbacks.Add(callback);
                    }
                    else { 
                        this.fields.callbacks[0] = callback;
                    }
                }
            }
        }

        /// <summary>
        /// Subscribes this action once to this event.
        /// </summary>
        /// <param name="callback"></param>
        public void Once (Reactor.Action action) {
            lock (this.fields) {
                var callback = new Callback{action = action, once = true};
                if (this.fields.multicast) {
                    this.fields.callbacks.Add(callback);
                }
                else {
                    if (this.fields.callbacks.Count == 0) {
                        this.fields.callbacks.Add(callback);
                    }
                    else { 
                        this.fields.callbacks[0] = callback;
                    }
                }
            }
        }

        /// <summary>
        /// Unsubscribes this action from this event.
        /// </summary>
        /// <param name="action"></param>
        public void Remove (Reactor.Action action) {
            lock (this.fields) {
                for (int i = 0; i < this.fields.callbacks.Count; i++) {
                    if (this.fields.callbacks[i].action == action) {
                        this.fields.callbacks.Remove(this.fields.callbacks[i]);
                    }
                }
            }
        }

        /// <summary>
        /// Gets all actions associated with this event.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Reactor.Action> Subscribers () {
            lock (this.fields) {
                var clone = new List<Reactor.Action>();
                foreach (var callback in this.fields.callbacks) {
                    clone.Add(callback.action);
                }
                return clone;
            }
        }

        /// <summary>
        /// Emits this event.
        /// </summary>
        /// <param name="data"></param>
        public void Emit () {
            lock (this.fields) {
                for (int i = 0; i < this.fields.callbacks.Count; i++) {
                    var callback = this.fields.callbacks[i];
                    if (callback.once) {
                        this.fields.callbacks.Remove(callback);
                    }
                    callback.action();
                }
            }
        }

        #endregion

        #region IDisposable

        private bool disposed = false;
        private void Dispose(bool disposing) {
            lock (this.fields) {
                this.fields.callbacks.Clear();
                this.disposed = true;
            }
        }

        public void Dispose() {
            this.Dispose(true);
        }

        ~Event() {
            this.Dispose(false);
        }

        #endregion

        #region Statics

        /// <summary>
        /// Creates a new event.
        /// </summary>
        /// <param name="multicast"></param>
        /// <returns></returns>
        public static Event Create(bool multicast) {
            return new Event(multicast);
        }

        /// <summary>
        /// Creates a new multicast event.
        /// </summary>
        /// <returns></returns>
        public static Event Create() {
            return new Event(true);
        }

        /// <summary>
        /// Creates a new event.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="multicast"></param>
        /// <returns></returns>
        public static Event<T> Create<T> (bool multicast){
            return new Event<T>(multicast);
        }

        /// <summary>
        /// Creates a new multicast event.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="multicast"></param>
        /// <returns></returns>
        public static Event<T> Create<T> (){
            return new Event<T>(true);
        }

        #endregion
    }

    /// <summary>
    /// Publisher / Subscriber messaging channel.
    /// </summary>
    /// <typeparam name="T">The type of message sent over this channel</typeparam>
    public class Event<T> : IDisposable {

        #region Callback

        /// <summary>
        /// Event Callback.
        /// </summary>
        internal class Callback {
            public bool              once   { get; set; }
            public Reactor.Action<T> action { get; set; }
        }

        #endregion

        #region Data

        internal class Data<T> {
            public bool multicast;
            public List<Callback> callbacks;
            public Data() {
                this.multicast = true;
                this.callbacks = new List<Callback>();
            }
        }

        #endregion

        private Data<T> data;

        #region Constructors

        /// <summary>
        /// Creates a new event.
        /// </summary>
        /// <param name="multicast"></param>
        public Event (bool multicast) {
            this.data = new Data<T>();
            this.data.multicast = multicast;
        }
        
        /// <summary>
        /// Creates a new multicast event.
        /// </summary>
        public Event (): this(true) { }

        #endregion

        #region Properties

        /// <summary>
        /// Indicates if this event is multicast.
        /// </summary>
        public bool Multicast {
            get {  lock(this.data) return this.data.multicast; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Subscribes this action to this event.
        /// </summary>
        /// <param name="callback"></param>
        public void On (Reactor.Action<T> action) {
            lock (this.data) {
                var callback = new Callback{action = action, once = false};
                if (this.data.multicast) {
                    this.data.callbacks.Add(callback);
                }
                else {
                    if (this.data.callbacks.Count == 0)  {
                        this.data.callbacks.Add(callback);
                    }
                    else {
                        this.data.callbacks[0] = callback;
                    }
                }
            }
        }

        /// <summary>
        /// Subscribes this action once to this event.
        /// </summary>
        /// <param name="callback"></param>
        public void Once (Reactor.Action<T> action) {
            lock (this.data) {
                var callback = new Callback{action = action, once = true};
                if (this.data.multicast) {
                    this.data.callbacks.Add(callback);
                }
                else {
                    if (this.data.callbacks.Count == 0)  {
                        this.data.callbacks.Add(callback);
                    }
                    else {
                        this.data.callbacks[0] = callback;
                    }
                }
            }
        }

        /// <summary>
        /// Unsubscribes this callback from this event.
        /// </summary>
        public void Remove (Reactor.Action<T> action) {
            lock (this.data) {
                for (int i = 0; i < this.data.callbacks.Count; i++) {
                    if (this.data.callbacks[i].action == action) {
                        this.data.callbacks.Remove(this.data.callbacks[i]);
                    }
                }
            }
        }

        /// <summary>
        /// Gets all listeners for this event.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Action<T>> Subscribers() {
            lock (this.data) {
                var clone = new List<Reactor.Action<T>>();
                foreach (var callback in this.data.callbacks) {
                    clone.Add(callback.action);
                }
                return clone;
            }
        }

        /// <summary>
        /// Emits this event.
        /// </summary>
        /// <param name="data"></param>
        public void Emit (T data) {
            lock (this.data) {
                for (int i = 0; i < this.data.callbacks.Count; i++) {
                    var callback = this.data.callbacks[i];
                    if (callback.once) {
                        this.data.callbacks.Remove(callback);
                    }
                    callback.action(data);
                } 
            }
        }

        #endregion

        #region IDisposable

        private bool disposed = false;
        private void Dispose(bool disposing) {
            lock (this.data) {
                this.data.callbacks.Clear();
                this.disposed = true;
            }
        }

        public void Dispose() {
            this.Dispose(true);
        }

        ~Event() {
            this.Dispose(false);
        }

        #endregion
    }
}