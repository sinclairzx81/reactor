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
    /// Reactor Future.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Future<T> {

        #region State
        
        enum State {
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

        private Exception                       _error;
        private T                               _value;
        private State                           _state;
        private List<Reactor.Action<Exception>> _errors;
        private List<Reactor.Action<T>>         _thens;

        #region Constructors

        /// <summary>
        /// Creates a new future.
        /// </summary>
        internal Future() {
            this._state     = State.Pending;
            this._error     = null;
            this._value     = default(T);
            this._errors    = new List<Reactor.Action<Exception>>();
            this._thens     = new List<Reactor.Action<T>>();
        }

        /// <summary>
        /// Creates a new future with resolved value.
        /// </summary>
        /// <param name="value"></param>
        public Future(T value) {
            this._state     = State.Resolved;
            this._value     = value;
            this._errors    = new List<Reactor.Action<Exception>>();
            this._thens     = new List<Reactor.Action<T>>();
        }

        /// <summary>
        /// Creates a new future.
        /// </summary>
        /// <param name="resolver">The resolve / reject function.</param>
        public Future(Action<Action<T>, Action<Exception>> resolver) {
            this._state     = State.Pending;
            this._error     = null;
            this._value     = default(T);
            this._errors    = new List<Reactor.Action<Exception>>();
            this._thens     = new List<Reactor.Action<T>>();
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
        public Reactor.Async.Future Then  (Reactor.Action<T> callback) {
            var future  = new Reactor.Async.Future();
            var reject  = new Reactor.Action<Exception>(error => {
                future.Reject(error);
            });
            var resolve = new Reactor.Action<T>(value => {
                callback(value);
                future.Resolve();
            });
            switch (this._state) {
                case State.Resolved:
                    resolve(this._value);
                    break;
                case State.Rejected:
                    reject(this._error);
                    break;
                case State.Pending:
                    this._thens.Add(resolve);
                    this._errors.Add(reject);
                    break;
            }
            return future;
        }

        /// <summary>
        /// Assigns a action which is called on a resolved state.
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public Reactor.Async.Future<TResult> Then<TResult>  (Reactor.Func<T, TResult> callback) {
            var future  = new Reactor.Async.Future<TResult>();
            var reject  = new Reactor.Action<Exception>(error => {
                future.Reject(error);
            });
            var resolve = new Reactor.Action<T>(value => {
                var v = callback(value);
                future.Resolve(v);
            });
            switch (this._state) {
                case State.Resolved:
                    resolve(this._value);
                    break;
                case State.Rejected:
                    reject(this._error);
                    break;
                case State.Pending:
                    this._thens.Add(resolve);
                    this._errors.Add(reject);
                    break;
            }
            return future;
        }

        /// <summary>
        /// Assigns a action which is called on a rejected state.
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public Reactor.Async.Future Error (Reactor.Action<Exception> callback) {
            var future  = new Reactor.Async.Future();
            var resolve = new Reactor.Action<T>(value => {
                future.Resolve();
            });
            var reject  = new Reactor.Action<Exception>(error => {
                callback(error);
                future.Resolve();
            });
            switch (this._state) {
                case State.Resolved:
                    resolve(this._value);
                    break;
                case State.Rejected:
                    reject(this._error);
                    break;
                case State.Pending:
                    this._thens.Add(resolve);
                    this._errors.Add(reject);
                    break;
            }
            return future;
        }

        /// <summary>
        /// Assigns a action which is called irrespective of a rejected / resolved state.
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public Reactor.Async.Future Finally (Reactor.Action callback) {
            var future  = new Reactor.Async.Future();
            var resolve = new Reactor.Action<T>(value => {
                callback();
                future.Resolve();
            });
            var reject  = new Reactor.Action<Exception>(error => {
                callback();
                future.Reject(error);
            });
            switch (this._state) {
                case State.Resolved:
                    resolve(this._value);
                    break;
                case State.Rejected:
                    reject(this._error);
                    break;
                case State.Pending:
                    this._thens.Add(resolve);
                    this._errors.Add(reject);
                    break;
            }            
            return future;
        }

        /// <summary>
        /// Cancels this future. If this future has not already
        /// resolved, a cancelled future will reject with a exception
        /// containing the supplied message.
        /// </summary>
        /// <param name="reason"></param>
        public void Cancel(string reason) {
            this.Reject(new Exception(reason));
        }

        /// <summary>
        /// Cancels this future. If this future has not already
        /// resolved, a cancelled future will reject with a exception
        /// containing the message 'cancelled'.
        /// </summary>
        /// <param name="reason"></param>
        public void Cancel() {
            this.Cancel("cancelled");
        }

        #endregion

        #region Internals

        /// <summary>
        /// Rejects this future.
        /// </summary>
        /// <param name="error"></param>
        internal void Reject(Exception error) {
            if (this._state != State.Pending) {
                return;
            }
            this._error = error;
            this._state = State.Rejected;
            foreach (var handler in this._errors) {
                handler(error);
            }
            this._thens.Clear();
            this._errors.Clear();
        }

        /// <summary>
        /// Resolves this future.
        /// </summary>
        /// <param name="value"></param>
        internal void Resolve(T value) {
            if (this._state != State.Pending) {
                return;
            }
            this._value = value;
            this._state = State.Resolved;
            foreach (var handler in this._thens) {
                handler(value);
            }
            this._thens.Clear();
            this._errors.Clear();
        }

        #endregion
    }

    /// <summary>
    /// Reactor Future.
    /// </summary>
    public class Future {

        #region State

        enum State {
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

        private Exception                       _error;
        private State                           _state;
        private List<Reactor.Action<Exception>> _errors;
        private List<Reactor.Action>            _thens;

        #region Constructors

        /// <summary>
        /// Creates a new future.
        /// </summary>
        internal Future() {
            this._state    = State.Pending;
            this._error    = null;
            this._errors   = new List<Action<Exception>>();
            this._thens    = new List<Action>();
        }

        /// <summary>
        /// Creates a new future. 
        /// </summary>
        /// <param name="resolver">The resolve / reject function.</param>
        public  Future(Reactor.Action<Reactor.Action, Reactor.Action<Exception>> resolver) {
            this._state    = State.Pending;
            this._error    = null;
            this._errors   = new List<Action<Exception>>();
            this._thens    = new List<Action>();
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
        public Reactor.Async.Future Then (Reactor.Action callback) {
            var future  = new Reactor.Async.Future();
            var reject  = new Reactor.Action<Exception>(error => {
                future.Reject(error);
            });
            var resolve = new Reactor.Action(() => {
                callback();
                future.Resolve();
            });
            switch (this._state) {
                case State.Resolved:
                    resolve();
                    break;
                case State.Rejected:
                    reject(this._error);
                    break;
                case State.Pending:
                    this._thens.Add(resolve);
                    this._errors.Add(reject);
                    break;
            }
            return future;
        }

        /// <summary>
        /// Assigns a action which is called on a resolved state.
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public Reactor.Async.Future<TResult> Then<TResult> (Reactor.Func<TResult> callback) {
            var future  = new Reactor.Async.Future<TResult>();
            var reject  = new Reactor.Action<Exception>(error => {
                future.Reject(error);
            });
            var resolve = new Reactor.Action(() => {
                var v = callback();
                future.Resolve(v);
            });
            switch (this._state) {
                case State.Resolved:
                    resolve();
                    break;
                case State.Rejected:
                    reject(this._error);
                    break;
                case State.Pending:
                    this._thens.Add(resolve);
                    this._errors.Add(reject);
                    break;
            }
            return future;
        }

        /// <summary>
        /// Assigns a action which is called on a rejected state.
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public Reactor.Async.Future Error (Reactor.Action<Exception> callback) {
            var future  = new Reactor.Async.Future();
            var resolve = new Reactor.Action(() => {
                future.Resolve();
            });
            var reject  = new Reactor.Action<Exception>(error => {
                callback(error);
                future.Resolve();
            });
            switch (this._state) {
                case State.Resolved:
                    resolve();
                    break;
                case State.Rejected:
                    reject(this._error);
                    break;
                case State.Pending:
                    this._thens.Add(resolve);
                    this._errors.Add(reject);
                    break;
            }            
            return future;
        }

        /// <summary>
        /// Assigns a action which is called irrespective of a rejected / resolved state.
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public Reactor.Async.Future Finally (Reactor.Action callback) {
            var future  = new Reactor.Async.Future();
            var resolve = new Reactor.Action(() => {
                callback();
                future.Resolve();
            });
            var reject  = new Reactor.Action<Exception>(error => {
                callback();
                future.Reject(error);
            });
            switch (this._state) {
                case State.Resolved:
                    resolve();
                    break;
                case State.Rejected:
                    reject(this._error);
                    break;
                case State.Pending:
                    this._thens.Add(resolve);
                    this._errors.Add(reject);
                    break;
            }            
            return future;
        }

        /// <summary>
        /// Cancels this future. If this future has not already
        /// resolved, a cancelled future will reject with a exception
        /// containing the supplied message.
        /// </summary>
        /// <param name="reason"></param>
        public void Cancel(string reason) {
            this.Reject(new Exception(reason));
        }

        /// <summary>
        /// Cancels this future. If this future has not already
        /// resolved, a cancelled future will reject with a exception
        /// containing the message 'cancelled'.
        /// </summary>
        /// <param name="reason"></param>
        public void Cancel() {
            this.Cancel("cancelled");
        }

        #endregion

        #region Internals

        internal void Reject(Exception error) {
            if (this._state != State.Pending) {
                return;
            }
            this._error = error;
            this._state = State.Rejected;
            foreach (var handler in this._errors) {
                handler(error);
            }
            this._thens.Clear();
            this._errors.Clear();
        }

        internal void Resolve() {
            if (this._state != State.Pending) {
                return;
            }
            this._state = State.Resolved;
            foreach (var handler in this._thens) {
                handler();
            }
            this._thens.Clear();
            this._errors.Clear();
        }

        #endregion

        #region Statics

        /// <summary>
        /// Returns a new Future.
        /// </summary>
        /// <param name="resolver"></param>
        /// <returns></returns>
        public static Reactor.Async.Future Create(Reactor.Action<Action, Action<Exception>> resolver) {
            return new Reactor.Async.Future(resolver);
        }

        /// <summary>
        /// Returns a new Future.
        /// </summary>
        /// <param name="resolver"></param>
        /// <returns></returns>
        public static Reactor.Async.Future<T> Create<T>(Reactor.Action<Action<T>, Action<Exception>> resolver) {
            return new Reactor.Async.Future<T>(resolver);
        }

        /// <summary>
        /// Returns a resolved future.
        /// </summary>
        /// <returns></returns>
        public static Reactor.Async.Future Resolved() {
            return new Reactor.Async.Future((resolve, reject) => resolve());
        }

        /// <summary>
        /// Returns a resolved future.
        /// </summary>
        /// <param name="value">The resolved value.</param>
        /// <returns></returns>
        public static Reactor.Async.Future<T> Resolve<T>(T value) {
            return new Reactor.Async.Future<T>((resolve, reject) => resolve(value));
        }

        /// <summary>
        /// Returns a rejected future.
        /// </summary>
        /// <returns></returns>
        public static Reactor.Async.Future Rejected(Exception error) {
            return new Reactor.Async.Future((resolve, reject) => reject(error));
        }

        /// <summary>
        /// Returns a rejected future.
        /// </summary>
        /// <param name="value">The rejected value.</param>
        /// <returns></returns>
        public static Reactor.Async.Future<T> Rejected<T>(Exception error) {
            return new Reactor.Async.Future<T>((resolve, reject) => reject(error));
        }

        #endregion
    }

    /// <summary>
    /// Reactor Deferred
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Deferred<T> {
        private Reactor.Async.Future<T>   future;
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
        public Reactor.Async.Future<T> Future {
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
        private Reactor.Async.Future      future;
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
        public Reactor.Async.Future Future {
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
