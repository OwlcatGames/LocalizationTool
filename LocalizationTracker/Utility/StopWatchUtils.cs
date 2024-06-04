using System;
using System.Diagnostics;

namespace LocalizationTracker
{
	static class StopWatchUtils
	{
		/// <summary>
		/// Gets estimated time on compleation. 
		/// </summary>
		public static TimeSpan GetEta(this Stopwatch sw, int counter, int counterGoal)
		{
			if (counter == 0) return TimeSpan.Zero;
			float elapsedMin = ((float)sw.ElapsedMilliseconds / 1000) / 60;
			float minLeft = (elapsedMin / counter) * (counterGoal - counter); //see comment a
			TimeSpan ret = TimeSpan.FromMinutes(minLeft);
			return ret;
		}
	}
}
