using LocalizationTracker.Data;
using LocalizationTracker.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DocumentFormat.OpenXml.Presentation;
using StringsCollector.Data;
using StringsCollector.Data.Wrappers;
using LocalizationTracker.Tools.GlossaryTools;
using StringsCollector.Data.Unity;
using StringsCollector.Data.Unreal;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace LocalizationTracker.Logic
{
    public static class StringManager
    {
        public enum EngineType
        {
            Unity,
            Unreal
        }

        public record struct StringData(
            StringEntry[] Strings,
            Dictionary<string, StringEntry> StringsByKey,
            Dictionary<string, List<StringEntry>> Duplicates,
            List<(string, int)> AllFolders);

        private static StringsArchive? Archive { get; set; }

        public static StringEntry[] AllStrings { get; private set; } = Array.Empty<StringEntry>();

        public static List<DialogsData> AllDialogs { get; private set; } = new();

        public static Dictionary<string, StringEntry> StringsByKey { get; private set; } = new();

        public static Dictionary<string, List<StringEntry>> Duplicates = new();

        // all possible folders, sorted by folder, with counts
        public static List<(string, int)> AllFolders = new();

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
                .Select(s => (s.Key, s.Count()))
                .OrderBy(s => s.Key, StringComparer.InvariantCultureIgnoreCase)
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
                if (str.AssetStatus == AssetStatus.NotUsed)
                {
                    str.Data.AddStringTrait("NotUsed", true);
                }
            }

            return new StringData(newStrings.ToArray(), stringsByKey, duplicatesDict, folders);
        }

        public static IEnumerable<string> GetAllFoldersLeadingTo(StringEntry se)
        {
            var dir = se.PathRelativeToStringsFolder;

            var idx = 0;
            var directorySeparatorChar = "/"; //Path.DirectorySeparatorChar.ToString(); this is the URI, not path
            do
            {
                idx = dir.IndexOf(directorySeparatorChar, idx, StringComparison.Ordinal);
                if (idx > 0)
                {
                    yield return
                        dir.Substring(
                            0,
                            idx + 1); // include trailing '/': we need it for correct sorting (otherwise Foo and FooBar may sort differently compared to Foo/ and Foobar/)  
                    idx++;
                }
            } while (idx > 0);
        }

        public static void InitializeArchive(EngineType instanceEngine)
        {
            Archive = instanceEngine == EngineType.Unity
                ? new StringsArchiveUnity()
                : new StringsArchiveUnreal();
        }

        public static void DeleteStrings(StringEntry[] selected)
        {
            if (selected.Length == 0)
                return;

            if (Archive == null) throw new NullReferenceException("Archive not initialized");

            var deleteTask = Task.Run(() => selected.AsParallel().ForEach(item => Archive.Delete(item.Data)));
            var selectedHashSet = selected.ToHashSet();
            var newStrings = AllStrings.AsParallel().Where(s => !selectedHashSet.Contains(s)).ToArray();

            var stringData = PrepareAdditionalStringData(newStrings);
            SetNewStrings(stringData);
            deleteTask.Wait();
        }

        private static List<StringsArchiveUnity.ArchiveEntry> LoadAll(DirectoryInfo rootDir, CancellationToken ct, DirectoryInfo? dialogsDir)
        {
            if (Archive == null) throw new NullReferenceException("Archive not initialized");
            return Archive.LoadAll(rootDir, ct, dialogsDir);
        }

        private static void LoadAllDialogs(CancellationToken ct, DirectoryInfo dialogsDir)
        {
            if (Archive == null) throw new NullReferenceException("Archive not initialized");
            AllDialogs = Archive.LoadAllDialogs(ct, dialogsDir);
        }

        public static void Save(IStringData data)
        {
            if (Archive == null) throw new NullReferenceException("Archive not initialized");
            Archive.Save(data);
        }

        public static IStringData Reload(IStringData data)
        {
            if (Archive == null) throw new NullReferenceException("Archive not initialized");
            return Archive.Reload(data);
        }

        public static DialogsData ReloadDialog(DialogsData data)
        {
            if (Archive == null) throw new NullReferenceException("Archive not initialized");
            return Archive.ReloadDialog(data);
        }

        public static bool IsFileModified(IStringData data)
        {
            if (Archive == null) throw new NullReferenceException("Archive not initialized");
            return Archive.IsFileModified(data);
        }


        public static async Task Scan(DirectoryInfo rootDir, CancellationToken ct, DirectoryInfo? dialogsDir)
        {
            var existingUnmodifiedTask = Task.Run(
                () => AllStrings
                    .AsParallel()
                    .WithCancellation(ct)
                    .Where(v => !IsFileModified(v.Data))
                    .Select(se => se)
                    .ToArray(),
                ct);

            if (dialogsDir != null)
            {
                LoadAllDialogs(ct, dialogsDir);
            }

            var existingUnmodified = await existingUnmodifiedTask;
            var loadedStrings = LoadAll(rootDir, ct, dialogsDir);

            var voiceComments = AllDialogs
                            .SelectMany(s => s.Nodes)
                            .Where(node => node.VOComment != null);
            List<StringEntry> newStringsList = new List<StringEntry>();

            foreach (var item in voiceComments)
            {
                var virtualString = new StringEntry(new LocalizedStringData
                {
                    StringPath = $"Strings/_Virtual/{item.Id}.json",
                    Key = item.Id,
                    Source = Locale.ruRU,
                    AbsolutePath = $"{AppConfig.Instance.AbsStringsFolder}/_Virtual/{item.Id}.json".Replace("\\", "/"),
                    Languages = new List<LocaleData>
                            {
                                new LocaleData { Text = "Dialog comment", TranslatedFrom = Locale.ruRU, Locale = Locale.enGB, VoiceComment = item.VOComment[Locale.enGB], ModificationDate = DateTime.UtcNow},
                                new LocaleData { Text = "Dialog comment", Locale = Locale.ruRU, VoiceComment = item.VOComment[Locale.ruRU], ModificationDate = DateTime.UtcNow }
                            },
                });

                virtualString.DialogsDataList = new List<DialogsData> { AllDialogs.First(f => f.Nodes.Any(w => w.Id == item.Id)) };
                virtualString.UpdateLocaleEntries();
                newStringsList.Add(virtualString);
            }

            // now files = ExistingModified + New
            Glossary.Instance.RecalculateTerms();

            var loadedStringsResult = loadedStrings
              .AsParallel()
              .Select(
                lsd =>
                {
                    switch (lsd.State)
                    {
                        case StringsArchive.ArchiveEntryState.Unmodified:
                            return AllStrings.FirstOrDefault(s => s.Data == lsd.Data);
                        case StringsArchive.ArchiveEntryState.Added:
                            {
                                var se = new StringEntry(lsd.Data);
                                se.UpdateLocaleEntries();
                                return se;
                            }
                        case StringsArchive.ArchiveEntryState.Deleted:
                        default:
                            return null;
                    }
                })
              .Where(s => s != null)
              .OrderBy(se => se)
              .ToArray();

            newStringsList.AddRange(loadedStringsResult);
            StringEntry[] newStrings = newStringsList.ToArray();

            var stringData = PrepareAdditionalStringData(newStrings!);

            SetNewStrings(stringData);

            // var newLoadedEntries = LoadedStrings
            // 	.AsParallel()
            // 	.WithCancellation(ct)
            // 	.Select(
            // 		sd =>
            // 		{
            // 			var se = new StringEntry(sd);
            // 			se.UpdateLocaleEntries();
            // 			return se;
            // 		});
            //
            // var newStrings = existingUnmodified
            // 	.AsParallel()
            // 	.Concat(newLoadedEntries)
            // 	.OrderBy(se => se)
            // 	.ToArray();

        }
    }
}
