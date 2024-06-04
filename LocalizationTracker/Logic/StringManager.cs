using LocalizationTracker.Data;
using LocalizationTracker.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LocalizationTracker.Data.Wrappers;

namespace LocalizationTracker.Logic
{
	static class StringManager
	{
		public static StringsArchive Archive = new StringsArchiveUnity(); // todo
        public static StringEntry[] AllStrings { get; private set; } = Array.Empty<StringEntry>();

        public static Dictionary<string, StringEntry> StringsByKey { get; private set; } = new();

        public static readonly StringsFilter Filter  = new();

        public static StringEntry[] FilteredStrings { get; private set; } = Array.Empty<StringEntry>();

		public static Dictionary<string, List<StringEntry>> Duplicates = new();
		
		// all possible folders, sorted by folder, with counts
		public static List<(string, int)> AllFolders = new();

		public record struct StringData(StringEntry[] Strings, Dictionary<string, StringEntry> StringsByKey, Dictionary<string, List<StringEntry>> Duplicates, List<(string, int)> AllFolders);

		public static void SetNewStrings(StringData strings)
		{
            AllStrings = strings.Strings;
            StringsByKey = strings.StringsByKey;
            Duplicates = strings.Duplicates;
			AllFolders = strings.AllFolders;
		}

		public static StringData PrepareAdditionalStringData(StringEntry[] newStrings)
		{
			var folders = newStrings
				.AsParallel()
				.SelectMany(GetAllFoldersLeadingTo)
				.GroupBy(s => s)
				.Select(s=>(s.Key, s.Count()))
				.OrderBy(s=>s.Key, StringComparer.InvariantCultureIgnoreCase)
				.ToList();
			
            var grps = from str in newStrings.AsParallel()
                       group str by str.Key;

            var duplicates = from grp in grps
                             where grp.Skip(1).Any()
                             select grp;

            var stringsByKey = grps.ToDictionary(v => v.Key, v => v.First());

            var duplicatesDict = duplicates.ToDictionary(v => v.Key, v => v.ToList());

			foreach (var str in newStrings)
			{
				if (!str.Data.StringTraits.Select(s => s.Trait).Contains("NotUsed") && str.AssetStatus == AssetStatus.NotUsed)
				{
					str.Data.AddStringTrait("NotUsed");
				}
			}

			return new StringData(newStrings.ToArray(), stringsByKey, duplicatesDict, folders);
        }
		
		private static IEnumerable<string> GetAllFoldersLeadingTo(StringEntry se)
		{
			var dir = se.PathRelativeToStringsFolder;
			var idx = 0;
			var directorySeparatorChar = "/"; //Path.DirectorySeparatorChar.ToString(); this is the URI, not path
			do
			{
				idx = dir.IndexOf(directorySeparatorChar, idx, StringComparison.Ordinal);
				if (idx > 0)
				{
					yield return dir.Substring(0, idx+1); // include trailing '/': we need it for correct sorting (otherwise Foo and FooBar may sort differently compared to Foo/ and Foobar/)  
					idx++;
				}
			} while (idx>0);
		}

        public static void SetFilteredStrings(StringEntry[] filtered)
		{
            FilteredStrings = filtered;
		}

		public static void DeleteStrings(StringEntry[] selected)
		{
			if (selected.Length == 0)
				return;

            var deleteTask = Task.Run(() => selected.AsParallel().ForEach(item => Archive.Delete(item.Data)));
			var selectedHashSet = selected.ToHashSet();
            var newStrings = AllStrings.AsParallel().Where(s=>!selectedHashSet.Contains(s)).ToArray();

            var stringData = PrepareAdditionalStringData(newStrings);
            SetNewStrings(stringData);
            deleteTask.Wait();
        }
    }
}
