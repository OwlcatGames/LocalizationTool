using JetBrains.Annotations;
using Kingmaker.Localization.Shared;
using LocalizationTracker.Logic;
using LocalizationTracker.Utility;
using LocalizationTracker.Windows;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using LocalizationTracker.Data.Unreal;
using LocalizationTracker.Data.Wrappers;
using System.ComponentModel;
using DocumentFormat.OpenXml.Wordprocessing;
using Google.Apis.Sheets.v4.Data;
using DocumentFormat.OpenXml.Drawing;

namespace LocalizationTracker.Data
{
    public partial class StringEntry : IComparable<StringEntry>
    {
        public static event Action<string> StaticPropertyChanged;

        public static string SelectedDirPrefix;

        private static Locale? s_SourceLocale;
        private static string s_SourceTrait = LocaleTrait.Final.ToString();
        private static Locale? s_TargetLocale;

        public static Locale SourceLocale
        {
            get => s_SourceLocale ?? Locale.DefaultFrom;
            set
            {
                if (value == s_SourceLocale)
                    return;
                s_SourceLocale = value;
                StaticPropertyChanged?.Invoke(nameof(SourceLocale));
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
                StaticPropertyChanged?.Invoke(nameof(TargetLocale));
                UpdateAllLocaleEntries();
            }
        }

        [NotNull]
        public string Key => Data.Key;

        // may use backward or forward slashes, no guarantees
        [NotNull]
        public string AbsolutePath { get; }
        private readonly string m_Name;

        // uses forward slashes '/'
        [NotNull]
        public string PathRelativeToStringsFolder => Data.StringPath;

        // excludes string name, has trailing slash, uses forward slashes '/'
        public string DirectoryRelativeToStringsFolder =>
            m_DirectoryRelativeToStringsFolder ??= Data.StringPath.Substring(0, Data.StringPath.LastIndexOf('/') + 1);

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

        public LocaleEntry TargetLocaleEntry { get; private set; }

        public string Speaker => Data.Speaker;
        public ParentId ParentId => Data.ParentId;


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

        public StringEntry(string absolutePath)
        {
            AbsolutePath = absolutePath;
            m_Name = System.IO.Path.GetFileName(absolutePath);
            //FileModificationTime = modificationTime;
        }

        public StringEntry(IStringData data)
        {
            Data = data;
            AbsolutePath = data is LocalizedStringData lsd ? lsd.AbsolutePath : data.StringPath; // todo: get rid of absolutepath maybe?
            m_Name = System.IO.Path.GetFileName(AbsolutePath);

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
            else if (StringManager.Filter.Mode == FilterMode.Updated_Trait)
            {
                SourceLocaleEntry = new LocaleEntry(this, SourceLocale);
                TargetLocaleEntry = LocaleEntry.Empty;
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
            StringManager.Archive.Save(Data);
        }

        public void Reload()
        {
            Data = StringManager.Archive.Reload(Data);
        }

        public bool IsFileModified()
        {
            return StringManager.Archive.IsFileModified(Data);
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