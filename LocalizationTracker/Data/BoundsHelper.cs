using System.Collections.Generic;

namespace LocalizationTracker.Data
{
    public delegate int Comparison<in T, in Q>(T x, Q y);

    public static class BoundsHelper
    {
        public static int LowerBound<T, Q>(this IList<T> array, int start, int count, Q value, Comparison<T, Q> comparison)
        {
            while (count > 0)
            {
                int step = count / 2;
                int middle = start + step;

                if (comparison(array[middle], value) < 0)
                {
                    start = middle + 1;
                    count -= step + 1;
                }
                else
                    count = step;
            }

            return start;
        }

        public static int LowerBound<T>(this IList<T> array, int start, int count, T value)
            => LowerBound(array, start, count, value, Comparer<T>.Default.Compare);

        public static int LowerBound<T>(this IList<T> array, T value)
            => LowerBound(array, 0, array.Count, value, Comparer<T>.Default.Compare);

        public static int LowerBound<T, Q>(this IList<T> array, Q value, Comparison<T, Q> comparison)
            => LowerBound(array, 0, array.Count, value, comparison);

        public static int UpperBound<T, Q>(this IList<T> array, int start, int count, Q value, Comparison<T, Q> comparer)
        {
            while (count > 0)
            {
                int step = count / 2;
                int middle = start + step;

                if (comparer(array[middle], value) <= 0)
                {
                    start = middle + 1;
                    count -= step + 1;
                }
                else
                    count = step;
            }
            return start;
        }

        public static int UpperBound<T>(this IList<T> array, int start, int count, T value)
            => UpperBound(array, start, count, value, Comparer<T>.Default.Compare);

        public static int UpperBound<T>(this IList<T> array, T value)
            => UpperBound(array, 0, array.Count, value, Comparer<T>.Default.Compare);

        public static int UpperBound<T, Q>(this IList<T> array, Q value, Comparison<T, Q> comparer)
            => UpperBound(array, 0, array.Count, value, comparer);
    }
}
