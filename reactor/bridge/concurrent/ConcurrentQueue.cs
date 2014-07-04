/*--------------------------------------------------------------------------

Reactor

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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Reactor
{
    internal class ConcurrentQueue<T> : IEnumerable<T>, ICollection, IEnumerable
    {
        #region Node

        internal class Node
        {
            public T   Value;

            public Node Next;
        }

        #endregion

        Node head = new Node();

        Node tail;

        int count;

        public ConcurrentQueue()
        {
            tail = head;
        }

        public ConcurrentQueue(IEnumerable<T> collection) : this()
        {
            foreach (T item in collection)
            {
                Enqueue(item);
            }
        }

        public void Enqueue(T item)
        {
            Node node    = new Node();

            node.Value   = item;

            Node oldTail = null;

            Node oldNext = null;

            bool update = false;

            while (!update)
            {
                oldTail = tail;

                oldNext = oldTail.Next;

                if (tail == oldTail)
                {
                    if (oldNext == null)
                    {
                        update = Interlocked.CompareExchange(ref tail.Next, node, null) == null;
                    }
                    else
                    {
                        Interlocked.CompareExchange(ref tail, oldNext, oldTail);
                    }
                }
            }
            
            Interlocked.CompareExchange(ref tail, node, oldTail);

            Interlocked.Increment(ref count);
        }

        public bool TryDequeue(out T result)
        {
            result = default(T);

            bool advanced = false;

            while (!advanced)
            {
                Node oldHead = head;

                Node oldTail = tail;

                Node oldNext = oldHead.Next;

                if (oldHead == head)
                {
                    if (oldHead == oldTail)
                    {
                        if (oldNext != null)
                        {
                            Interlocked.CompareExchange(ref tail, oldNext, oldTail);

                            continue;
                        }

                        result = default(T);

                        return false;
                    }
                    else
                    {
                        result = oldNext.Value;

                        advanced = Interlocked.CompareExchange(ref head, oldNext, oldHead) == oldHead;
                    }
                }
            }

            Interlocked.Decrement(ref count);

            return true;
        }

        public bool TryPeek(out T result)
        {
            Node first = head.Next;

            if (first == null)
            {
                result = default(T);

                return false;
            }

            result = first.Value;

            return true;
        }

        internal void Clear()
        {
            count = 0;

            tail = head = new Node();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)InternalGetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return InternalGetEnumerator();
        }

        IEnumerator<T> InternalGetEnumerator()
        {
            Node my_head = head;

            while ((my_head = my_head.Next) != null)
            {
                yield return my_head.Value;
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            if (array.Rank > 1)
            {
                throw new ArgumentException("The array can't be multidimensional");
            }
            if (array.GetLowerBound(0) != 0)
            {
                throw new ArgumentException("The array needs to be 0-based");
            }

            T [] dest = array as T[];
            
            if (dest == null)
            {
                throw new ArgumentException("The array cannot be cast to the collection element type", "array");
            }

            CopyTo(dest, index);
        }

        public void CopyTo(T[] array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            
            if (index >= array.Length)
            {
                throw new ArgumentException("index is equals or greather than array length", "index");
            }

            IEnumerator<T> e = InternalGetEnumerator();
            
            int i = index;
            
            while (e.MoveNext())
            {
                if (i == array.Length - index)
                {
                    throw new ArgumentException("The number of elements in the collection exceeds the capacity of array", "array");
                }

                array[i++] = e.Current;
            }
        }

        public T[] ToArray()
        {
            return new List<T>(this).ToArray();
        }

        bool ICollection.IsSynchronized
        {
            get 
            { 
                return true; 
            }
        }

        object syncRoot = new object();
        
        object ICollection.SyncRoot
        {
            get 
            { 
                return syncRoot; 
            }
        }

        public int Count
        {
            get
            {
                return count;
            }
        }

        public bool IsEmpty
        {
            get
            {
                return count == 0;
            }
        }
    }
}
