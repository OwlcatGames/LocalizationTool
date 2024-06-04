using System;
using System.Collections.Generic;

namespace LocalizationTracker
{
    public static class SystemExtentions
    {
        public static void ForEach<T>(this IEnumerable<T> ienumerable, Action<T> action)
        {
            foreach (var item in ienumerable)
            {
                action(item);
            }
        }

        public static bool Contains<T>(this IEnumerable<T> source, Predicate<T> predicate)
            => source.TryFind(predicate, out var result);

        public static bool TryFind<T>(this IEnumerable<T> source, Predicate<T> predicate, out T result)
        {
            foreach (var item in source)
            {
                if (predicate(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default;
            return false;
        }
    }
}
