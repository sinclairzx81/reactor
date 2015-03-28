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

namespace Reactor
{
    public class Future<T>
    {
        public enum State
        {
            Pending,

            Rejected,

            Resolved
        };

        private Exception                       error;

        private T                               value;

        private State                           state;

        private List<Reactor.Action<Exception>> _errors;

        private List<Reactor.Action<T>>         _callbacks;

        private Future()
        {
            this.state          = State.Pending;

            this.error          = null;

            this.value          = default(T);

            this._errors = new List<Action<Exception>>();

            this._callbacks = new List<Action<T>>();
        }

        public  Future(T value)
        {
            this.state = State.Resolved;

            this.value = value;
        }

        public  Future(Action<Action<T>, Action<Exception>> resolver)
        {
            this.state          = State.Pending;

            this.error          = null;

            this.value          = default(T);

            this._errors        = new List<Action<Exception>>();

            this._callbacks     = new List<Action<T>>();

            try
            {
                resolver(this.Resolve, this.Reject);
            }
            catch(Exception error)
            {
                this.Reject(error);
            }
        }

        public  Future<T> Then  (Action<T> callback)
        {
            var future = new Future<T>();

            Action<Exception> reject  = future.Reject;

            Action<T>         resolve = value => {

                callback(value);

                future.Resolve(value);
            };

            switch (this.state)
            {
                case State.Resolved:

                    resolve(this.value);

                    break;

                case State.Rejected:

                    reject(this.error);

                    break;

                case State.Pending:

                    this._callbacks.Add(callback);

                    break;
            }

            return future;
        }

        public  Future<T> Error (Action<Exception> callback)
        {
            var future = new Future<T>();

            Action<T> resolve = future.Resolve;

            Action<Exception> reject = error =>
            {
                callback(error);

                future.Reject(error);
            };

            switch (this.state)
            {
                case State.Resolved:

                    resolve(this.value);

                    break;

                case State.Rejected:

                    reject(this.error);

                    break;

                case State.Pending:

                    this._errors.Add(callback);

                    break;
            }

            return future;
        }

        #region Resolve / Reject

        private void Reject(Exception error)
        {
            if (this.state != State.Pending) {

                throw new Exception("invalid state: " + this.state);
            }

            this.error = error;

            this.state = State.Rejected;

            foreach (var handler in this._errors) {

                handler(error);
            }

            this._callbacks.Clear();

            this._errors.Clear();
        }

        private void Resolve(T value)
        {
            if (this.state != State.Pending) {

                throw new Exception("invalid state: " + this.state);
            }

            this.value = value;

            this.state = State.Resolved;

            foreach (var handler in this._callbacks) {

                handler(value);
            }

            this._callbacks.Clear();

            this._errors.Clear();
        }

        #endregion
    }

    public class Future
    {
        enum State
        {
            Pending,

            Rejected,

            Resolved
        };

        private Exception   error;

        private State       state;

        private List<Reactor.Action<Exception>> _errors;

        private List<Reactor.Action>            _callbacks;

        private Future()
        {
            this.state          = State.Pending;

            this.error          = null;

            this._errors = new List<Action<Exception>>();

            this._callbacks = new List<Action>();
        }

        public  Future(Action<Action, Action<Exception>> resolver)
        {
            this.state          = State.Pending;

            this.error          = null;

            this._errors = new List<Action<Exception>>();

            this._callbacks = new List<Action>();

            try
            {
                resolver(this.Resolve, this.Reject);
            }
            catch(Exception error)
            {
                this.Reject(error);
            }
        }

        public  Future Then  (Action callback)
        {
            var future = new Future();

            Action<Exception> reject = future.Reject;

            Action resolve = () =>
            {
                callback();

                future.Resolve();
            };

            switch (this.state)
            {
                case State.Resolved:

                    resolve();

                    break;

                case State.Rejected:

                    reject(this.error);

                    break;

                case State.Pending:

                    this._callbacks.Add(callback);

                    break;
            }

            return future;
        }

        public  Future Error (Action<Exception> callback)
        {
            var future = new Future();

            Action resolve = future.Resolve;

            Action<Exception> reject = error =>
            {
                callback(error);

                future.Reject(error);
            };

            switch (this.state)
            {
                case State.Resolved:

                    resolve();

                    break;

                case State.Rejected:

                    reject(this.error);

                    break;

                case State.Pending:

                    this._errors.Add(callback);

                    break;
            }

            return future;
        }

        #region Resolve / Reject

        private void Reject(Exception error)
        {
            if (this.state != State.Pending) {

                throw new Exception("invalid state: " + this.state);
            }

            this.error = error;

            this.state = State.Rejected;

            foreach (var handler in this._errors) {

                handler(error);
            }

            this._callbacks.Clear();

            this._errors.Clear();
        }

        private void Resolve()
        {
            if (this.state != State.Pending) {

                throw new Exception("invalid state: " + this.state);
            }

            this.state = State.Resolved;

            foreach (var handler in this._callbacks) {

                handler();
            }

            this._callbacks.Clear();

            this._errors.Clear();
        }

        #endregion
    }
}