using System;
using System.Collections.Generic;

namespace LocalizationTracker.Data
{
    public static class StringEntryComparison
    {
        public static int LimitLen(StringEntry x, string y)
        {
            if (x == null && y == null)
                return 0;

            if (x == null)
                return -1;

            if (y == null)
                return 1;

            int len = y.Length;

            var dirCompareResult = string.Compare(x.DirectoryRelativeToStringsFolder, 0, y, 0, len, StringComparison.InvariantCultureIgnoreCase);
            if(dirCompareResult == 0)
            {
                if (x.DirectoryRelativeToStringsFolder.Length == len)
                    return 0;
                return x.DirectoryRelativeToStringsFolder[len-1]=='/' ? 0 : 1;
            }
            return dirCompareResult;
        }

        public static int Exact(StringEntry x, string y)
        {
            if (x == null && y == null)
                return 0;

            if (x == null)
                return -1;

            if (y == null)
                return 1;

            var dirCompareResult = string.Compare(x.DirectoryRelativeToStringsFolder, y, StringComparison.InvariantCultureIgnoreCase);
            return dirCompareResult;
        }
    }
}
