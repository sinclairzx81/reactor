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

namespace Reactor {

    /// <summary>
    /// Reactor Future.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Future<T> {

        #region State
        
        internal enum State {
            /// <summary>
            /// A state indicating a pending state.
            /// </summary>
            Pending,
            /// <summary>
            /// A state indicating a rejected state.
            /// </summary>
            Rejected,
            /// <summary>
            /// A state indicating a resolved state.
            /// </summary>
            Resolved
        };

        #endregion

        #region Fields

        internal class Fields<T> {
            public Exception                       error;
            public T                               value;
            public State                           state;
            public List<Reactor.Action<Exception>> errors;
            public List<Reactor.Action<T>>         thens;
            public Fields() {
                this.error  = null;
                this.value  = default(T);
                this.state  = State.Pending;
                this.errors = new List<Reactor.Action<Exception>>();
                this.thens  = new List<Reactor.Action<T>>();
            }
        } private Fields<T> fields;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new future.
        /// </summary>
        internal Future() {
            this.fields = new Fields<T>();
        }

        /// <summary>
        /// Creates a new future with resolved value.
        /// </summary>
        /// <param name="value"></param>
        public Future(T value) {
            this.fields = new Fields<T>();
            this.fields.state = State.Resolved;
            this.fields.value = value;
            this.fields.error = null;
        }

        /// <summary>
        /// Creates a new future.
        /// </summary>
        /// <param name="resolver">The resolve / reject function.</param>
        public Future(Action<Action<T>, Action<Exception>> resolver) {
            this.fields = new Fields<T>();
            try {
                resolver(this.Resolve, this.Reject);
            }
            catch(Exception error) {
                this.Reject(error);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Assigns a action which is called on a resolved state.
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public Reactor.Future Then  (Reactor.Action<T> callback) {
            lock (this.fields) {
                var future  = new Reactor.Future();
                var reject  = new Reactor.Action<Exception>(error => {
                    future.Reject(error);
                });
                var resolve = new Reactor.Action<T>(value => {
                    try { callback(value); future.Resolve(); }
                    catch (Exception error) { future.Reject(error); }
                });
                switch (this.fields.state) {
                    case State.Resolved:
                        resolve(this.fields.value);
                        break;
                    case State.Rejected:
                        reject(this.fields.error);
                        break;
                    case State.Pending:
                        this.fields.thens.Add(resolve);
                        this.fields.errors.Add(reject);
                        break;
                }
                return future;
            }
        }

        /// <summary>
        /// Assigns a action which is called on a resolved state.
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public Reactor.Future<TResult> Then<TResult>  (Reactor.Func<T, TResult> callback) {
            lock (this.fields) {
                var future  = new Reactor.Future<TResult>();
                var reject  = new Reactor.Action<Exception>(error => {
                    future.Reject(error);
                });
                var resolve = new Reactor.Action<T>(value => {
                    try { future.Resolve(callback(value)); }
                    catch (Exception error) { future.Reject(error); }
                });
                switch (this.fields.state) {
                    case State.Resolved:
                        resolve(this.fields.value);
                        break;
                    case State.Rejected:
                        reject(this.fields.error);
                        break;
                    case State.Pending:
                        this.fields.thens.Add(resolve);
                        this.fields.errors.Add(reject);
                        break;
                }
                return future;
            }
        }

        /// <summary>
        /// Assigns a action which is called on a rejected state.
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public Reactor.Future Error (Reactor.Action<Exception> callback) {
            lock (this.fields) {
                var future  = new Reactor.Future();
                var resolve = new Reactor.Action<T>(value => {
                    future.Resolve();
                });
                var reject  = new Reactor.Action<Exception>(error => {
                    callback(error);
                    future.Resolve();
                });
                switch (this.fields.state) {
                    case State.Resolved:
                        resolve(this.fields.value);
                        break;
                    case State.Rejected:
                        reject(this.fields.error);
                        break;
                    case State.Pending:
                        this.fields.thens.Add(resolve);
                        this.fields.errors.Add(reject);
                        break;
                }
                return future;
            }
        }

        /// <summary>
        /// Assigns a action which is called irrespective of a rejected / resolved state.
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public Reactor.Future Finally (Reactor.Action callback) {
            lock (this.fields) {
                var future  = new Reactor.Future();
                var resolve = new Reactor.Action<T>(value => {
                    callback();
                    future.Resolve();
                });
                var reject  = new Reactor.Action<Exception>(error => {
                    callback();
                    future.Reject(error);
                });
                switch (this.fields.state) {
                    case State.Resolved:
                        resolve(this.fields.value);
                        break;
                    case State.Rejected:
                        reject(this.fields.error);
                        break;
                    case State.Pending:
                        this.fields.thens.Add(resolve);
                        this.fields.errors.Add(reject);
                        break;
                }            
                return future;
            }
        }

        #endregion

        #region Internals

        /// <summary>
        /// Rejects this future.
        /// </summary>
        /// <param name="error"></param>
        internal void Reject(Exception error) {
            lock (this.fields) {
                if (this.fields.state != State.Pending) {
                    return;
                }
                this.fields.error = error;
                this.fields.state = State.Rejected;
                foreach (var handler in this.fields.errors) {
                    handler(error);
                }
                this.fields.thens.Clear();
                this.fields.errors.Clear();
            }
        }

        /// <summary>
        /// Resolves this future.
        /// </summary>
        /// <param name="value"></param>
        internal void Resolve(T value) {
            lock (this.fields) {
                if (this.fields.state != State.Pending) {
                    return;
                }
                this.fields.value = value;
                this.fields.state = State.Resolved;
                foreach (var handler in this.fields.thens) {
                    handler(value);
                }
                this.fields.thens.Clear();
                this.fields.errors.Clear();
            }
        }

        #endregion
    }

    /// <summary>
    /// Reactor Future.
    /// </summary>
    public class Future {

        #region State

        internal enum State {
            /// <summary>
            /// A state indicating a pending state.
            /// </summary>
            Pending,
            /// <summary>
            /// A state indicating a rejected state.
            /// </summary>
            Rejected,
            /// <summary>
            /// A state indicating a resolved state.
            /// </summary>
            Resolved
        };

        #endregion

        #region Fields

        internal class Fields {
            public Exception                       error;
            public State                           state;
            public List<Reactor.Action<Exception>> errors;
            public List<Reactor.Action>            thens;
            public Fields() {
                this.error  = null;
                this.state  = State.Pending;
                this.errors = new List<Reactor.Action<Exception>>();
                this.thens  = new List<Reactor.Action>();
            }
        } private Fields fields;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new future.
        /// </summary>
        internal Future() {
            this.fields = new Fields();
        }

        /// <summary>
        /// Creates a new future. 
        /// </summary>
        /// <param name="resolver">The resolve / reject function.</param>
        public  Future(Reactor.Action<Reactor.Action, Reactor.Action<Exception>> resolver) {
            this.fields = new Fields();
            try {
                resolver(this.Resolve, this.Reject);
            }
            catch(Exception error) {
                this.Reject(error);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Assigns a action which is called on a resolved state.
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public Reactor.Future Then (Reactor.Action callback) {
            lock (this.fields) {
                var future  = new Reactor.Future();
                var reject  = new Reactor.Action<Exception>(error => {
                    future.Reject(error);
                });
                var resolve = new Reactor.Action(() => {
                    try { callback(); future.Resolve(); }
                    catch (Exception error) { future.Reject(error); }
                });
                switch (this.fields.state) {
                    case State.Resolved:
                        resolve();
                        break;
                    case State.Rejected:
                        reject(this.fields.error);
                        break;
                    case State.Pending:
                        this.fields.thens.Add(resolve);
                        this.fields.errors.Add(reject);
                        break;
                }
                return future;
            }
        }

        /// <summary>
        /// Assigns a action which is called on a resolved state.
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public Reactor.Future<TResult> Then<TResult> (Reactor.Func<TResult> callback) {
            lock (this.fields) {
                var future  = new Reactor.Future<TResult>();
                var reject  = new Reactor.Action<Exception>(error => {
                    future.Reject(error);
                });
                var resolve = new Reactor.Action(() => {
                    try { future.Resolve(callback()); }
                    catch (Exception error) { future.Reject(error); }
                });
                switch (this.fields.state) {
                    case State.Resolved:
                        resolve();
                        break;
                    case State.Rejected:
                        reject(this.fields.error);
                        break;
                    case State.Pending:
                        this.fields.thens.Add(resolve);
                        this.fields.errors.Add(reject);
                        break;
                }
                return future;
            }
        }

        /// <summary>
        /// Assigns a action which is called on a rejected state.
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public Reactor.Future Error (Reactor.Action<Exception> callback) {
            lock (this.fields) {
                var future  = new Reactor.Future();
                var resolve = new Reactor.Action(() => {
                    future.Resolve();
                });
                var reject  = new Reactor.Action<Exception>(error => {
                    callback(error);
                    future.Resolve();
                });
                switch (this.fields.state) {
                    case State.Resolved:
                        resolve();
                        break;
                    case State.Rejected:
                        reject(this.fields.error);
                        break;
                    case State.Pending:
                        this.fields.thens.Add(resolve);
                        this.fields.errors.Add(reject);
                        break;
                }            
                return future;
            }
        }

        /// <summary>
        /// Assigns a action which is called irrespective of a rejected / resolved state.
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public Reactor.Future Finally (Reactor.Action callback) {
            lock (this.fields) {
                var future  = new Reactor.Future();
                var resolve = new Reactor.Action(() => {
                    callback();
                    future.Resolve();
                });
                var reject  = new Reactor.Action<Exception>(error => {
                    callback();
                    future.Reject(error);
                });
                switch (this.fields.state) {
                    case State.Resolved:
                        resolve();
                        break;
                    case State.Rejected:
                        reject(this.fields.error);
                        break;
                    case State.Pending:
                        this.fields.thens.Add(resolve);
                        this.fields.errors.Add(reject);
                        break;
                }            
                return future;
            }
        }

        #endregion

        #region Internals

        internal void Reject(Exception error) {
            lock (this.fields) {
                if (this.fields.state != State.Pending) {
                    return;
                }
                this.fields.error = error;
                this.fields.state = State.Rejected;
                foreach (var handler in this.fields.errors) {
                    handler(error);
                }
                this.fields.thens.Clear();
                this.fields.errors.Clear();
            }
        }

        internal void Resolve() {
            lock (this.fields) {
                if (this.fields.state != State.Pending) {
                    return;
                }
                this.fields.state = State.Resolved;
                foreach (var handler in this.fields.thens) {
                    handler();
                }
                this.fields.thens.Clear();
                this.fields.errors.Clear();
            }
        }

        #endregion

        #region Statics

        /// <summary>
        /// Returns a new Future.
        /// </summary>
        /// <param name="resolver"></param>
        /// <returns></returns>
        public static Reactor.Future Create(Reactor.Action<Action, Action<Exception>> resolver) {
            return new Reactor.Future(resolver);
        }

        /// <summary>
        /// Returns a new Future.
        /// </summary>
        /// <param name="resolver"></param>
        /// <returns></returns>
        public static Reactor.Future<T> Create<T>(Reactor.Action<Action<T>, Action<Exception>> resolver) {
            return new Reactor.Future<T>(resolver);
        }

        /// <summary>
        /// Returns a resolved future.
        /// </summary>
        /// <returns></returns>
        public static Reactor.Future Resolved() {
            return new Reactor.Future((resolve, reject) => resolve());
        }

        /// <summary>
        /// Returns a resolved future.
        /// </summary>
        /// <param name="value">The resolved value.</param>
        /// <returns></returns>
        public static Reactor.Future<T> Resolved<T>(T value) {
            return new Reactor.Future<T>((resolve, reject) => resolve(value));
        }

        /// <summary>
        /// Returns a rejected future.
        /// </summary>
        /// <returns></returns>
        public static Reactor.Future Rejected(Exception error) {
            return new Reactor.Future((resolve, reject) => reject(error));
        }

        /// <summary>
        /// Returns a rejected future.
        /// </summary>
        /// <param name="value">The rejected value.</param>
        /// <returns></returns>
        public static Reactor.Future<T> Rejected<T>(Exception error) {
            return new Reactor.Future<T>((resolve, reject) => reject(error));
        }

        /// <summary>
        /// Returns a future that resolves when all of the futures in the iterable argument have been resolved. Each
        /// future is run in parallel, if any one future rejects, then it is immediately rejected to the caller.
        /// </summary>
        /// <param name="futures"></param>
        /// <returns></returns>
        public static Reactor.Future All(IEnumerable<Reactor.Future> futures) {
            return new Reactor.Future((resolve, reject) => {
                var count = 0;
                foreach (var future in futures) {
                    count++;
                    future.Then(() => {
                        count--;
                        if (count == 0) { 
                            resolve();
                        }
                    }).Error(reject);
                }
            });
        }

        /// <summary>
        /// Returns a future that resolves when all of the futures in the iterable argument have been resolved. Each
        /// future is run in parallel, if any one future rejects, then it is immediately rejected to the caller.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="futures"></param>
        /// <returns></returns>
        public static Reactor.Future<IEnumerable<T>> All<T>(IEnumerable<Reactor.Future<T>> futures) {
            var count = 0;
            return new Reactor.Future<IEnumerable<T>>((resolve, reject) => {
                var list = new List<T>();
                foreach (var future in futures) {
                    count++;
                    future.Then(v => {
                        list.Add(v);
                        count--;
                        if(count == 0) 
                            resolve(list);
                    }).Error(reject);
                }
            });
        }

        #endregion
    }

    /// <summary>
    /// Reactor Deferred
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Deferred<T> {
        private Reactor.Future<T>         future;
        private Reactor.Action<T>         resolve;
        private Reactor.Action<Exception> reject;

        #region Constructors

        /// <summary>
        /// Creates a new deferred.
        /// </summary>
        public Deferred() {
            this.future = new Future<T>((resolve, reject) => {
                this.resolve = resolve;
                this.reject = reject;
            });
        }

        #endregion

        #region Properties

        /// <summary>
        /// The deferred future.
        /// </summary>
        public Reactor.Future<T> Future {
            get {  return this.future; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Resolves this deferred.
        /// </summary>
        /// <param name="value"></param>
        public void Resolve(T value) {
            this.resolve(value);
        }

        /// <summary>
        /// Rejects this deferred.
        /// </summary>
        /// <param name="error"></param>
        public void Reject(Exception error) {
            this.reject(error);
        }

        #endregion
    }

    /// <summary>
    /// Reactor Deferred
    /// </summary>
    public class Deferred {
        private Reactor.Future            future;
        private Reactor.Action            resolve;
        private Reactor.Action<Exception> reject;

        #region Constructors

        /// <summary>
        /// Creates a new deferred.
        /// </summary>
        public Deferred() {
            this.future = new Future((resolve, reject) => {
                this.resolve = resolve;
                this.reject = reject;
            });
        }

        #endregion

        #region Properties

        /// <summary>
        /// The deferred future.
        /// </summary>
        public Reactor.Future Future {
            get {  return this.future; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Resolves this deferred.
        /// </summary>
        public void Resolve() {
            this.resolve();
        }

        /// <summary>
        /// Rejects this deferred.
        /// </summary>
        /// <param name="error"></param>
        public void Reject(Exception error) {
            this.reject(error);
        }

        #endregion
    }
}
