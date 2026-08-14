using System;
using System.Collections.Generic;

namespace CounterTool.Utils;

public static class EnumerableUtils
{
    extension(System.Range range)
    {
        public IEnumerable<int> EnumerateChunked(int step)
        {
            for (var x = range.Start.Value; x < range.End.Value; x += step)
            {
                yield return x;
            }
        }
    }
}