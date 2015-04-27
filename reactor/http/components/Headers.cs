using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.Serialization;
using System.Text;

namespace Reactor.Http {

    /// <summary>
    /// Reactor HTTP Header.
    /// </summary>
    public class Headers : IEnumerable<KeyValuePair<string, string>> {

        #region Structures

        static readonly bool[] allowed_chars = {
			false, false, false, false, false, false, false, false, false, false, false, false, false, false,
			false, false, false, false, false, false, false, false, false, false, false, false, false, false,
			false, false, false, false, false, true, false, true, true, true, true, false, false, false, true,
			true, false, true, true, false, true, true, true, true, true, true, true, true, true, true, false,
			false, false, false, false, false, false, true, true, true, true, true, true, true, true, true,
			true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true,
			false, false, false, true, true, true, true, true, true, true, true, true, true, true, true, true,
			true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true,
			false, true, false
		};

        internal enum HeaderInfo {
            Request    = 1,
            Response   = 1 << 1,
            MultiValue = 1 << 10
        }

        static readonly Dictionary<string, HeaderInfo> headers = new Dictionary<string, HeaderInfo>(StringComparer.OrdinalIgnoreCase) {
				{ "Allow",               HeaderInfo.MultiValue },
				{ "Accept",              HeaderInfo.Request | HeaderInfo.MultiValue },
				{ "Accept-Charset",      HeaderInfo.MultiValue },
				{ "Accept-Encoding",     HeaderInfo.MultiValue },
				{ "Accept-Language",     HeaderInfo.MultiValue },
				{ "Accept-Ranges",       HeaderInfo.MultiValue },
				{ "Authorization",       HeaderInfo.MultiValue },
				{ "Cache-Control",       HeaderInfo.MultiValue },
				{ "Cookie",              HeaderInfo.MultiValue },
				{ "Connection",          HeaderInfo.Request | HeaderInfo.MultiValue },
				{ "Content-Encoding",    HeaderInfo.MultiValue },
				{ "Content-Length",      HeaderInfo.Request | HeaderInfo.Response },
				{ "Content-Type",        HeaderInfo.Request },
				{ "Content-Language",    HeaderInfo.MultiValue },
				{ "Date",                HeaderInfo.Request },
				{ "Expect",              HeaderInfo.Request | HeaderInfo.MultiValue},
				{ "Host",                HeaderInfo.Request },
				{ "If-Match",            HeaderInfo.MultiValue },
				{ "If-Modified-Since",   HeaderInfo.Request },
				{ "If-None-Match",       HeaderInfo.MultiValue },
				{ "Keep-Alive",          HeaderInfo.Response },
				{ "Pragma",              HeaderInfo.MultiValue },
				{ "Proxy-Authenticate",  HeaderInfo.MultiValue },
				{ "Proxy-Authorization", HeaderInfo.MultiValue },
				{ "Proxy-Connection",    HeaderInfo.Request | HeaderInfo.MultiValue },
				{ "Range",               HeaderInfo.Request | HeaderInfo.MultiValue },
				{ "Referer",             HeaderInfo.Request },
				{ "Set-Cookie",          HeaderInfo.MultiValue },
				{ "Set-Cookie2",         HeaderInfo.MultiValue },
				{ "TE",                  HeaderInfo.MultiValue },
				{ "Trailer",             HeaderInfo.MultiValue },
				{ "Transfer-Encoding",   HeaderInfo.Request | HeaderInfo.Response | HeaderInfo.MultiValue },
				{ "Upgrade",             HeaderInfo.MultiValue },
				{ "User-Agent",          HeaderInfo.Request },
				{ "Vary",                HeaderInfo.MultiValue },
				{ "Via",                 HeaderInfo.MultiValue },
				{ "Warning",             HeaderInfo.MultiValue },
				{ "WWW-Authenticate",    HeaderInfo.Response | HeaderInfo. MultiValue }
			};

        #endregion

        #region Fields

        private NameValueCollection collection;

        private string []           restrictions;

        #endregion

        #region Constructors

        internal Headers(string [] restrictions) {

            this.restrictions = restrictions;

            this.collection   = new NameValueCollection();
        }
        
        public Headers() : this(new string[]{}) {

        }

        #endregion

        #region Properties

        public string this[string name]  {

            get { return this.Get(name); }
            
            set { this.Set(name, value); }
        }

        #endregion

        #region External

        public string[] AllKeys 
        {
            get
            {
                return this.collection.AllKeys;
            }
        }

        public int      Count   
        {
            get
            {
                return this.collection.Count;
            }
        }

        public string   Get       (string name) {

            if(!this.ValidateNameRestriction(name)) {

                throw new ArgumentNullException("restricted name.");
            }

            return this.Get_Internal(name);
        }

        public string[] GetValues (string name)
        {
            if(!this.ValidateNameRestriction(name)) {

                throw new ArgumentNullException("restricted name.");
            }

            return this.GetValues_Internal(name);
        }

        public void     Set       (string name, string value) {

            if(!this.ValidateNameRestriction(name)) {

                throw new ArgumentNullException("restricted name.");
            }

            this.Set_Internal(name, value);
        }

        public void     Add       (string name, string value) {

            if(!this.ValidateNameRestriction(name)) {

                throw new ArgumentNullException("restricted name.");
            }

            this.Add_Internal(name, value);
        }

        public void     Remove    (string name) {

            if(!this.ValidateNameRestriction(name)) {

                throw new ArgumentNullException("restricted name.");
            }

            this.Remove_Internal(name);
        }

        #endregion

        #region Internal

        internal string   Get_Internal       (string name) {

            if (!Headers.ValidateName(name)) {

                throw new ArgumentNullException("invalid header name.");
            }

            return collection.Get(name);
        }

        internal string[] GetValues_Internal (string name) {

           if (!Headers.ValidateName(name)) {

                throw new ArgumentNullException("invalid header name.");
            }

            string[] values = this.collection.GetValues(name);

            if (values == null || values.Length == 0) {

                return null;
            }

            if (Headers.IsMultiValue(name)) {

                List<string> separated = null;

                foreach (var value in values) {

                    if (value.IndexOf(',') < 0) {

                        continue;
                    }

                    if (separated == null) {

                        separated = new List<string>(values.Length + 1);

                        foreach (var v in values)
                        {
                            if (v == value)
                            {
                                break;
                            }

                            separated.Add(v);
                        }
                    }

                    var slices = value.Split(',');

                    var slices_length = slices.Length;
                    
                    if (value[value.Length - 1] == ',') {

                        --slices_length;
                    }

                    for (int i = 0; i < slices_length; ++i) {

                        separated.Add(slices[i].Trim());
                    }
                }

                if (separated != null) {

                    return separated.ToArray();
                }
            }

            return values;
        }

        internal void     Set_Internal       (string name, string value)
        {
            if (!Headers.ValidateName(name)) {

                throw new ArgumentNullException("invalid header name.");
            }

            if(!Headers.ValidateValue(value)) {

                throw new ArgumentNullException("invalid header value.");
            }

            this.collection.Set(name, value);
        }

        internal void     Add_Internal       (string name, string value) {

            if (!Headers.ValidateName(name)) {

                throw new ArgumentNullException("invalid header name.");
            }

            if(!Headers.ValidateValue(value)) {

                throw new ArgumentNullException("invalid header value.");
            }

            this.collection.Add(name, value);
        }

        internal void     Remove_Internal    (string name) {

            if (!Headers.ValidateName(name)) {

                throw new ArgumentNullException("invalid header name.");
            }

            this.collection.Remove(name);
        }

        #endregion

        #region Private

        private bool ValidateNameRestriction  (string name)  {

            foreach (var restriction in this.restrictions) {

                if (name == restriction) {

                    return false;
                }
            }

            return true;
        }        

        #endregion

        #region Statics

        internal static bool ValidateName   (string name)  {

            if (name == null || name.Length == 0) {

                return false;
            }

            int len = name.Length;

            for (int i = 0; i < len; i++) {

                char c = name[i];

                if (c > 126 || !allowed_chars[c]) {

                    return false;
                }
            }

            return true;
        }

        internal static bool ValidateValue  (string value) {

            // TEXT any 8 bit value except CTL's (0-31 and 127)
            //      but including \r\n space and \t
            //      after a newline at least one space or \t must follow
            //      certain header fields allow comments ()

            int len = value.Length;

            for (int i = 0; i < len; i++) {

                char c = value[i];

                if (c == 127) {

                    return false;
                }

                if (c < 0x20 && (c != '\r' && c != '\n' && c != '\t')) {

                    return false;
                }

                if (c == '\n' && ++i < len) {

                    c = value[i];
                    
                    if (c != ' ' && c != '\t') {

                        return false;
                    }
                }
            }

            return true;
        }

        internal static bool IsMultiValue   (string name)  {

            if (name == null) {

                return false;
            }

            HeaderInfo info;

            return headers.TryGetValue(name, out info) && 
                    
                (info & HeaderInfo.MultiValue) != 0;
        }

        #endregion

        #region IEnumerable

        IEnumerator<KeyValuePair<string, string>> IEnumerable<KeyValuePair<string, string>>.GetEnumerator() {

            foreach (var key in this.AllKeys) {

                yield return new KeyValuePair<string, string>(key, string.Join(", ", this.GetValues(key)));
            } 
        }

        IEnumerator IEnumerable.GetEnumerator() {

            foreach (var key in this.AllKeys) {

                yield return new KeyValuePair<string, string>(key, string.Join(", ", this.GetValues(key)));
            } 
        }

        #endregion

        #region ToString

        public override string ToString() {

            var builder = new StringBuilder();
            
            for (int i = 0; i < this.collection.Count; i++) {

                string key = this.collection.GetKey(i);

                if (Headers.IsMultiValue(key)) {

                    foreach (string v in this.collection.GetValues(i)) {

                        builder.Append(key);

                        builder.Append(": ");

                        builder.Append(v);

                        builder.Append("\r\n");
                    }
                }
                else {

                    builder.Append(key);

                    builder.Append(": ");

                    builder.Append(this.collection.Get(i));

                    builder.Append("\r\n");
                }
            }
            return builder.Append("\r\n").ToString();
        }

        #endregion
    }
}
