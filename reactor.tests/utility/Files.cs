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

using System.Security.Cryptography;

namespace Reactor.Tests.Utility
{
    public static class Files {

        #region Compare

        /// <summary>
        /// Compares two files. Returns true if these files are the same.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <returns></returns>
        public static bool AreSame (string src, string dst) {
            try {
                var hash0 = "";
                var hash1 = "";
                using (var md5 = MD5.Create()) {
                    using (var stream = System.IO.File.OpenRead(src)) {
                        hash0 = System.Convert.ToBase64String(md5.ComputeHash(stream));
                    }
                }
                using (var md5 = MD5.Create()) {
                    using (var stream = System.IO.File.OpenRead(dst)) {
                        hash1 = System.Convert.ToBase64String(md5.ComputeHash(stream));
                    }
                }
                return (hash0 == hash1);
            } catch{
                return false;
            }
        }

        #endregion

        #region Delete

        public static bool Delete(string filename){
            try {
                if (System.IO.File.Exists(filename)) {
                    System.IO.File.Delete(filename);
                }
                return true;
            }
            catch {
                return false;
            }
        }
        

        #endregion

        #region Numeric Sequence

        /// <summary>
        /// Creates new file containing a numeric sequence of System.Int32. The
        /// iteration count is the number of Int32's to write, therefore, a
        /// iteration count of 100 will result in a file 400 bytes long.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="iteration"></param>
        public static bool CreateNumbericSequenceFile (string filename, int iteration) {
            try {
                var mode = System.IO.FileMode.Create;
                if(System.IO.File.Exists(filename)) mode = System.IO.FileMode.Truncate;
                var file = new System.IO.FileStream(filename, mode, System.IO.FileAccess.Write);
                for (int i = 0; i < iteration; i++) {
                    var data = System.BitConverter.GetBytes(i);
                    file.Write(data, 0, data.Length);
                }
                file.Dispose();
                return true;
            }
            catch {
                return false;
            }
        }
        
        /// <summary>
        /// Verifies a NumericSequenceFile. This function will check that
        /// the number of Int32's read from this file are in proper sequenctial
        /// order. In addition, this function will begin checking sequence
        /// from the first Int32 read from this file.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static bool ValidateNumericSequenceFile(string filename) {
            try {
                if(!System.IO.File.Exists(filename)) return false;
                var info = new System.IO.FileInfo(filename);
                if(info.Length % 4 != 0) return false;
                var file = new System.IO.FileStream(filename, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                var started = false;
                var index   = 0;
                var data    = new byte[4];
                while (file.Read(data, 0, 4) != 0) {
                    if (!started) {
                        index = System.BitConverter.ToInt32(data, 0);
                    }
                    else {
                        var temp = System.BitConverter.ToInt32(data, 0);
                        if (temp != (index + 1)) return false;
                        index = temp;
                    }
                }
                file.Dispose();
                return true;
            }
            catch {
                return false;
            }
        }

        #endregion
    }
}
