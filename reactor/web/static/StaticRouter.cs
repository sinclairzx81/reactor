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

using System.IO;

namespace Reactor.Web {

    /// <summary>
    /// Reactor static content router.
    /// </summary>
    public class StaticRouter {
        
        #region Assets
        private const string foldericon = "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAAXNSR0IArs4c6QAA" +
                                          "AARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAYdEVYdFNvZnR3" +
                                          "YXJlAHBhaW50Lm5ldCA0LjAuNvyMY98AAACLSURBVDhPYxh4YGxs/B8ZGxkZfQdi" +
                                          "Zag0YYBuABRvhkoTBlg0E4Wh2iEGAJ3829DQMBzIDiUWQ7XDXXAGaEgllE0KvgVz" +
                                          "wSQgfQhJglg8AeaCDJA3kCSIwkA9bjAXFKNLEoG/qKiosIMM+Ac04ASaJEEM1LMR" +
                                          "ORDJwemUGiBHtgFA518Cax4EgIEBAAey80UGlWOxAAAAAElFTkSuQmCC";
        private const string fileicon  = "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAAXNSR0IArs4c6QAA" +
                                          "AARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAYdEVYdFNvZnR3" +
                                          "YXJlAHBhaW50Lm5ldCA0LjAuNvyMY98AAAC+SURBVDhPtdO7DgFBGIbhKYiCOHRa" +
                                          "jU6ikNCoRUPhcAOOtSvT6yhouAcVSgVK3j9mNuz6mcaXPMlusnl3tlhj18YZF+tq" +
                                          "3Sx3/6qDYANsUPa0wxTBhlg8L722wn8Caex/yCASGEECMdQVNVQhz6iBLO6KCty+" +
                                          "nqChkLfnIIsExpCAfN/BOlqnFy3I1EAc/ZACwlMDKSxDmghPDSQwU+ThFglMIIEk" +
                                          "5ooi3NSA7z4G1ih52uIt0IX7dX31jDHmAXhbYD2/xU68AAAAAElFTkSuQmCC";

        #endregion

        #region Fields

        private string basePath;
        private string directory;
        private bool   browsable;

        #endregion

        #region Constructor
        
        /// <summary>
        /// Creates a new static file router.
        /// </summary>
        /// <param name="basePath">path in which to serve static files from.</param>
        /// <param name="directory">The local directory in which to serve from.</param>
        public StaticRouter(string basePath, string directory, bool browsable) : base() {
            this.basePath  = basePath;
            this.directory = directory;
            this.browsable = browsable;
        }

        #endregion

        #region Public

        /// <summary>
        /// Processes this static router.
        /// </summary>
        /// <param name="context">The reactor http context.</param>
        /// <param name="next">The next callback.</param>
        public void Process(Reactor.Http.Context context, Reactor.Action<Reactor.Http.Context> next) {
            
            // format basepath
            if (basePath[basePath.Length - 1] != '/') {
                basePath = basePath + "/";
            }

            // check the basepath.
            if (context.Request.Url.AbsolutePath.IndexOf(basePath) != 0) {
                next(context);
                return;
            }

            // format fullpath...
            var fullpath = Reactor.Net.HttpUtility.UrlDecode(Path.Combine(directory,
                    context.Request.Url.AbsolutePath.Substring(basePath.Length,
                    context.Request.Url.AbsolutePath.Length - basePath.Length))
                    .Replace("\\", "/")
                    .Replace("//", "/"));

            // if the path is a directory and browsable...
            if (System.IO.Directory.Exists(fullpath) && this.browsable) {
                this.ProcessDirectory(context, fullpath);
            }
            // if the path is a asset...
            else if (System.IO.File.Exists(fullpath)) {
                this.ProcessFile(context, fullpath);
            }
            // defer to next handler.
            else next(context);
        }

        #endregion

        #region Private

        /// <summary>
        /// Processes a file.
        /// </summary>
        /// <param name="context">The http context.</param>
        /// <param name="fullpath">The path to the file.</param>
        private void ProcessFile(Reactor.Http.Context context, string fullpath) {
            // compute content disposition.
            var disposition = System.IO.Path.GetFileName(fullpath)
                .Replace(",",  string.Empty).Replace(";", string.Empty).Replace("\"", string.Empty)
                .Replace("'",  string.Empty).Replace("+", string.Empty).Replace("\n", string.Empty)
                .Replace("\t", string.Empty).Replace("@", string.Empty).Replace("$",  string.Empty)
                .Replace("^",  string.Empty).Replace("%", string.Empty).Replace("(",  string.Empty);

            var range = context.Request.Headers["Range"];
            if (range == null) {
                // process file as standard http stream.
                var readstream = Reactor.File.Reader.Create(fullpath, FileMode.Open, FileShare.ReadWrite);
                context.Response.ContentLength = readstream.Length;
                context.Response.ContentType = Reactor.Web.Mime.Lookup(fullpath);
                context.Response.AppendHeader("Content-Disposition", "inline; filename=" + disposition);
                context.Response.AddHeader("Cache-Control", "public");
                context.Response.StatusCode = 200;
                readstream.Pipe(context.Response);
            }
            else {
                // process file as chunked range.
                var info  = new System.IO.FileInfo(fullpath);
                var split = range.Replace("bytes=", "").Split(new char[] { '-' });
                var start = long.Parse(split[0]);
                var end   = info.Length - 1;
                if (split[1] != "") {
                    end = long.Parse(split[1]);
                }

                var length = (end + start) + 1;

                context.Response.ContentType   = Reactor.Web.Mime.Lookup(fullpath);
                context.Response.ContentLength = length;
                context.Response.AppendHeader("Content-Disposition", "inline; filename=" + disposition);
                context.Response.AddHeader("Content-Range", string.Format("bytes {0}-{1}/{2}", start, end, info.Length));
                context.Response.AddHeader("Cache-Control", "public");
                context.Response.StatusCode  = 206;
                var readstream = Reactor.File.Reader.Create(fullpath, start, length, FileMode.OpenOrCreate, FileShare.ReadWrite);
                readstream.Pipe(context.Response);
            }
        }

        /// <summary>
        /// Formats the filesize to human readable string.
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        private string FormatFileLength(long length) {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; 
            if (length == 0)
                return "0" + suf[0];
            long bytes = System.Math.Abs(length);
            int place = System.Convert.ToInt32(System.Math.Floor(System.Math.Log(bytes, 1024)));
            double num = System.Math.Round(bytes / System.Math.Pow(1024, place), 1);
            return (System.Math.Sign(length) * num).ToString() + suf[place];
        }

        /// <summary>
        /// Processes the contents of a directory.
        /// </summary>
        /// <param name="context">The http context.</param>
        /// <param name="basepath"></param>
        /// <param name="fullpath"></param>
        private void ProcessDirectory(Reactor.Http.Context context, string fullpath) {
            var builder = new System.Text.StringBuilder();
            var info    = new DirectoryInfo(fullpath);
            builder.Append("<html>");
            builder.Append("<style type='text/css'>");
            builder.Append("body { font-family: monospace; } ");
            builder.Append("a {color: black; text-decoration: none; } ");
            builder.Append("ul {line-style: none; } ");
            builder.Append("li.folder { list-style: url(data:image/png;base64,"+foldericon+"); } ");
            builder.Append("li.file { list-style: url(data:image/png;base64,"+fileicon+"); } ");
            builder.Append("</style>");
            var currentPath = Reactor.Net.HttpUtility.UrlDecode(context.Request.Url.AbsolutePath);
            builder.Append(string.Format("<h4>{0}</h4>", currentPath));
            builder.Append("<ul>");
            foreach (var directory in info.GetDirectories()) {
                var webpath = Path.Combine(currentPath, directory.Name).Replace("\\", "/");
                builder.Append(string.Format("<li class=\"folder\"><a href=\"{0}\">{1}</a></li>", webpath, directory.Name));
            }
            foreach (var file in info.GetFiles()) {
                var webpath = Path.Combine(currentPath, file.Name).Replace("\\", "/");
                builder.Append(string.Format("<li class=\"file\"><a href=\"{0}\">{1} ({2})</a></li>", webpath, file.Name, this.FormatFileLength(file.Length)));
            }
            builder.Append("</ul>");
            builder.Append("</html>");
            context.Response.ContentType = "text/html";
            context.Response.End(builder.ToString());
        }

        #endregion

        #region Statics

        /// <summary>
        /// Creates a new static router.
        /// </summary>
        /// <param name="basePath">The URI base.</param>
        /// <param name="directory">The local directory from which to serve.</param>
        /// <returns>A static content router.</returns>
        public static StaticRouter Create(string basePath, string directory) {
            return new StaticRouter(basePath, directory, false);
        }

        /// <summary>
        /// Creates a new static router.
        /// </summary>
        /// <param name="basePath">The URI base.</param>
        /// <param name="directory">The local directory from which to serve.</param>
        /// <param name="browsable">Allow for directory browsing.</param>
        /// <returns>A static content router.</returns>
        public static StaticRouter Create(string basePath, string directory, bool browsable) {
            return new StaticRouter(basePath, directory, browsable);
        }

        #endregion

        #region Implicit Cast

        /// <summary>
        /// Implicit cast to middleware.
        /// </summary>
        /// <param name="staticFiles"></param>
        public static implicit operator Reactor.Action<Reactor.Http.Context, 
                                        Reactor.Action<Reactor.Http.Context>> (Reactor.Web.StaticRouter staticFiles) {
            return staticFiles.Process;
        }

        #endregion
    }
}
