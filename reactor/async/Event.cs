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
            public bool           Once   { get; set; }
            public Reactor.Action Action { get; set; }
        }

        #endregion

        private bool           multicast;
        private List<Callback> callbacks;

        #region Constructors

        /// <summary>
        /// Creates a new Event.
        /// </summary>
        /// <param name="multicast"></param>
        public Event(bool multicast) {
            this.multicast = multicast;
            this.callbacks = new List<Callback>();
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
            get {  return this.multicast; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Subscribes this action to this event.
        /// </summary>
        /// <param name="action"></param>
        public void On (Reactor.Action action) {
            var callback = new Callback{Action = action, Once = false};
            if (this.multicast) {
                this.callbacks.Add(callback);
            }
            else {
                if (this.callbacks.Count == 0) {
                    this.callbacks.Add(callback);
                }
                else { 
                    this.callbacks[0] = callback;
                }
            }
        }

        /// <summary>
        /// Subscribes this action once to this event.
        /// </summary>
        /// <param name="callback"></param>
        public void Once (Reactor.Action action) {
            var callback = new Callback{Action = action, Once = true};
            if (this.multicast) {
                this.callbacks.Add(callback);
            }
            else {
                if (this.callbacks.Count == 0) {
                    this.callbacks.Add(callback);
                }
                else { 
                    this.callbacks[0] = callback;
                }
            }
        }

        /// <summary>
        /// Unsubscribes this action from this event.
        /// </summary>
        /// <param name="action"></param>
        public void Remove (Reactor.Action action) {
            for (int i = 0; i < this.callbacks.Count; i++) {
                if (this.callbacks[i].Action == action) {
                    this.callbacks.Remove(this.callbacks[i]);
                }
            }
        }

        /// <summary>
        /// Gets all actions associated with this event.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Reactor.Action> Subscribers () {
            var clone = new List<Reactor.Action>();
            foreach (var callback in this.callbacks) {
                clone.Add(callback.Action);
            }
            return clone;
        }

        /// <summary>
        /// Emits this event.
        /// </summary>
        /// <param name="data"></param>
        public void Emit () {
            for (int i = 0; i < this.callbacks.Count; i++) {
                var callback = this.callbacks[i];
                if (callback.Once) {
                    this.callbacks.Remove(callback);
                }
                callback.Action();
            }           
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Disposes of this event.
        /// </summary>
        public void Dispose() {
            this.callbacks.Clear();
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
            public bool              Once   { get; set; }
            public Reactor.Action<T> Action { get; set; }
        }

        #endregion

        private bool              multicast;
        private List<Callback>    callbacks;

        #region Constructors

        /// <summary>
        /// Creates a new event.
        /// </summary>
        /// <param name="multicast"></param>
        public Event (bool multicast) {
            this.multicast = multicast;
            this.callbacks = new List<Callback>();
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
            get {  return this.multicast; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Subscribes this action to this event.
        /// </summary>
        /// <param name="callback"></param>
        public void On (Reactor.Action<T> action) {
            var callback = new Callback{Action = action, Once = false};
            if (this.multicast) {
                this.callbacks.Add(callback);
            }
            else {
                if (this.callbacks.Count == 0)  {
                    this.callbacks.Add(callback);
                }
                else {
                    this.callbacks[0] = callback;
                }
            }
        }

        /// <summary>
        /// Subscribes this action once to this event.
        /// </summary>
        /// <param name="callback"></param>
        public void Once (Reactor.Action<T> action) {
            var callback = new Callback{Action = action, Once = true};
            if (this.multicast) {
                this.callbacks.Add(callback);
            }
            else {
                if (this.callbacks.Count == 0)  {
                    this.callbacks.Add(callback);
                }
                else {
                    this.callbacks[0] = callback;
                }
            }
        }

        /// <summary>
        /// Unsubscribes this callback from this event.
        /// </summary>
        public void Remove (Reactor.Action<T> action) {
            for (int i = 0; i < this.callbacks.Count; i++) {
                if (this.callbacks[i].Action == action) {
                    this.callbacks.Remove(this.callbacks[i]);
                }
            }
        }

        /// <summary>
        /// Gets all listeners for this event.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Action<T>> Subscribers() {
            var clone = new List<Reactor.Action<T>>();
            foreach (var callback in this.callbacks) {
                clone.Add(callback.Action);
            }
            return clone;
        }

        /// <summary>
        /// Emits this event.
        /// </summary>
        /// <param name="data"></param>
        public void Emit (T data) {
            for (int i = 0; i < this.callbacks.Count; i++) {
                var callback = this.callbacks[i];
                if (callback.Once) {
                    this.callbacks.Remove(callback);
                }
                callback.Action(data);
            } 
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Disposes of this event.
        /// </summary>
        public void Dispose() {
            this.callbacks.Clear();
        }

        #endregion
    }
}