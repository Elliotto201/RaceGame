using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matchmaking
{
    internal static class MathUtils
    {
        public static uint Median(IEnumerable<uint> source)
        {
            if (source == null || !source.Any())
                throw new ArgumentException("Source cannot be null or empty.");

            var sorted = source.OrderBy(x => x).ToArray();
            int count = sorted.Length;
            int mid = count / 2;

            return (count % 2 != 0)
                ? sorted[mid]
                : (uint)((sorted[mid - 1] + sorted[mid]) / 2);
        }

        public static uint Distance(uint a, uint b)
        {
            return a > b ? a - b : b - a;
        }
    }
}
