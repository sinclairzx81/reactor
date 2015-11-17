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
using System.Collections;
using System.Collections.Generic;

namespace Reactor {

    /// <summary>
    /// Provides a set of methods for querying data structures over IEnumerable types. It is 
    /// provided as a convenience when targeting legacy .net platforms.
    /// </summary>
    /// <typeparam name="T">The type given for enumeration.</typeparam>
    public class Enumerable<T> : IEnumerable<T> {

        private IEnumerable<T> enumerable;

        #region Constructors

        /// <summary>
        /// Creates a new sequence.
        /// </summary>
        /// <param name="enumerable"></param>
        public Enumerable(IEnumerable<T> enumerable) {
            this.enumerable = enumerable;
        }

        #endregion

        #region Aggregate

        /// <summary>
        /// Applies an accumulator function over a sequence.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public T Aggregate(Reactor.Func<T, T, T> predicate) {
            return Enumerable.Aggregate(this.enumerable, predicate);
        }

        /// <summary>
        /// Applies an accumulator function over a sequence. The specified seed value is used as the initial accumulator value.
        /// </summary>
        /// <param name="initial"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public T Aggregate(T initial, Reactor.Func<T, T, T> predicate) {
            return Enumerable.Aggregate(this.enumerable, initial, predicate);
        }

        #endregion

        #region All

        /// <summary>
        /// Determines whether all the elements of a sequence satisfy a condition.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public bool All(Reactor.Func<T, bool> predicate) {
            return Enumerable.All(this.enumerable, predicate);
        }

        #endregion

        #region Any

        /// <summary>
        /// Determines whether a sequence contains any elements.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public bool Any(Reactor.Func<T, bool> predicate) {
            return Enumerable.Any(this.enumerable, predicate);
        }

        #endregion

        #region Average

        /// <summary>
        /// Computes the average of a sequence of Decimal values that is obtained by invoking a projection function on each element of the input sequence.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public double Average(Reactor.Func<T, double> predicate) {
            return Enumerable.Average(this.enumerable, predicate);
        }

        #endregion

        #region Cast

        /// <summary>
        /// Converts the elements of an IQueryable to the specified type.
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <returns></returns>
        public Reactor.Enumerable<U> Cast<U>() {
            return new Enumerable<U>(Enumerable.Cast<T, U>(this.enumerable));
        }

        #endregion

        #region Concat

        /// <summary>
        /// Concatenates two sequences.
        /// </summary>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        public Enumerable<T> Concat(IEnumerable<T> enumerable) {
            return new Enumerable<T>(Enumerable.Concat<T>(this.enumerable, enumerable));
        }

        #endregion

        #region Contains

        /// <summary>
        /// Determines whether a sequence contains a specified element by using the default equality comparer.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public bool Contains(Reactor.Func<T, bool> predicate) {
            return Enumerable.Contains(this.enumerable, predicate);
        }

        #endregion

        #region Count

        /// <summary>
        /// Returns the number of elements in a sequence.
        /// </summary>
        /// <returns></returns>
        public int Count   () {
            return Enumerable.Count(this.enumerable);
        }

        /// <summary>
        /// Returns the number of elements in the specified sequence that satisfies a condition.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public int Count(Reactor.Func<T, bool> predicate) {
            return Enumerable.Count(this.enumerable, predicate);
        }

        #endregion

        #region Distinct

        /// <summary>
        /// Returns distinct elements from a sequence by using the default equality comparer to compare values.
        /// </summary>
        /// <returns></returns>
        public Enumerable<T> Distinct() {
            return new Enumerable<T>(Enumerable.Distinct(this.enumerable));
        }

        #endregion

        #region ElementAt

        /// <summary>
        /// Returns the element at a specified index in a sequence.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T ElementAt(int index) {
            return Enumerable.ElementAt(this.enumerable, index);
        }

        #endregion

        #region ElementAtOrDefault

        /// <summary>
        /// Returns the element at a specified index in a sequence or a default value if the index is out of range.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T ElementAtOrDefault(int index) {
            return Enumerable.ElementAtOrDefault(this.enumerable, index);
        }

        #endregion

        #region Except

        /// <summary>
        /// Produces the set difference of two sequences by using the default equality comparer to compare values.
        /// </summary>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        public Enumerable<T> Except(IEnumerable<T> enumerable) {
            return new Enumerable<T>(Enumerable.Except(this.enumerable, enumerable));
        }

        #endregion

        #region First

        /// <summary>
        /// Returns the first element of a sequence.
        /// </summary>
        /// <returns></returns>
        public T First() {
            return Enumerable.First(this.enumerable);
        }

        /// <summary>
        /// Returns the first element of a sequence that satisfies a specified condition.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public T First(Reactor.Func<T, bool> predicate) {
            return Enumerable.First(this.enumerable, predicate);
        }

        #endregion

        #region FirstOrDefault

        /// <summary>
        /// Returns the first element of a sequence, or a default value if the sequence contains no elements.
        /// </summary>
        /// <returns></returns>
        public T FirstOrDefault() {
            return Enumerable.FirstOrDefault<T>(this.enumerable);
        }


        /// <summary>
        /// Returns the first element of a sequence that satisfies a specified condition or a default value if no such element is found.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public T FirstOrDefault(Reactor.Func<T, bool> predicate) {
            return Enumerable.FirstOrDefault(this.enumerable, predicate);
        }

        #endregion

        #region Intersect

        /// <summary>
        /// Produces the set intersection of two sequences by using the default equality comparer to compare values.
        /// </summary>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        public Enumerable<T> Intersect(IEnumerable<T> enumerable) {
            return new Enumerable<T>(Enumerable.Intersect(this.enumerable, enumerable));
        }

        #endregion

        #region Last

        /// <summary>
        /// Returns the last element in a sequence.
        /// </summary>
        /// <returns></returns>
        public T Last() {
            return Enumerable.Last(this.enumerable);
        }

        /// <summary>
        /// Returns the last element of a sequence that satisfies a specified condition.
        /// </summary>
        /// <returns></returns>
        public T Last(Reactor.Func<T, bool> predicate) {
            return Enumerable.Last(this.enumerable, predicate);
        }

        #endregion

        #region LastOrDefault

        /// <summary>
        /// Returns the last element in a sequence, or a default value if the sequence contains no elements.
        /// </summary>
        /// <returns></returns>
        public T LastOrDefault() {
            return Enumerable.LastOrDefault(this.enumerable);
        }

        /// <summary>
        /// Returns the last element of a sequence that satisfies a condition or a default value if no such element is found.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public T LastOrDefault(Reactor.Func<T, bool> predicate) {
            return Enumerable.LastOrDefault(this.enumerable, predicate);
        }

        #endregion

        #region OrderBy

        /// <summary>
        /// Sorts the elements of a sequence in ascending order according to a key.
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public Reactor.Enumerable<T> OrderBy<U>(Reactor.Func<T, U> predicate) {
             return new Enumerable<T>(Enumerable.OrderBy(this.enumerable, predicate));
        }

        #endregion

        #region OrderByDescending

        /// <summary>
        /// Sorts the elements of a sequence in descending order according to a key.
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public Reactor.Enumerable<T> OrderByDescending<U>(Reactor.Func<T, U> predicate) {
             return new Enumerable<T>(Enumerable.OrderByDescending(this.enumerable, predicate));
        }

        #endregion
        
        #region Reverse

        /// <summary>
        /// Inverts the order of the elements in a sequence.
        /// </summary>
        /// <returns></returns>
        public Reactor.Enumerable<T> Reverse () {
            return new Enumerable<T>(Enumerable.Reverse(this.enumerable));
        }

        #endregion

        #region Select

        /// <summary>
        /// Projects each element of a sequence into a new form.
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public Reactor.Enumerable<U> Select<U>(Reactor.Func<T, U> predicate) {
            return new Reactor.Enumerable<U>(
                Reactor.Enumerable.Select<T, U>(this.enumerable, predicate));
        }

        #endregion

        #region SelectMany

        /// <summary>
        /// Projects each element of a sequence to an IEnumerable<T> and combines the resulting sequences into one sequence.
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public Reactor.Enumerable<U> SelectMany<U>(Reactor.Func<T, IEnumerable<U>> predicate) {
            return new Reactor.Enumerable<U>(Reactor.Enumerable.SelectMany(this.enumerable, predicate));
        }

        #endregion

        #region Single

        /// <summary>
        /// Returns the only element of a sequence that satisfies a specified condition, and throws an exception if more than one such element exists.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public T Single(Reactor.Func<T, bool> predicate) {
            return Enumerable.Single(this.enumerable, predicate);
        }

        #endregion

        #region SingleOrDefault

        /// <summary>
        /// Returns the only element of a sequence that satisfies a specified condition or a default value if no such element exists; this method throws an exception if more than one element satisfies the condition.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public T SingleOrDefault(Reactor.Func<T, bool> predicate) {
            return Enumerable.SingleOrDefault(this.enumerable, predicate);
        }

        #endregion

        #region Skip

        /// <summary>
        /// Bypasses a specified number of elements in a sequence and then returns the remaining elements.
        /// </summary>
        /// <param name="skip"></param>
        /// <returns></returns>
        public Enumerable<T> Skip(int skip) {
            return new Enumerable<T>(Enumerable.Skip(this.enumerable, skip));
        }

        #endregion

        #region Sum

        /// <summary>
        /// Computes the sum of the sequence of Decimal values that is obtained by invoking a projection function on each element of the input sequence.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public double Sum(Reactor.Func<T, double> predicate) {
            return Enumerable.Sum(this.enumerable, predicate);
        }

        #endregion

        #region Take

        /// <summary>
        /// Returns a specified number of contiguous elements from the start of a sequence.
        /// </summary>
        /// <param name="take"></param>
        /// <returns></returns>
        public Enumerable<T> Take(int take) {
            return new Enumerable<T>(Enumerable.Take(this.enumerable, take));
        }

        #endregion

        #region Union

        /// <summary>
        /// Produces the set union of two sequences by using the default equality comparer.
        /// </summary>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        public Enumerable<T> Union(IEnumerable<T> enumerable) {
            return new Enumerable<T>(Enumerable.Union(this.enumerable, enumerable));
        }

        #endregion

        #region Where

        /// <summary>
        /// Filters a sequence of values based on a predicate.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public Enumerable<T> Where(Reactor.Func<T, bool> predicate) {
            return new Enumerable<T>(
                Enumerable.Where<T>(this.enumerable, predicate));
        }

        #endregion

        #region ForEach

        /// <summary>
        /// Enumerates over the sequence.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public Reactor.Enumerable<T> ForEach(Reactor.Action<T> action) {
            foreach (var element in this.enumerable) {
                action(element);
            }
            return this;
        }

        #endregion

        #region Zip

        /// <summary>
        /// Merges two sequences by using the specified predicate function.
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public Enumerable<V> Zip<U, V>(IEnumerable<U> enumerable, Reactor.Func<T, U, V> predicate) {
            return new Enumerable<V>(Enumerable.Zip(this.enumerable, enumerable, predicate));
        }

        #endregion

        #region ToArray

        /// <summary>
        /// Returns this enumerable as a array.
        /// </summary>
        /// <returns></returns>
        public T[] ToArray () {
             return Enumerable.ToArray(this.enumerable);
        }

        #endregion

        #region ToList

        /// <summary>
        /// Returns this enumerable as a list.
        /// </summary>
        /// <returns></returns>
        public List<T> ToList  () {
            return Enumerable.ToList(this.enumerable);
        }

        #endregion

        #region ToQueue

        /// <summary>
        /// Return this enumerable as a queue.
        /// </summary>
        /// <returns></returns>
        public Queue<T> ToQueue() {
            return Enumerable.ToQueue(this.enumerable);
        }

        #endregion

        #region IEnumerable<T>

        /// <summary>
        /// Returns this enumerables enumerator.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator() {
            return this.enumerable.GetEnumerator();
        }

        /// <summary>
        /// Returns this enumerables enumerator.
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator() {
            return this.enumerable.GetEnumerator();
        }

        #endregion
    }

    /// <summary>
    /// Provides a set of methods for querying data structures over IEnumerable types.
    /// </summary>
    public static class Enumerable {

        #region Aggregate

        /// <summary>
        /// Applies an accumulator function over a sequence.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="initial"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        internal static T Aggregate<T>(IEnumerable<T> enumerable, T initial, Reactor.Func<T, T, T> predicate) {
            var enumerator  = enumerable.GetEnumerator();
            var accumulator = initial;
            while (enumerator.MoveNext()) {
                accumulator = predicate(accumulator, enumerator.Current);
            }
            return accumulator;
        }

        /// <summary>
        /// Applies an accumulator function over a sequence. The specified seed value is used as the initial accumulator value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        internal static T Aggregate<T>(IEnumerable<T> enumerable, Reactor.Func<T, T, T> predicate) {
            var enumerator = enumerable.GetEnumerator();
            if (enumerator.MoveNext()) {
                var accumulator = enumerator.Current;
                while (enumerator.MoveNext()) {
                    accumulator = predicate(accumulator, enumerator.Current);
                }
                return accumulator;
            }

            return default(T);
        }

        #endregion

        #region All

        /// <summary>
        /// Determines whether all the elements of a sequence satisfy a condition.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        internal static bool All<T>(IEnumerable<T> enumerable, Func<T, bool> predicate) {
            foreach (var element in enumerable) {
                if (!predicate(element)) {
                    return false;
                }
            }
            return true;
        }

        #endregion

        #region Any
		 
        /// <summary>
        /// Determines whether a sequence contains any elements.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        internal static bool Any<T>(IEnumerable<T> enumerable, Reactor.Func<T, bool> predicate) {
            foreach (var element in enumerable) {
                if (predicate(element)) {
                    return true;
                }
            } 
            return false;
        }

        #endregion

        #region Average

        /// <summary>
        /// Computes the average of a sequence of Decimal values that is obtained by invoking a projection function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        internal static double Average<T>(IEnumerable<T> enumerable, Func<T, double> predicate) {
            double result = 0;
            int count = 0;
            foreach (var element in enumerable) {
                result += predicate(element);
                count++;
            }
            return (count > 0) ? result / count : 0;
        }

        #endregion

        #region Cast

        /// <summary>
        /// Converts the elements of an IQueryable to the specified type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        internal static IEnumerable<U> Cast<T, U>(IEnumerable<T> enumerable) {
            foreach (var element in enumerable) {
                var boxed = (object)element;
                yield return (U)boxed;
            }
        }

        #endregion

        #region Concat

        /// <summary>
        /// Concatenates two sequences.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable_0"></param>
        /// <param name="enumerable_1"></param>
        /// <returns></returns>
        internal static IEnumerable<T> Concat<T>(IEnumerable<T> enumerable_0, IEnumerable<T> enumerable_1) {
            foreach (var element in enumerable_0) {
                yield return element;
            }
            foreach (var element in enumerable_1) {
                yield return element;
            }
        }

        #endregion

        #region Contains

        /// <summary>
        /// Returns the number of elements in the specified sequence that satisfies a condition.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        internal static bool Contains<T>(IEnumerable<T> enumerable, Reactor.Func<T, bool> predicate) {
            foreach (var element in enumerable) {
                if (predicate(element)) {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Count

        /// <summary>
        /// Returns the number of elements in a sequence.
        /// </summary>
        /// <typeparam name="T">The input type.</typeparam>
        /// <param name="enumerable">The enumerable.</param>
        /// <returns>The count.</returns>
        internal static int Count<T>(IEnumerable<T> enumerable) {
            var count = 0;
            foreach(var element in enumerable) {
                count++;
            }
            return count;
        }

        /// <summary>
        /// Returns the number of elements in the specified sequence that satisfies a condition.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        internal static int Count<T>(IEnumerable<T> enumerable, Reactor.Func<T, bool> predicate) {
            var count = 0;
            foreach (var element in enumerable) {
                if (predicate(element)) {
                    count++;
                }
            }
            return count;
        }

        #endregion

        #region Distinct

        /// <summary>
        /// Returns distinct elements from a sequence by using a specified IEqualityComparer<T> to compare values.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        internal static IEnumerable<T> Distinct<T>(IEnumerable<T> enumerable) {
            var cache = new List<T>();
            foreach (var element in enumerable) {
                if (!cache.Contains(element)) {
                    cache.Add(element);
                    yield return element;
                }
            }
        }

        #endregion

        #region ElementAt

        /// <summary>
        /// Returns the element at a specified index in a sequence.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        internal static T ElementAt<T>(IEnumerable<T> enumerable, int index) {
            var count = 0;
            foreach (var element in enumerable) {
                if (count == index) {
                    return element;
                }
                count++;
            }
            throw new Exception("no element exists at " + index.ToString());
        }

        #endregion

        #region ElementAtOrDefault

        /// <summary>
        /// Returns the element at a specified index in a sequence or a default value if the index is out of range.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        internal static T ElementAtOrDefault<T>(IEnumerable<T> enumerable, int index) {
            var count = 0;
            foreach (var element in enumerable) {
                if (count == index) {
                    return element;
                }
                count++;
            }
            return default(T);
        }

        #endregion

        #region Except

        /// <summary>
        /// Produces the set difference of two sequences by using the default equality comparer to compare values.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable_0"></param>
        /// <param name="enumerable_1"></param>
        /// <returns></returns>
        internal static IEnumerable<T> Except<T>(IEnumerable<T> enumerable_0, IEnumerable<T> enumerable_1) {
            var cache = new List<T>();
            foreach (var element in enumerable_1) {
                cache.Add(element);
            }
            foreach (var element in enumerable_0) {
                if (!cache.Contains(element)) {
                    yield return element;
                }
            }
        }

        #endregion

        #region First

        /// <summary>
        /// Returns the first element of a sequence.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        internal static T First<T>(IEnumerable<T> enumerable) {
            foreach(var element in enumerable) {
                return element;
            }
            throw new Exception("sequence is empty");
        }

        /// <summary>
        /// Returns the first element of a sequence that satisfies a specified condition.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        internal static T First<T>(IEnumerable<T> enumerable, Reactor.Func<T, bool> predicate) {
            foreach(var element in enumerable) {
                if (predicate(element)) { 
                    return element;
                }
            }
            throw new Exception("element not found");
        }

        #endregion

        #region FirstOrDefault

        /// <summary>
        /// Returns the first element of a sequence, or a default value if the sequence contains no elements.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        internal static T FirstOrDefault<T>(IEnumerable<T> enumerable) {
            foreach(var element in enumerable) {
                return element;
            }
            return default(T);            
        }

        /// <summary>
        /// Returns the first element of a sequence that satisfies a specified condition or a default value if no such element is found.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        internal static T FirstOrDefault<T>(IEnumerable<T> enumerable, Reactor.Func<T, bool> predicate) {
            foreach(var element in enumerable) {
                if (predicate(element)) { 
                    return element;
                }
            }
            return default(T);
        }        

        #endregion

        #region Intersect

        /// <summary>
        /// Produces the set intersection of two sequences by using the specified IEqualityComparer<T> to compare values.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable_0"></param>
        /// <param name="enumerable_1"></param>
        /// <returns></returns>
        internal static IEnumerable<T> Intersect<T>(IEnumerable<T> enumerable_0, IEnumerable<T> enumerable_1) {
            var cache_0 = new List<T>();
            foreach (var element in enumerable_1) {
                cache_0.Add(element);
            }
            var cache_1 = new List<T>();
            foreach (var element in enumerable_0) {
                if (cache_0.Contains(element)) {
                    if (!cache_1.Contains(element)) {
                        cache_1.Add(element);
                        yield return element;
                    }
                }
            }
        }

        #endregion

        #region Last

        /// <summary>
        /// Returns the last element in a sequence.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        internal static T Last<T>(IEnumerable<T> enumerable) {
            var enumerator = enumerable.GetEnumerator();
            T current = default(T);
            bool hasvalue = false;
            while (enumerator.MoveNext()) {
                hasvalue = true;
                current = enumerator.Current;
            }
            if(hasvalue) return current;
            throw new Exception("sequence is empty.");
        }

        /// <summary>
        /// Returns the last element of a sequence that satisfies a specified condition.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        internal static T Last<T>(IEnumerable<T> enumerable, Reactor.Func<T, bool> predicate) {
            var enumerator = enumerable.GetEnumerator();
            T current = default(T);
            bool hasvalue = false;
            while (enumerator.MoveNext()) {
                if (predicate(enumerator.Current)) {
                    hasvalue = true;
                    current = enumerator.Current;
                }
            }
            if(hasvalue) return current;
            throw new Exception("element not found.");
        }

        #endregion

        #region LastOrDefault

        /// <summary>
        /// Returns the last element in a sequence, or a default value if the sequence contains no elements.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        internal static T LastOrDefault<T>(IEnumerable<T> enumerable) {
            var enumerator = enumerable.GetEnumerator();
            T current = default(T);
            while (enumerator.MoveNext()) {
                current = enumerator.Current;
            }
            return current;
        }

        /// <summary>
        /// Returns the last element of a sequence that satisfies a condition or a default value if no such element is found.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        internal static T LastOrDefault<T>(IEnumerable<T> enumerable, Reactor.Func<T, bool> predicate) {
            var enumerator = enumerable.GetEnumerator();
            T current = default(T);
            while (enumerator.MoveNext()) {
                if (predicate(enumerator.Current)) {
                    current = enumerator.Current;
                }
            }
            return current;
        }

        #endregion

        #region OrderBy

        /// <summary>
        /// Sorts the elements of a sequence in ascending order according to a key.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        internal static IEnumerable<T> OrderBy<T, U>(IEnumerable<T> enumerable, Reactor.Func<T, U> predicate) {
            return Enumerable.Sort(enumerable, (a, b) => {
                var map_0 = predicate(a);
                var map_1 = predicate(b);
                if (map_0 is IComparable && map_1 is IComparable) {
                    var comparer_a = map_0 as IComparable;
                    var comparer_b = map_1 as IComparable;
                    return comparer_a.CompareTo(comparer_b);
                }
                return 0;
            });
        }

        #endregion

        #region OrderByDescending

        /// <summary>
        /// Sorts the elements of a sequence in descending order according to a key.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        internal static IEnumerable<T> OrderByDescending<T, U>(IEnumerable<T> enumerable, Reactor.Func<T, U> predicate) {
            return Enumerable.Sort(enumerable, (a, b) => {
                var map_0 = predicate(a);
                var map_1 = predicate(b);
                if (map_0 is IComparable && map_1 is IComparable) {
                    var comparer_a = map_0 as IComparable;
                    var comparer_b = map_1 as IComparable;
                    return comparer_b.CompareTo(comparer_a);
                }
                return 0;
            });
        }

        #endregion

        #region Reverse

        /// <summary>
        /// Inverts the order of the elements in a sequence.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        internal static IEnumerable<T> Reverse<T>(IEnumerable<T> enumerable) {
            var clone = Enumerable.ToArray(enumerable);
            for (int i = clone.Length - 1; i >= 0; i--) {
                yield return clone[i];
            }
        }

        #endregion

        #region Select

        /// <summary>
        /// Projects each element of a sequence into a new form.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        internal static IEnumerable<U> Select<T, U>(IEnumerable<T> enumerable, Reactor.Func<T, U> predicate) {
            foreach (var element in enumerable) {
                yield return predicate(element);
            } 
        }

        #endregion

        #region SelectMany

        /// <summary>
        /// Projects each element of a sequence to an IEnumerable<T> and invokes a result selector function on each element therein. The resulting values from each intermediate sequence are combined into a single, one-dimensional sequence and returned.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        internal static IEnumerable<U> SelectMany<T, U>(IEnumerable<T> enumerable, Func<T, IEnumerable<U>> predicate) {
            foreach (var element in enumerable) {
                foreach (var item in predicate(element)) {
                    yield return item;
                }
            } 
        }

        #endregion

        #region Single

        /// <summary>
        /// Returns the only element of a sequence that satisfies a specified condition, and throws an exception if more than one such element exists.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        internal static T Single<T>(IEnumerable<T> enumerable, Reactor.Func<T, bool> predicate) {
            T current = default(T);
            bool hasvalue = false;
            foreach (var element in enumerable) {
                if (predicate(element)) {
                    if (!hasvalue) {
                        hasvalue = true;
                        current = element;
                    }
                    else {
                        throw new Exception("more than one element found.");
                    }

                }
            }
            if(hasvalue) return current;
            throw new Exception("no element found.");
        }

        #endregion

        #region SingleOrDefault

        /// <summary>
        /// Returns the only element of a sequence, or a default value if the sequence is empty; this method throws an exception if there is more than one element in the sequence.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        internal static T SingleOrDefault<T>(IEnumerable<T> enumerable, Reactor.Func<T, bool> predicate) {
            T current = default(T);
            bool hasvalue = false;
            foreach (var element in enumerable) {
                if (predicate(element)) {
                    if (!hasvalue) {
                        hasvalue = true;
                        current = element;
                    }
                    else {
                        throw new Exception("more than one element found.");
                    }

                }
            }
            return current;
        }

        #endregion

        #region Skip

        /// <summary>
        /// Bypasses a specified number of elements in a sequence and then returns the remaining elements.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="skip"></param>
        /// <returns></returns>
        internal static IEnumerable<T> Skip<T>(IEnumerable<T> enumerable, int skip) {
            int index = 0;
            foreach (var element in enumerable) {
                if (index >= skip) {
                    yield return element;
                }
                index++;
            }
        }

        #endregion

        #region Sum

        /// <summary>
        /// Computes the sum of the sequence of Decimal values that is obtained by invoking a projection function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        internal static double Sum<T> (IEnumerable<T> enumerable, Func<T, double> predicate) {
            double result = 0;
            foreach (var element in enumerable) {
                result += predicate(element);
            }
            return result;
        }

        #endregion

        #region Take

        /// <summary>
        /// Returns a specified number of contiguous elements from the start of a sequence.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        internal static IEnumerable<T> Take<T>(IEnumerable<T> enumerable, int take) {
            int count = 0;
            foreach (var element in enumerable) {
                if (count != take) {
                    yield return element;
                    count++;
                }
                else {
                    break;
                }
            }
        }

        #endregion

        #region Union

        /// <summary>
        /// Produces the set union of two sequences by using the default equality comparer.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable_0"></param>
        /// <param name="enumerable_1"></param>
        /// <returns></returns>
        internal static IEnumerable<T> Union<T>(IEnumerable<T> enumerable_0, IEnumerable<T> enumerable_1) {
            var cache_0 = new List<T>();
            foreach (var element in enumerable_0) {
                if (!cache_0.Contains(element)) {
                    cache_0.Add(element);
                    yield return element;
                }
            }
            foreach (var element in enumerable_1) {
                if (!cache_0.Contains(element)) {
                    cache_0.Add(element);
                    yield return element;
                }
            }
        }

        #endregion

        #region Where

        /// <summary>
        /// Filters a sequence of values based on a predicate.
        /// </summary>
        /// <typeparam name="T">The input type.</typeparam>
        /// <param name="enumerable">The enumerable.</param>
        /// <param name="predicate">The filter predicate.</param>
        /// <returns>The filtered enumerable.</returns>
        internal static IEnumerable<T> Where<T>(IEnumerable<T> enumerable, Reactor.Func<T, bool> predicate) {
            foreach (var element in enumerable) {
                if (predicate(element)) {
                    yield return element;
                }
            }            
        }

        #endregion

        #region Zip

        public static IEnumerable<V> Zip<T, U, V>(IEnumerable<T> enumerable_0, IEnumerable<U> enumerable_1, Reactor.Func<T, U, V> predicate) {
            var enumerator_0 = enumerable_0.GetEnumerator();
            var enumerator_1 = enumerable_1.GetEnumerator();
            while(enumerator_0.MoveNext() &&
                  enumerator_1.MoveNext())
                   yield return predicate(enumerator_0.Current, 
                                          enumerator_1.Current);
        }

        #endregion

        #region Sort

        internal class Comparer<T> : IComparer<T>  {
            private Reactor.Func<T, T, int> comparer;
            public Comparer(Reactor.Func<T, T, int> comparer) {
                this.comparer = comparer;
            }
            public int Compare(T x, T y) {
                return comparer(x, y);
            }
        }

        /// <summary>
        /// Sorts this enumerable.
        /// </summary>
        /// <typeparam name="T">The input type.</typeparam>
        /// <param name="enumerable">The enumerable.</param>
        /// <param name="predicate">The sort predicate.</param>
        /// <returns>The sorted enumerable.</returns>
        /// <example>
        /// var sorted = Reactor.Enumerable.Sort(array, (a, b) =&gt; {
        ///       if(a &gt; b) return 1;
        ///       if(a &lt; b) return -1;
        ///       return 0;
        /// });
        /// </example>
        internal static IEnumerable<T> Sort<T>(IEnumerable<T> enumerable, Reactor.Func<T, T, int> predicate) {
            var array = Enumerable.ToArray(enumerable);
            Array.Sort(array, new Comparer<T>(predicate));
            return array;
        }

        #endregion

        #region ToArray

        /// <summary>
        /// Returns this enumerable as a array.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        internal static T [] ToArray<T>(IEnumerable<T> enumerable) {
            var result = new T[Enumerable.Count(enumerable)];
            var index  = 0;
            foreach(var element in enumerable) {
                result[index] = element;
                index++;
            }
            return result;
        }

        #endregion

        #region ToList

        /// <summary>
        /// Returns this enumerable as a list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        internal static List<T> ToList<T>(IEnumerable<T> enumerable) {
            var result = new List<T>();
            foreach(var element in enumerable) {
                result.Add(element);
            }
            return result;
        }

        #endregion

        #region ToQueue

        /// <summary>
        /// Returns this enumerable as a queue.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        internal static Queue<T> ToQueue<T>(IEnumerable<T> enumerable) {
            var result = new Queue<T>();
            foreach(var element in enumerable) {
                result.Enqueue(element);
            }
            return result;
        }

        #endregion

        #region Sources

        /// <summary>
        /// Returns a enumerable of the specified range.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        internal static IEnumerable<int> RangeSource(int start, int count) {
            int index = 0;
            while (index < count) {
                yield return index;
                index++;
            }
        }

        /// <summary>
        /// Returns a enumerable repeated with the specified item and count.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        internal static IEnumerable<T> RepeatSource<T>(T item, int count) {
            for (int i = 0; i < count; i++) {
                yield return item;
            }
        }

        #endregion

        #region Statics

        /// <summary>
        /// Returns a new Reactor.Enumerable.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        public static Reactor.Enumerable<T> Create<T>(IEnumerable<T> enumerable) {
            return new Reactor.Enumerable<T>(enumerable);
        }

        /// <summary>
        /// Returns a new Reactor.Enumerable of the given range.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static Reactor.Enumerable<int> Range(int start, int count) {
            return new Reactor.Enumerable<int>(RangeSource(start, count));
        }

        /// <summary>
        /// Returns a new Reactor.Enumerable repeated with the specified item and count.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static Reactor.Enumerable<T> Repeat<T>(T item, int count) {
            return new Reactor.Enumerable<T>(RepeatSource(item, count));
        }

        #endregion
    }
}