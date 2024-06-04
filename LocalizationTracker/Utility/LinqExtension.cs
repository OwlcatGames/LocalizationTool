using System;
using System.Collections.Generic;

namespace LocalizationTracker.Utility
{
	public static class LinqExtension
	{
		public static int IndexAt<T>(this IList<T> source, Predicate<T> predicate)
		{
			for (int i = 0; i < source.Count; i++)
			{
				if (predicate(source[i]))
					return i;
			}

			return -1;
		}
	}
}