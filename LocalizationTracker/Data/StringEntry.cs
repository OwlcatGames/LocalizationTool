using JetBrains.Annotations;
using LocalizationTracker.Logic;
using LocalizationTracker.Windows;
using System;
using System.Threading.Tasks;
using System.Windows.Media;
using Path = System.IO.Path;
using System.Linq;
using System.Collections.Generic;
using StringsCollector.Data.Unity;
using StringsCollector.Data.Wrappers;
using StringsCollector.Data.Unreal;

namespace LocalizationTracker.Data
{
    public partial class StringEntry : IComparable<StringEntry>
    {
        public static event Action? SourceLocaleChanged;
        public static event Action? TargetLocaleChanged;

        public static string SelectedDirPrefix;
        private static Locale? s_SourceLocale;
        
        private static string s_SourceTrait = LocaleTrait.Loc_Final.ToString();
        
        private static Locale? s_TargetLocale;

        public static Locale SourceLocale
        {
            get => s_SourceLocale ?? Locale.DefaultFrom;
            set
            {
                if (value == s_SourceLocale)
                    return;
                s_SourceLocale = value;
                SourceLocaleChanged?.Invoke();
                UpdateAllLocaleEntries();
            }
        }

        public static Locale TargetLocale
        {
            get => s_TargetLocale ?? Locale.DefaultTo;
            set
            {
                if (value == s_TargetLocale)
                    return;
                s_TargetLocale = value;
                TargetLocaleChanged?.Invoke();
                UpdateAllLocaleEntries();
            }
        }

        [NotNull]
        public string Key => Data.Key;

        // may use backward or forward slashes, no guarantees
        [NotNull]
        private readonly string m_Name;

        public string AbsolutePath { get; }
        public string StringPath { get; set; }

        // uses forward slashes '/'
        [NotNull]
        public string PathRelativeToStringsFolder => StringPath;

        public List<DialogsData?> DialogsDataList { get; set; }


        // excludes string name, has trailing slash, uses forward slashes '/'
        public string DirectoryRelativeToStringsFolder =>
            m_DirectoryRelativeToStringsFolder ??= StringPath.Substring(0, StringPath.LastIndexOf('/') + 1);

        [NotNull]
        private string m_SelectedFolderPath;

        public string SelectedFolderPath
        {
            get
            {
                if (m_SelectedFolderPath == "")
                {
                    // todo: this only works for paths that are actually INSIDE the selected dir
                    // which is probably always the case?
                    m_SelectedFolderPath = PathRelativeToStringsFolder.Substring(SelectedDirPrefix.Length);
                }
                return m_SelectedFolderPath;
            }
        }

        private AssetStatus m_AssetStatus = AssetStatus.Unknown;

        public AssetStatus AssetStatus
        {
            get => m_AssetStatus;
            set
            {
                m_AssetStatus = value;
                UseBackgroundBrush = m_AssetStatus == AssetStatus.Used || m_AssetStatus == AssetStatus.NotUsed;
            }
        }

        public bool UseBackgroundBrush { get; set; } = false;

        public Brush BackgroundBrush => AssetStatus switch
        {
            AssetStatus.Unknown => Brushes.White,
            AssetStatus.Used => Brushes.LimeGreen,
            AssetStatus.NotUsed => Brushes.Red,
            _ => Brushes.White,
        };


        public LocaleEntry SourceLocaleEntry { get; private set; }

        public LocaleEntry TargetLocaleEntry { get;  private set; }

        public string? Speaker => Data.Speaker;

        [NotNull]
        public IStringData Data;

        private string? m_DirectoryRelativeToStringsFolder;

        public string TranslationStatus
        {
            get
            {
                // 0. unused strings
                var isUnused = Data is UnrealStringData { Unused: true };
                if (isUnused)
                    return "NOT USED";

                // 1. Check if source locale has draft or ToTranslate traits
                bool isDraft = Data.GetTraitData(SourceLocale, KnownTraits.Draft) != null;
                if (isDraft)
                    return "DRAFT"; // don't care any further, probably
                bool isFinal = Data.GetTraitData(SourceLocale, KnownTraits.ToTranslate) != null;

                // 2. Check target locale pipeline state
                string translationState = Data.GetTraitData(TargetLocale, KnownTraits.Edited) != null
                    ? "Edited"
                    : Data.GetTraitData(TargetLocale, KnownTraits.SentToEd) != null
                        ? "SentToEd"
                        : Data.GetTraitData(TargetLocale, KnownTraits.Translated) != null
                            ? "Translated"
                            : Data.GetTraitData(TargetLocale, KnownTraits.SentToTr) != null
                                ? "SentToTr"
                                : "";
                isFinal = isFinal && translationState == "";

                // 3. Check if there's a MajorChange mark
                bool major = Data.GetStringTraitData(KnownTraits.MajorChange) != null;

                // 4. Check if target has been changed directly
                bool hasEngineChange = Data.GetTraitData(TargetLocale, KnownTraits.EditorChange) != null;

                // 5. Check if source or target are out of date
                string GetCurrentTranslationSource(Locale locale)
                {
                    var locData = Data.GetLocale(locale);
                    if (locData == null)
                        return "";
                    var origLoc = locData.TranslatedFrom;
                    if (origLoc == null || origLoc.Code == "")
                    {
                        return (Data as UnrealStringData)?.m_SourceText ?? "";
                    }

                    var sourceLocData = Data.GetLocale(origLoc);
                    return sourceLocData?.Text ?? "";
                }
                var sourceData = Data.GetLocale(SourceLocale);
                bool sourceOutOfDate = sourceData != null && sourceData.OriginalText != GetCurrentTranslationSource(SourceLocale);

                var targetData = Data.GetLocale(TargetLocale);
                bool targetOutOfDate = targetData != null && targetData.OriginalText != GetCurrentTranslationSource(TargetLocale);

                // build final status string

                return $"{(major ? "!!! " : "")}{(isFinal ? "Ready " : "")}{translationState}|{(hasEngineChange ? "TARGET CHANGED" : "")}{(sourceOutOfDate ? "SOURCE WRONG! " : "")}{(targetOutOfDate ? "Out of date!" : "")}";
            }
        }

        [Obsolete("Not used except tests")]
        public StringEntry(string absolutePath)
        {
            AbsolutePath = absolutePath;
            m_Name = Path.GetFileName(absolutePath);

            //FileModificationTime = modificationTime;
        }

        public StringEntry(IStringData data)
        {
            Data = data;
            
            AbsolutePath = data.SourcePath;
            StringPath = data.StringPath;
                
            m_Name = Path.GetFileName(AbsolutePath);

            if (StringManager.AllDialogs.Count() != 0)
            {
                var cleanKey = data.Key;
                var name = string.Empty;

                if (data.Key.Contains(":"))
                {
                    var findKey = data.Key.Split(":");
                    name = findKey[0].Trim();
                    cleanKey = findKey[1].Trim();
                }

                var result = StringManager.AllDialogs
                    .Select(dialog => new
                    {
                        Dialog = dialog,
                        Node = dialog.Nodes
                           .Where(node => node.Text != null &&
                                  node.Text.Key == cleanKey)
                            .ToList(),
                    })
                    .Where(x => x.Node.Count() != 0)
                    .ToList();

                if (result.SelectMany(r => r.Node).Any(node => node.Shared == true))
                {
                    result = StringManager.AllDialogs
                    .Select(dialog => new
                    {
                        Dialog = dialog,
                        Node = dialog.Nodes
                           .Where(node => node.Text != null &&
                                  node.Text.Key == cleanKey &&
                                  node.Text.Namespace == name)
                            .ToList(),
                    })
                    .Where(x => x.Node.Count() != 0)
                    .ToList();
                }

                if (result.Count() != 0)
                {
                    DialogsDataList = result.Select(s => s.Dialog).ToList();
                }
            }

            if (data is UnrealStringData usd)
            {
                AssetStatus = usd.Unused ? AssetStatus.NotUsed : AssetStatus.Used;
            }

        }

        private static void UpdateAllLocaleEntries() => Parallel.ForEach(StringManager.AllStrings, s => s.UpdateLocaleEntries());

        public void UpdateLocaleEntries()
        {
            if (SourceLocale == Locale.TranslationSource)
            {
                TargetLocaleEntry = new LocaleEntry(this, TargetLocale);
                SourceLocaleEntry = new LocaleEntry(TargetLocaleEntry);
            }
            else if (StringsFilter.Filter.Mode == FilterMode.Updated_Trait)
            {
                SourceLocaleEntry = new LocaleEntry(this, SourceLocale);
                //TargetLocaleEntry = LocaleEntry.Empty;
            }
            else
            {
                TargetLocaleEntry = new LocaleEntry(this, TargetLocale);
                SourceLocaleEntry = new LocaleEntry(this, SourceLocale);
            }
        }

        public void ClearRelativePath()
        {
            m_SelectedFolderPath = "";
        }

        public void Save()
        {
            StringManager.Save(Data);
        }

        public void Reload()
        {
            Data = StringManager.Reload(Data);
        }

        public int CompareTo(StringEntry? other)
        {
            // all comparison methods everywhere should match: OrdinalIgnoreCase and InvariantCultureIgnoreCase sort some strings differently,
            // and using both can lead to weirdness and missing strings and folders
            var dirCompareResult = StringComparer.InvariantCultureIgnoreCase.Compare(DirectoryRelativeToStringsFolder, other?.DirectoryRelativeToStringsFolder);

            if (dirCompareResult != 0 || string.IsNullOrWhiteSpace(m_Name) || string.IsNullOrWhiteSpace(other?.m_Name))
                return dirCompareResult;

            return StringComparer.InvariantCultureIgnoreCase.Compare(m_Name, other?.m_Name);
        }

        public override string ToString() => AbsolutePath;

        public void UpdateInlines()
        {
            SourceLocaleEntry.UpdateInlines();
            TargetLocaleEntry.UpdateInlines();
        }
    }

    public enum AssetStatus
    {
        Unknown,
        Used,
        NotUsed
    }
}