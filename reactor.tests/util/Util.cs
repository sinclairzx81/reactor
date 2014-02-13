using System;
using System.Collections.Generic;
using System.Text;

namespace Reactor.Tests
{
    public static class Util
    {
        public static string SizeSuffix(long value)
        {
            string[] suffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

            int i = 0;

            decimal dValue = (decimal)value;

            while (Math.Round(dValue / 1024) >= 1)
            {
                dValue /= 1024;

                i++;
            }

            return string.Format("{0:n1} {1} [{2} bytes]", dValue, suffixes[i], value);
        }
    }
}
