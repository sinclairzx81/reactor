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
using System.Globalization;
using System.Net;
using System.Text;

namespace Reactor.Http
{
    public sealed class Cookie
    {
        private string   comment;
        private Uri      commentUri;
        private bool     discard;
        private string   domain;
        private DateTime expires;
        private bool     httpOnly;
        private string   name;
        private string   path;
        private string   port;
        private int[]    ports;
        private bool     secure;
        private DateTime timestamp;
        private string   value;
        private int      version;

        static char[] reservedCharsName = new char[] { ' ', '=', ';', ',', '\n', '\r', '\t' };
        static char[] portSeparators    = new char[] { '"', ',' };
        static string tspecials         = "()<>@,;:\\\"/[]?={} \t";   // from RFC 2965, 2068

        public Cookie()
        {
            this.expires   = DateTime.MinValue;
            this.timestamp = DateTime.Now;
            this.domain    = String.Empty;
            this.name      = String.Empty;
            this.value     = String.Empty;
            this.comment   = String.Empty;
            this.port      = String.Empty;
        }

        public Cookie(string name, string value) : this() {

            this.name  = name;
            this.value = value;
        }

        public Cookie(string name, string value, string path) : this(name, value) {

            this.path = path;
        }

        public Cookie(string name, string value, string path, string domain) : this(name, value, path) {

            this.domain = domain;
        }

        public string Comment
        {
            get { return this.comment; }

            set { this.comment = value == null ? String.Empty : value; }
        }

        public Uri CommentUri {

            get { return this.commentUri; }

            set { this.commentUri = value; }
        }

        public bool Discard
        {
            get { return discard; }

            set { discard = value; }
        }

        public string Domain
        {
            get { return domain; }

            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    domain = String.Empty;

                    HasDomain = false;
                }
                else
                {
                    domain = value;

                    IPAddress test;

                    if (IPAddress.TryParse(value, out test)) {

                        HasDomain = false;
                    }
                    else {

                        HasDomain = true;
                    }
                }
            }
        }

        /*
         * Set this to false to disable same-origin checks.
         * 
         * This should be done whenever the cookie does not actually
         * contain a domain and we fallback to the Uri's hostname.
         * 
         */
        internal bool HasDomain
        {
            get;
            set;
        }

        public bool Expired
        {
            get
            {
                return expires <= DateTime.Now && expires != DateTime.MinValue;
            }
            set
            {
                if (value)
                {
                    expires = DateTime.Now;
                }
            }
        }

        public DateTime Expires
        {
            get { return expires; }

            set { expires = value; }
        }

        public bool HttpOnly
        {
            get { return httpOnly; }

            set { httpOnly = value; }
        }

        public string Name
        {
            get { return name; }
            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    throw new Exception("Name cannot be empty");
                }

                if (value[0] == '$' || value.IndexOfAny(reservedCharsName) != -1)
                {
                    // see CookieTest, according to MS implementation
                    // the name value changes even though it's incorrect
                    name = String.Empty;

                    throw new Exception("Name contains invalid characters");
                }

                name = value;
            }
        }

        public string Path
        {
            get { return (path == null) ? String.Empty : path; }

            set { path = (value == null) ? String.Empty : value; }
        }

        public string Port
        {
            get { return port; }

            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    port = String.Empty;

                    return;
                }
                if (value[0] != '"' || value[value.Length - 1] != '"')
                {
                    throw new Exception("The 'Port'='" + value + "' part of the cookie is invalid. Port must be enclosed by double quotes.");
                }

                port = value;

                string[] values = port.Split(portSeparators);

                ports = new int[values.Length];

                for (int i = 0; i < ports.Length; i++)
                {
                    ports[i] = Int32.MinValue;

                    if (values[i].Length == 0)
                    {
                        continue;
                    }

                    try
                    {
                        ports[i] = Int32.Parse(values[i]);
                    }
                    catch (Exception e)
                    {
                        throw new Exception("The 'Port'='" + value + "' part of the cookie is invalid. Invalid value: " + values[i], e);
                    }
                }
                Version = 1;
            }
        }

        internal int[] Ports
        {
            get { return ports; }

            set { ports = value; }
        }

        public bool Secure
        {
            get { return secure; }

            set { secure = value; }
        }

        public DateTime TimeStamp
        {
            get { return timestamp; }

        }

        public string Value
        {
            get { return value; }

            set
            {
                if (value == null)
                {
                    value = String.Empty;

                    return;
                }

                // LAMESPEC: According to .Net specs the Value property should not accept 
                // the semicolon and comma characters, yet it does. For now we'll follow
                // the behaviour of MS.Net instead of the specs.
                /*
                if (value.IndexOfAny(reservedCharsValue) != -1)
                    throw new CookieException("Invalid value. Value cannot contain semicolon or comma characters.");
                */

                this.value = value;
            }
        }

        public int Version
        {
            get { return version; }
            set
            {
                if ((value < 0) || (value > 10))
                    version = 0;
                else
                    version = value;
            }
        }

        public override bool Equals(Object comparand)
        {
            System.Net.Cookie c = comparand as System.Net.Cookie;

            return c != null &&

                   String.Compare(this.name, c.Name, true, CultureInfo.InvariantCulture) == 0 &&

                   String.Compare(this.value, c.Value, false, CultureInfo.InvariantCulture) == 0 &&

                   String.Compare(this.Path, c.Path, false, CultureInfo.InvariantCulture) == 0 &&

                   String.Compare(this.domain, c.Domain, true, CultureInfo.InvariantCulture) == 0 &&

                   this.version == c.Version;
        }

        public override int GetHashCode()
        {
            return hash(StringComparer.InvariantCulture.GetHashCode(name),

                        value.GetHashCode(),

                        Path.GetHashCode(),

                        StringComparer.InvariantCulture.GetHashCode(domain),

                        version);
        }

        private static int hash(int i, int j, int k, int l, int m)
        {
            return i ^ (j << 13 | j >> 19) ^ (k << 26 | k >> 6) ^ (l << 7 | l >> 25) ^ (m << 20 | m >> 12);
        }

        // returns a string that can be used to send a cookie to an Origin Server
        // i.e., only used for clients
        // see para 4.2.2 of RFC 2109 and para 3.3.4 of RFC 2965
        // see also bug #316017
        public override string ToString()
        {
            return ToString(null);
        }

        internal string ToString(Uri uri)
        {
            if (name.Length == 0)
                return String.Empty;

            StringBuilder result = new StringBuilder(64);

            if (version > 0)
            {
                result.Append("$Version=").Append(version).Append("; ");
            }

            result.Append(name).Append("=").Append(value);

            if (version == 0)
            {
                return result.ToString();
            }
            if (!String.IsNullOrEmpty(path))
            {
                result.Append("; $Path=").Append(path);
            }

            bool append_domain = (uri == null) || (uri.Host != domain);

            if (append_domain && !String.IsNullOrEmpty(domain))
            {
                result.Append("; $Domain=").Append(domain);
            }

            if (port != null && port.Length != 0)
            {
                result.Append("; $Port=").Append(port);
            }

            return result.ToString();
        }

        internal string ToClientString()
        {
            if (name.Length == 0)
            {
                return String.Empty;
            }

            StringBuilder result = new StringBuilder(64);

            if (version > 0)
            {
                result.Append("Version=").Append(version).Append(";");
            }

            result.Append(name).Append("=").Append(value);

            if (path != null && path.Length != 0)
            {
                result.Append(";Path=").Append(QuotedString(path));
            }

            if (domain != null && domain.Length != 0)
            {
                result.Append(";Domain=").Append(QuotedString(domain));
            }

            if (port != null && port.Length != 0)
            {
                result.Append(";Port=").Append(port);
            }

            return result.ToString();
        }

        // See par 3.6 of RFC 2616
        string QuotedString(string value)
        {
            if (version == 0 || IsToken(value))
            {
                return value;
            }
            else
            {
                return "\"" + value.Replace("\"", "\\\"") + "\"";
            }
        }

        bool IsToken(string value)
        {
            int len = value.Length;

            for (int i = 0; i < len; i++)
            {
                char c = value[i];

                if (c < 0x20 || c >= 0x7f || tspecials.IndexOf(c) != -1)
                {
                    return false;
                }
            }
            return true;
        }
    }

    public class Cookies : ICollection, IEnumerable
    {
        // not 100% identical to MS implementation
        sealed class CookieCollectionComparer : IComparer<Cookie>
        {
            public int Compare(Cookie x, Cookie y)
            {
                if (x == null || y == null)
                {
                    return 0;
                }

                var ydomain = y.Domain.Length - (y.Domain[0] == '.' ? 1 : 0);
                
                var xdomain = x.Domain.Length - (x.Domain[0] == '.' ? 1 : 0);

                int result = ydomain - xdomain;
                
                return result == 0 ? y.Path.Length - x.Path.Length : result;
            }
        }

        static CookieCollectionComparer Comparer = new CookieCollectionComparer();

        private List<Cookie> list = new List<Cookie>();

        internal IList<Cookie> List
        {
            get { return list; }
        }

        // ICollection
        public int Count
        {
            get { return list.Count; }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public Object SyncRoot
        {
            get { return this; }
        }

        public void CopyTo(Array array, int index)
        {
            (list as IList).CopyTo(array, index);
        }

        public void CopyTo(Cookie[] array, int index)
        {
            list.CopyTo(array, index);
        }

        // IEnumerable
        public IEnumerator GetEnumerator()
        {
            return list.GetEnumerator();
        }

        // This

        // LAMESPEC: So how is one supposed to create a writable CookieCollection 
        // instance?? We simply ignore this property, as this collection is always
        // writable.
        public bool IsReadOnly
        {
            get { return true; }
        }

        public void Add(Cookie cookie)
        {
            if (cookie == null)
            {
                throw new ArgumentNullException("cookie");
            }

            int pos = SearchCookie(cookie);

            if (pos == -1)
            {
                list.Add(cookie);
            }
            else
            {
                list[pos] = cookie;
            }
        }

        internal void Sort()
        {
            if (list.Count > 0)
            {
                list.Sort(Comparer);
            }
        }

        int SearchCookie(Cookie cookie)
        {
            string name   = cookie.Name;

            string domain = cookie.Domain;
            
            string path   = cookie.Path;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                Cookie c = list[i];

                if (c.Version != cookie.Version)
                {
                    continue;
                }

                if (0 != String.Compare(domain, c.Domain, true, CultureInfo.InvariantCulture))
                {
                    continue;
                }

                if (0 != String.Compare(name, c.Name, true, CultureInfo.InvariantCulture))
                {
                    continue;
                }

                if (0 != String.Compare(path, c.Path, true, CultureInfo.InvariantCulture))
                {
                    continue;
                }

                return i;
            }

            return -1;
        }

        public void Add(Cookies cookies)
        {
            if (cookies == null)
            {
                throw new ArgumentNullException("cookies");
            }

            foreach (Cookie c in cookies)
            {
                Add(c);
            }
        }

        public Cookie this[int index]
        {
            get
            {
                if (index < 0 || index >= list.Count)
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                return list[index];
            }
        }

        public Cookie this[string name]
        {
            get
            {
                foreach (Cookie c in list)
                {
                    if (0 == String.Compare(c.Name, name, true, CultureInfo.InvariantCulture))
                    {
                        return c;
                    }
                }

                return null;
            }
        }
    }
}
