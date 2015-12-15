using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RHCLib
{
    public static class Sorter
    {
        public enum SortType : byte
        {
            GreatestDistanceFirst,
            ShortestDistanceFirst
        }

        public static IEnumerable<IVector> RadialSort(IEnumerable<IVector> vectors, IVector focus, DistanceDelegate measure, SortType type)
        {
            switch (type)
            {
                case SortType.GreatestDistanceFirst:
                    return Enumerable.OrderByDescending(vectors, vector => measure(focus, vector)).ToList();
                case SortType.ShortestDistanceFirst:
                    return Enumerable.OrderBy(vectors, vector => measure(focus, vector)).ToList();
                default:
                    throw new Exception("Invalid sort type.");
            }
        }
    }
}
