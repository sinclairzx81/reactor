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
using System.Text;

namespace Reactor {
 
    public static class Tuple {
        public static Tuple<T1> Create<T1>(T1 item1) {
            return new Tuple<T1>(item1);
        }
        public static Tuple<T1, T2> Create<T1, T2>(T1 item1, T2 item2) {
            return new Tuple<T1, T2>(item1, item2);
        }
        public static Tuple<T1, T2, T3> Create<T1, T2, T3>(T1 item1, T2 item2, T3 item3) {
            return new Tuple<T1, T2, T3>(item1, item2, item3);
        }
        public static Tuple<T1, T2, T3, T4> Create<T1, T2, T3, T4>(T1 item1, T2 item2, T3 item3, T4 item4) {
            return new Tuple<T1, T2, T3, T4>(item1, item2, item3, item4);
        }
        public static Tuple<T1, T2, T3, T4, T5> Create<T1, T2, T3, T4, T5>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5) {
            return new Tuple<T1, T2, T3, T4, T5>(item1, item2, item3, item4, item5);
        }
        public static Tuple<T1, T2, T3, T4, T5, T6> Create<T1, T2, T3, T4, T5, T6>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6) {
            return new Tuple<T1, T2, T3, T4, T5, T6>(item1, item2, item3, item4, item5, item6);
        }
        public static Tuple<T1, T2, T3, T4, T5, T6, T7> Create<T1, T2, T3, T4, T5, T6, T7>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7) {
            return new Tuple<T1, T2, T3, T4, T5, T6, T7>(item1, item2, item3, item4, item5, item6, item7);
        }
        public static Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8>> Create<T1, T2, T3, T4, T5, T6, T7, T8>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8) {
            return new Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8>>(item1, item2, item3, item4, item5, item6, item7, new Tuple<T8>(item8));
        }
    }

    internal interface ITuple {
        int Size { get; }
    }

    public class Tuple<T1> : ITuple {
        public T1 Item1 { get; private set; }
        public Tuple(T1 item1) {
            this.Item1 = item1;
        }
        #region ITuple
        public int Size {
            get { return 1; }
        }
        #endregion
        #region ToString
        public override string ToString() {
            var builder = new StringBuilder();
            builder.Append("(");
            builder.Append(this.Item1);
            builder.Append(")");
            return builder.ToString();
        }
        #endregion
    }

    public class Tuple<T1, T2> : ITuple {
        public T1 Item1 { get; private set; }
        public T2 Item2 { get; private set; }
        public Tuple(T1 item1, T2 item2) {
            this.Item1 = item1;
            this.Item2 = item2;
        }
        #region ITuple
        public int Size {
            get { return 2; }
        }
        #endregion
        #region ToString
        public override string ToString() {
            var builder = new StringBuilder();
            builder.Append("(");
            builder.Append(this.Item1);
            builder.Append(", ");
            builder.Append(this.Item2);
            builder.Append(")");
            return builder.ToString();
        }
        #endregion
    }

    public class Tuple<T1, T2, T3> : ITuple {
        public T1 Item1 { get; private set; }
        public T2 Item2 { get; private set; }
        public T3 Item3 { get; private set; }
        public Tuple(T1 item1, T2 item2, T3 item3) {
            this.Item1 = item1;
            this.Item2 = item2;
            this.Item3 = item3;
        }
        #region ITuple
        public int Size {
            get { return 3; }
        }
        #endregion
        #region ToString
        public override string ToString() {
            var builder = new StringBuilder();
            builder.Append("(");
            builder.Append(this.Item1);
            builder.Append(", ");
            builder.Append(this.Item2);
            builder.Append(", ");
            builder.Append(this.Item3);
            builder.Append(")");
            return builder.ToString();
        }
        #endregion
    }

    public class Tuple<T1, T2, T3, T4> : ITuple {
        public T1 Item1 { get; private set; }
        public T2 Item2 { get; private set; }
        public T3 Item3 { get; private set; }
        public T4 Item4 { get; private set; }
        public Tuple(T1 item1, T2 item2, T3 item3, T4 item4) {
            this.Item1 = item1;
            this.Item2 = item2;
            this.Item3 = item3;
            this.Item4 = item4;
        }
        #region ITuple
        public int Size {
            get { return 4; }
        }
        #endregion
        #region ToString
        public override string ToString() {
            var builder = new StringBuilder();
            builder.Append("(");
            builder.Append(this.Item1);
            builder.Append(", ");
            builder.Append(this.Item2);
            builder.Append(", ");
            builder.Append(this.Item3);
            builder.Append(", ");
            builder.Append(this.Item4);
            builder.Append(")");
            return builder.ToString();
        }
        #endregion
    }

    public class Tuple<T1, T2, T3, T4, T5> : ITuple {
        public T1 Item1 { get; private set; }
        public T2 Item2 { get; private set; }
        public T3 Item3 { get; private set; }
        public T4 Item4 { get; private set; }
        public T5 Item5 { get; private set; }
        public Tuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5) {
            this.Item1 = item1;
            this.Item2 = item2;
            this.Item3 = item3;
            this.Item4 = item4;
            this.Item5 = item5;
        }
        #region ITuple
        public int Size {
            get { return 5; }
        }
        #endregion
        #region ToString
        public override string ToString() {
            var builder = new StringBuilder();
            builder.Append("(");
            builder.Append(this.Item1);
            builder.Append(", ");
            builder.Append(this.Item2);
            builder.Append(", ");
            builder.Append(this.Item3);
            builder.Append(", ");
            builder.Append(this.Item4);
            builder.Append(", ");
            builder.Append(this.Item5);
            builder.Append(")");
            return builder.ToString();
        }
        #endregion
    }

    public class Tuple<T1, T2, T3, T4, T5, T6> : ITuple {
        public T1 Item1 { get; private set; }
        public T2 Item2 { get; private set; }
        public T3 Item3 { get; private set; }
        public T4 Item4 { get; private set; }
        public T5 Item5 { get; private set; }
        public T6 Item6 { get; private set; }
        public Tuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6) {
            this.Item1 = item1;
            this.Item2 = item2;
            this.Item3 = item3;
            this.Item4 = item4;
            this.Item5 = item5;
            this.Item6 = item6;
        }
        #region ITuple
        public int Size {
            get { return 6; }
        }
        #endregion
        #region ToString
        public override string ToString() {
            var builder = new StringBuilder();
            builder.Append("(");
            builder.Append(this.Item1);
            builder.Append(", ");
            builder.Append(this.Item2);
            builder.Append(", ");
            builder.Append(this.Item3);
            builder.Append(", ");
            builder.Append(this.Item4);
            builder.Append(", ");
            builder.Append(this.Item5);
            builder.Append(", ");
            builder.Append(this.Item6);
            builder.Append(")");
            return builder.ToString();
        }
        #endregion
    }

    public class Tuple<T1, T2, T3, T4, T5, T6, T7> : ITuple {
        public T1 Item1 { get; private set; }
        public T2 Item2 { get; private set; }
        public T3 Item3 { get; private set; }
        public T4 Item4 { get; private set; }
        public T5 Item5 { get; private set; }
        public T6 Item6 { get; private set; }
        public T7 Item7 { get; private set; }
        public Tuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7) {
            this.Item1 = item1;
            this.Item2 = item2;
            this.Item3 = item3;
            this.Item4 = item4;
            this.Item5 = item5;
            this.Item6 = item6;
            this.Item7 = item7;
        }
        #region ITuple
        public int Size {
            get { return 7; }
        }
        #endregion
        #region ToString
        public override string ToString() {
            var builder = new StringBuilder();
            builder.Append("(");
            builder.Append(this.Item1);
            builder.Append(", ");
            builder.Append(this.Item2);
            builder.Append(", ");
            builder.Append(this.Item3);
            builder.Append(", ");
            builder.Append(this.Item4);
            builder.Append(", ");
            builder.Append(this.Item5);
            builder.Append(", ");
            builder.Append(this.Item6);
            builder.Append(", ");
            builder.Append(this.Item7);
            builder.Append(")");
            return builder.ToString();
        }
        #endregion
    }

    public class Tuple<T1, T2, T3, T4, T5, T6, T7, TRest> : ITuple {
        public T1 Item1   { get; private set; }
        public T2 Item2   { get; private set; }
        public T3 Item3   { get; private set; }
        public T4 Item4   { get; private set; }
        public T5 Item5   { get; private set; }
        public T6 Item6   { get; private set; }
        public T7 Item7   { get; private set; }
        public TRest Rest { get; private set; }
        public Tuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, TRest rest) {
            
            this.Item1 = item1;
            this.Item2 = item2;
            this.Item3 = item3;
            this.Item4 = item4;
            this.Item5 = item5;
            this.Item6 = item6;
            this.Item7 = item7;
            this.Rest  = rest;
        }
        #region ITuple
        public int Size {
            get { return 8; }
        }
        #endregion
        #region ToString
        public override string ToString() {
            var builder = new StringBuilder();
            builder.Append("(");
            builder.Append(this.Item1);
            builder.Append(", ");
            builder.Append(this.Item2);
            builder.Append(", ");
            builder.Append(this.Item3);
            builder.Append(", ");
            builder.Append(this.Item4);
            builder.Append(", ");
            builder.Append(this.Item5);
            builder.Append(", ");
            builder.Append(this.Item6);
            builder.Append(", ");
            builder.Append(this.Item7);
            builder.Append(", ");
            builder.Append(this.Rest);
            builder.Append(")");
            return builder.ToString();
        }
        #endregion
    }
}