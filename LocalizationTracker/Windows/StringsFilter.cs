using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Odbc;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Presentation;
using JetBrains.Annotations;
using Kingmaker.Localization.Shared;
using LocalizationTracker.Data;
using LocalizationTracker.Data.Unreal;
using LocalizationTracker.Logic;
using LocalizationTracker.Tools;
using LocalizationTracker.Tools.GlossaryTools;
using LocalizationTracker.Utility;
using LocalizationTracker.Components;
using System.Windows.Threading;

namespace LocalizationTracker.Windows
{
    public class StringsFilter : DependencyObject, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
    
        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(info));
        }

        private bool m_IgnoreCase = true;
        private bool m_HideTags = false;
        private string m_Name = "";
        private string m_Text;
        private string m_Speaker;
        private string m_Comment;
        private string m_NamePattern;

        private DateTime? m_ModificationDateFrom;
        private DateTime? m_ModificationDateTo;
        private Locale? m_ModificationLocale;

        private Color? m_Color;

        private FilterMode m_Mode;

        public bool IgnoreCase
        {
            get => m_IgnoreCase;
            set
            {
                m_IgnoreCase = value;
                Updated?.Invoke();
            }
        }
        public bool HideTags
        {
            get => m_HideTags;
            set
            {
                m_HideTags = value;
                Updated?.Invoke();
                HideTagsUpdated?.Invoke();
            }
        }
        public string NamePattern => m_NamePattern;

        public string Name
        {
            get => m_Name.Replace("\n", "\r\n").Contains(Environment.NewLine, StringComparison.InvariantCultureIgnoreCase) ? "<Multiple lines>" : m_Name;
            set
            {
                m_Name = value;
                m_NamePattern = StringUtils.ToPattern(value);
                NotifyPropertyChanged(nameof(Name));
                NotifyPropertyChanged(nameof(NameMultiline));
                if (string.IsNullOrEmpty(value))
                    Updated?.Invoke();
            }
        }
        public string NameMultiline
        {
            get => m_Name;
            set
            {
                m_Name = value;
                m_NamePattern = StringUtils.ToPattern(value);
                //Updated?.Invoke();
                NotifyPropertyChanged(nameof(Name));
                NotifyPropertyChanged(nameof(NameMultiline));
            }
        }

        public string Text
        {
            get => m_Text;
            set
            {
                m_Text = value;
                if (string.IsNullOrEmpty(value))
                    Updated?.Invoke();
            }
        }

        public string Speaker
        {
            get => m_Speaker;
            set
            {
                m_Speaker = value;
                if (string.IsNullOrEmpty(value))
                    Updated?.Invoke();
            }
        }

        public string Comment
        {
            get => m_Comment;
            set
            {
                m_Comment = value;
                if (string.IsNullOrEmpty(value))
                    Updated?.Invoke();
            }
        }

        public FilterMode Mode
        {
            get => m_Mode;
            set
            {
                m_Mode = value;
                Updated?.Invoke();
                ModeUpdated?.Invoke();
            }
        }

        public DateTime? ModificationDateFrom
        {
            get => m_ModificationDateFrom;
            set
            {
                m_ModificationDateFrom = value;
                //Updated?.Invoke();
            }
        }

        public DateTime? ModificationDateTo
        {
            get => m_ModificationDateTo;
            set
            {
                m_ModificationDateTo = value;
                //Updated?.Invoke();
            }
        }

        public string ModificationLocale
        {
            get
            {
                if (m_ModificationLocale == null)
                    return "";
                return m_ModificationLocale.ToString();
            }
            set
            {
                if (string.IsNullOrEmpty(value) ^ m_ModificationLocale == null)
                    ModificationTraits.Clear();

                if (string.IsNullOrEmpty(value))
                    m_ModificationLocale = null;
                else
                    m_ModificationLocale = value;
                ModificationTraitsAreLocale = m_ModificationLocale != null;
                //Updated?.Invoke();
            }
        }

        public Color? SelectedColor
        {
            get => m_Color;
            set
            {
                m_Color = value;
                Updated?.Invoke();
            }
        }

        public bool ModificationTraitsAreLocale
        {
            get => (bool)GetValue(ModificationTraitsAreLocaleProperty);
            set => SetValue(ModificationTraitsAreLocaleProperty, value);
        }

        public static readonly DependencyProperty ModificationTraitsAreLocaleProperty =
            DependencyProperty.Register("ModificationTraitsAreLocale", typeof(bool), typeof(StringsFilter));

        [NotNull]
        public ObservableCollection<string> ModificationTraits { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> UpdatedFilterTraits { get; } = new ObservableCollection<string>();

        [NotNull]
        public ObservableCollection<TraitsFilter> TraitFilters { get; } = new ObservableCollection<TraitsFilter>();
        public bool TraitCheckOr { get => traitCheckOr; set { traitCheckOr = value; Updated?.Invoke(); } }
        private bool traitCheckOr = false; // true - Or, false - AND

        public event FilterUpdateHandler Updated;

        public event FilterUpdateHandler ModeUpdated;

        public event FilterUpdateHandler HideTagsUpdated;

        public FilterFits filterFits = new FilterFits();

        public StringsFilter()
        {
            AddFirstTraitsFilter();
            UpdatedFilterTraits.CollectionChanged += (e, s) => Updated?.Invoke();
            ModificationTraits.CollectionChanged += (e, s) => Updated?.Invoke();
        }

        public bool Fits(StringEntry stringEntry)
        {
            if (!string.IsNullOrWhiteSpace(NamePattern) &&
                !NameMultiline.Split(Environment.NewLine).Contains(stringEntry.Data.Key) && !StringUtils.MatchesPattern(stringEntry.PathRelativeToStringsFolder, NamePattern, m_IgnoreCase))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(Speaker) &&
                !StringUtils.MatchesFilter(stringEntry.Data.Speaker, Speaker, m_IgnoreCase))
                return false;

            if (!string.IsNullOrEmpty(Comment) &&
                !StringUtils.MatchesFilter(stringEntry.Data.Comment, Comment, m_IgnoreCase) && 
                !StringUtils.MatchesFilter(stringEntry.SourceLocaleEntry.TranslatorComment, Comment, m_IgnoreCase) &&
                !StringUtils.MatchesFilter(stringEntry.TargetLocaleEntry.TranslatorComment, Comment, m_IgnoreCase))
                return false;

            if (!string.IsNullOrEmpty(Text) &&
                !StringUtils.MatchesFilter(stringEntry.SourceLocaleEntry.Text, Text, m_IgnoreCase) &&
                !StringUtils.MatchesFilter(stringEntry.TargetLocaleEntry.Text, Text, m_IgnoreCase))
                return false;

            if (m_ModificationDateFrom != null || m_ModificationDateTo != null)
            {
                if (!CheckModificationTime(stringEntry))
                    return false;
            }

            if (Mode == FilterMode.Updated_Source &&
                !stringEntry.SourceLocaleEntry.IsTranslationSourceUpdated)
                return false;

            if (Mode == FilterMode.Spelling_Errors &&
                !stringEntry.SourceLocaleEntry.HasSpellingErrors &&
                !stringEntry.TargetLocaleEntry.HasSpellingErrors)
                return false;

            if (Mode == FilterMode.Updated_Trait)
            {
                var localeData = stringEntry.SourceLocaleEntry;
                localeData.MismatchedTraitString = "";
                foreach (var trait in UpdatedFilterTraits)
                {
                    var traitData = stringEntry.Data.GetTraitData(localeData.Locale, trait);
                    if (traitData != null && localeData.Text != traitData.LocaleText)
                    {
                        localeData.MismatchedTraitString = traitData.LocaleText;
                        break;
                    }
                }
                if (localeData.MismatchedTraitString == "")
                    return false;
            }

            if (Mode == FilterMode.Key_Duplicates &&
                !StringManager.Duplicates.ContainsKey(stringEntry.Data.Key))
                return false;

            if (Mode == FilterMode.Glossary_Mismatch &&
                !Glossary.Instance.StringEntryHasTermsMismatch(stringEntry))
                return false;

            if (TraitCheckOr)
            {
                bool anyTrueExist = false;
                foreach (var tf in TraitFilters)
                {
                    if (tf.CheckString(stringEntry))
                    {
                        anyTrueExist = true;
                        break;
                    }
                }

                if (!anyTrueExist)
                    return false;
            }
            else
            {
                foreach (var tf in TraitFilters)
                {
                    if (!tf.CheckString(stringEntry))
                        return false;
                }
            }

            if (Mode == FilterMode.Tags_Mismatch &&
                TagsMatch(stringEntry.SourceLocaleEntry, stringEntry.TargetLocaleEntry))
                return false;

            if (Mode == FilterMode.Unreal_Unused && stringEntry.Data is UnrealStringData { Unused: false })
                return false;

            return true;
        }

        private bool TagsMatch(LocaleEntry e1, LocaleEntry e2)
        {
            if (string.IsNullOrEmpty(e1.Text) || string.IsNullOrEmpty(e2.Text))
                return true;

            if (e1.TagsList.HasUnmatchedTags.HasValue && e1.TagsList.HasUnmatchedTags == e2.TagsList.HasUnmatchedTags)
                return !e1.TagsList.HasUnmatchedTags.Value;

            if (e1.TagsList.Tags.Any(t => t.WrongOpenClose) || e2.TagsList.Tags.Any(t => t.WrongOpenClose))
                return false; // consider tag closing errors as match errors

            return TagsList.Compare(e1.TagsList, e2.TagsList);
        }

        private bool CheckModificationTime(StringEntry stringEntry)
        {
            if (ModificationTraits.Count == 0)
            {
                foreach (var ld in stringEntry.Data.Languages)
                {
                    if (m_ModificationLocale == null || ld.Locale == m_ModificationLocale)
                    {
                        if (m_ModificationDateFrom != null && ld.ModificationDate < m_ModificationDateFrom)
                            continue;

                        if (m_ModificationDateTo != null && ld.ModificationDate > m_ModificationDateTo + TimeSpan.FromDays(1))
                            continue;

                        return true;
                    }
                }
            }
            else
            {
                var traits = m_ModificationLocale == null
                    ? stringEntry.Data.StringTraits
                    : stringEntry.Data.GetLocale(m_ModificationLocale)?.Traits;

                if (traits != null)
                {
                    foreach (var t in traits)
                    {
                        if (!ModificationTraits.Contains(t.Trait))
                            continue;

                        if (m_ModificationDateFrom != null && t.ModificationDate < m_ModificationDateFrom)
                            continue;

                        if (m_ModificationDateTo != null && t.ModificationDate > m_ModificationDateTo + TimeSpan.FromDays(1))
                            continue;

                        return true;
                    }
                }
            }

            return false;
        }

        private void AddFirstTraitsFilter()
        {
            var traitsFilter = new TraitsFilter();
            traitsFilter.Locale = Locale.Empty;
            traitsFilter.Traits.Add(StringTrait.Invalid.ToString());
            traitsFilter.Not = true;
            traitsFilter.Updated += () => Updated?.Invoke();
            TraitFilters.Add(traitsFilter);
        }

        public void AddTraitsFilter()
        {
            var traitsFilter = new TraitsFilter();
            traitsFilter.Updated += () => Updated?.Invoke();
            TraitFilters.Add(traitsFilter);
        }

        public void RemoveTraitFilter(TraitsFilter filter)
        {
            TraitFilters.Remove(filter);
            Updated?.Invoke();
        }

        public void ClearTraitFilters()
        {
            TraitFilters.Clear();
            Updated?.Invoke();
        }

        public void SaveTraitFilters(string fileName)
        {

            var list = TraitFilters.Select(f => new TraitsFilter.Json(f)).ToList();
            using StreamWriter sw = new(fileName);
            sw.WriteLine(JsonSerializer.Serialize(list, JsonSerializerHelpers.JsonSerializerOptions));
        }

        public void LoadTraitFilters(string fileName)
        {
            using (var sr = new StreamReader(fileName))
            {
                var text = sr.ReadToEnd();
                var list = JsonSerializer.Deserialize<List<TraitsFilter.Json>>(text, JsonSerializerHelpers.JsonSerializerOptions);
                TraitFilters.Clear();
                foreach (var json in list)
                {
                    var filter = new TraitsFilter(json);
                    filter.Updated += () => Updated?.Invoke();
                    TraitFilters.Add(filter);
                }
                Updated?.Invoke();
            }

        }

        public void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            Updated?.Invoke();
        }
    }

    public delegate void FilterUpdateHandler();
}