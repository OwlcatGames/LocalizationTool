using JetBrains.Annotations;
using LocalizationTracker.Components;
using LocalizationTracker.Logic;
using LocalizationTracker.Tools;
using LocalizationTracker.Utility;
using LocalizationTracker.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using LocalizationTracker.Tools.GlossaryTools;
using System.ComponentModel;
using StringsCollector.Data.Unreal;
using StringsCollector.Data;
using static StringsCollector.Data.Unreal.UnrealStringData;
using System.Text.Json;
using System.IO;
using System.Windows;

namespace LocalizationTracker.Data
{
    public class LocaleEntry : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static readonly LocaleEntry Empty = new LocaleEntry();

        [NotNull]
        private readonly StringEntry m_String;

        public string StringKey => m_String.Key;

        public UnrealTraitData extraSymbols = new UnrealTraitData("ExtraSymbols");


        public bool TryGetPairedLocale(out Locale pairedLocale)
        {
            pairedLocale = this == m_String.SourceLocaleEntry ?
                m_String.TargetLocaleEntry != null ? m_String.TargetLocaleEntry.Locale : null :
                m_String.SourceLocaleEntry != null ? m_String.SourceLocaleEntry.Locale : null;

            return pairedLocale != null;
        }


        private readonly Locale m_Locale;

        private readonly bool m_Empty;

        public readonly LocaleEntry? Translation;

        public readonly string? TrackedTrait;

        private TagsList? m_TagsList;

        [NotNull]
        public string Text
        {
            get
            {
                if (m_Empty)
                    return "";

                var text = m_String.Data.GetText(m_Locale);
                return text;
            }
            set
            {
                if (m_Empty)
                    return;

                m_String.Reload();
                m_String.Data.UpdateText(m_Locale, value);
                m_String.Save();

                m_TagsList = null;

                Glossary.Instance.AnalyzeLocaleEntryForTerms(this);
                OnPropertyChanged(nameof(Text));
                OnPropertyChanged(nameof(SymbolsCount));
                SymbolsCount = Text.Length;
                //Glossary.Instance.AnalyzeLocaleEntryForTerms(this);

                if (StringsFilter.Filter.Mode == FilterMode.Tags_Mismatch)
                {
                    // rerun comparison immediately if we're comparing tags

                    TagsList.Compare(m_String.SourceLocaleEntry.TagsList, m_String.TargetLocaleEntry.TagsList);
                    m_String.UpdateInlines();
                }
                else
                {
                    UpdateInlines();
                }

            }
        }

        private int _symbolsCount;
        public int SymbolsCount
        {
            get
            {
                int textForCount = TextWithoutTags(Array.Empty<string>()).Length;

                if (Text.Contains("|") && !Text.Contains("{Speaker}"))
                {
                    textForCount = StringUtils.CountTotalSymbols(TextWithoutTags(Array.Empty<string>()));
                }

                return textForCount;
            }
            set
            {
                if (_symbolsCount != value)
                {
                    _symbolsCount = value;
                    OnPropertyChanged(nameof(SymbolsCount));
                    OnPropertyChanged(nameof(Color));
                    UpdateInlines();
                }
            }
        }

        private string _symbolsBordersAndCount;

        public string SymbolsBordersAndCount
        {
            get => _symbolsBordersAndCount;
            set
            {
                _symbolsBordersAndCount = value;
                OnPropertyChanged(nameof(SymbolsBordersAndCount));
                OnPropertyChanged(nameof(Color));
                OnPropertyChanged(nameof(SymbolsCount));
            }
        }

        private Brush _color = Brushes.LightSlateGray;

        private Brush _textBlockColor = Brushes.White;

        public Brush TextBlockColor
        {
            get => _textBlockColor;
            set
            {
                _textBlockColor = value;
                OnPropertyChanged(nameof(TextBlockColor));
            }
        }

        public Brush Color
        {
            get { return _color; }
            set
            {
                _color = value;
                OnPropertyChanged(nameof(Color));

                if (value == Brushes.Red)
                {
                    TextBlockColor = value;
                }
                else
                {
                    TextBlockColor = Brushes.White;
                }
            }
        }


        public string TextWithoutTags(string[] tagsToRemove) => StringUtils.RemoveTags(m_String.Data.GetText(m_Locale), tagsToRemove);
        public string TextWithoutEncyclopediaTags => TextWithoutTags(LocalizationExporter.TagsEncyclopedia);

        public string CurrentlyVisibleText => StringsFilter.Filter.HideTags
            ? TextWithoutEncyclopediaTags
            : Text;

        public string TranslatorComment
        {
            get
            {
                if (m_Empty)
                    return "";

                return m_String.Data.GetLocale(Locale)?.TranslatedComment ?? "";
            }
            set
            {
                if (m_Empty)
                    return;

                m_String.Reload();
                var localeData = m_String.Data.EnsureLocale(Locale);
                localeData.TranslatedComment = value;

                m_String.Save();
            }
        }

        public string VoiceComment
        {
            get
            {
                if (m_Empty)
                    return "";
                return m_String.Data.GetLocale(Locale)?.VoiceComment ?? "";
            }
            set
            {
                if (m_Empty)
                    return;
                OnPropertyChanged(nameof(VoiceComment));

                if (m_String.Data.GetLocale(Locale)?.VoiceComment != value)
                {
                    if (m_String.Data.GetLocale(Locale)?.Text != "Dialog comment")
                    {
                        m_String.Reload();
                        var localeData = m_String.Data.EnsureLocale(Locale);
                        localeData.VoiceComment = value;
                        m_String.Save();
                    }
                    else
                    {
                        var localeData = m_String.Data.EnsureLocale(Locale);
                        UpdateVoiceComment(m_String.DialogsDataList.First(), value);
                        localeData.VoiceComment = value;
                    }
                }
            }
        }

        private void UpdateVoiceComment(DialogsData? dialogsData, string value)
        {
            if (dialogsData == null)
                return;

            dialogsData.Nodes
                .First(n => n.Kind == "root")
                .VOComment[Locale] = value;

            var bpPath = Path.Combine(AppConfig.Instance.AbsBlueprintsFolder,
                 Path.GetRelativePath(AppConfig.Instance.DialogsFolder, dialogsData.FileSource)).Replace(".json", ".jbp");

            bpPath = Path.Combine(Path.GetDirectoryName(bpPath), $"{dialogsData.Name}.jbp");

            if (File.Exists(bpPath))
            {
                try
                {
                    string bpContent = File.ReadAllText(bpPath);
                    var blueprintData = JsonSerializer.Deserialize<BlueprintDialogRoot>(bpContent, JsonSerializerHelpers.JsonSerializerOptions);

                    if (blueprintData != null)
                    {

                        if (blueprintData.Data.VoComments != null)
                        {
                            blueprintData.Data.VoComments[Locale] = value;
                        }
                        string updatedBpContent = JsonSerializer.Serialize(blueprintData, JsonSerializerHelpers.JsonSerializerOptions);
                        File.WriteAllText(bpPath, updatedBpContent);
                    }
                    else
                    {
                        MessageBox.Show($"Error: Could not deserialize blueprint: {bpPath}");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error updating blueprint {bpPath}: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show($"Warning: Blueprint file not found: {bpPath} for dialog: {dialogsData.FileSource}");
            }

            if (!string.IsNullOrEmpty(dialogsData.FileSource))
            {
                string jsonString = JsonSerializer.Serialize(dialogsData, JsonSerializerHelpers.JsonSerializerOptions);
                File.WriteAllText(dialogsData.FileSource, jsonString);
            }
        }

        [NotNull]
        public InlinesWrapper Inlines { get; private set; } = new InlinesWrapper();


        public bool TryGetInlinesCollection(InlineCollectionType type, out InlinesWrapper collection) =>
            m_InlinesCollection.TryGetValue(type, out collection);

        private Dictionary<InlineCollectionType, InlinesWrapper> m_InlinesCollection = new();

        public TagsList TagsList => m_TagsList ??= TagsList.Parse(Text);

        public string MismatchedTraitString = "";

        public bool HasSpellingErrors
            => Translation == null && Inlines.HasAny;

        public bool IsTranslationSourceUpdated
        {
            get
            {
                if (Translation == null)
                    return false;
                var localeData = m_String.Data.GetLocale(Translation.m_Locale);
                if (localeData == null)
                    return false;
                return localeData.OriginalText.Replace("\r", "").Replace("\n", "").Trim() != Text.Replace("\r", "").Replace("\n", "").Trim();
            }
        }

        public Locale Locale => m_Locale;

        public LocaleEntry(StringEntry stringEntry, Locale locale)
        {
            m_String = stringEntry;
            m_Locale = locale;
            UpdateInlines();
        }


        public Brush AudioExportedBackground
        {
            get
            {
                var sourceLocale = m_String.SourceLocaleEntry.Locale;
                var targetLocale = m_String.TargetLocaleEntry.Locale;
                var localeData = m_String.Data.GetLocale(targetLocale);

                if (localeData != null
                    && sourceLocale == Locale.DefaultFrom
                    && targetLocale == Locale.DefaultTo
                    && m_String.Data.GetLocale(targetLocale).Traits.Any(a => a.Trait == LocaleTrait.AudioExported.ToString()))
                {
                    return new SolidColorBrush(System.Windows.Media.Color.FromArgb(200, 220, 220, 220));
                }


                return Brushes.Transparent;
            }
        }

        public LocaleEntry(LocaleEntry translation)
        {
            m_String = translation.m_String;
            Translation = translation;

            var translationLocale = m_String.Data.GetLocale(translation.m_Locale);
            if (translationLocale?.TranslatedFrom == null)
            {
                m_Empty = true;
                return;
            }

            m_Locale = translationLocale.TranslatedFrom;
            UpdateInlines();
        }

        private LocaleEntry()
        {
            m_Empty = true;
        }

        public void UpdateInlines()
        {
            Dictionary<InlineCollectionType, InlinesWrapper> inlinesCollection = new();

            inlinesCollection.Add(InlineCollectionType.Default, new InlinesWrapper());
            Inlines = inlinesCollection[InlineCollectionType.Default];

            if (m_Empty)
                return;

            var targetText = StringsFilter.Filter.HideTags ? TextWithoutEncyclopediaTags : Text;

            if (string.IsNullOrEmpty(targetText))
                return;

            ApplyMode(targetText, inlinesCollection);
            ApplyGlossary(targetText, inlinesCollection);
            ApplyMaxSymbols(inlinesCollection);
            ApplyFilter(targetText, inlinesCollection);
        }

        public void ApplyMode(string targetText, Dictionary<InlineCollectionType, InlinesWrapper> inlinesCollection)
        {
            if (StringsFilter.Filter.Mode == FilterMode.Updated_Trait && !string.IsNullOrEmpty(MismatchedTraitString))
            {
                inlinesCollection[InlineCollectionType.DiffTrait] = Diff.MakeInlines(MismatchedTraitString, targetText);
                Inlines = inlinesCollection[InlineCollectionType.DiffTrait];
            }
            else if (StringsFilter.Filter.Mode == FilterMode.Tags_Mismatch)
            {
                inlinesCollection[InlineCollectionType.TagsMismatch] = TagsList.MakeInlines(targetText);
                Inlines = inlinesCollection[InlineCollectionType.TagsMismatch];
            }
            else
            {
                if (Translation == null)
                {
                    inlinesCollection[InlineCollectionType.SpellCheck] = SpellCheck.MakeInlines(m_Locale, targetText);
                    Inlines = inlinesCollection[InlineCollectionType.SpellCheck];
                }
                else
                {
                    var localeData = m_String.Data.GetLocale(Translation.m_Locale);
                    if (localeData == null)
                    {
                        inlinesCollection[InlineCollectionType.Default] = new InlinesWrapper(targetText);
                        Inlines = inlinesCollection[InlineCollectionType.Default];
                    }
                    else
                    {
                        inlinesCollection[InlineCollectionType.DiffSource] = Diff.MakeInlines(localeData.OriginalText, Text);
                        if (StringsFilter.Filter.HideTags)
                        {
                            string originalText = StringsFilter.Filter.HideTags ?
                                StringUtils.RemoveTags(localeData.OriginalText, LocalizationExporter.TagsEncyclopedia) :
                                localeData.OriginalText;

                            inlinesCollection[InlineCollectionType.DiffSourceNoTags] = Diff.MakeInlines(originalText, targetText);
                        }

                        Inlines = StringsFilter.Filter.HideTags
                            ? inlinesCollection[InlineCollectionType.DiffSourceNoTags]
                            : inlinesCollection[InlineCollectionType.DiffSource];
                    }
                }
            }
        }

        public void ApplyGlossary(string targetText, Dictionary<InlineCollectionType, InlinesWrapper> inlinesCollection)
        {
            if (string.IsNullOrEmpty(targetText))
                return;

            try
            {
                inlinesCollection[InlineCollectionType.Glossary] = Glossary.Instance.MakeInlines(targetText, this);
                Inlines = Inlines.MergeWith(inlinesCollection[InlineCollectionType.Glossary]);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void ApplyMaxSymbols(Dictionary<InlineCollectionType, InlinesWrapper> inlinesCollection)
        {
            if (!AppConfig.Instance.HighlightMaxSymbols)
            {
                return;
            }
            if (m_String.Data.Kind == StringKind.DialogAnswer)
            {
                if (SymbolsCount > AppConfig.Instance.SymbolsBorders.ShortAnswer)
                {
                    inlinesCollection[InlineCollectionType.MaxLength] = MaxLength.MakeInlines(Inlines.ToString(), AppConfig.Instance.SymbolsBorders.ShortAnswer);
                    Inlines = Inlines.MergeWith(inlinesCollection[InlineCollectionType.MaxLength]);
                    Color = Brushes.Red;
                    if (!m_String.Data.GetLocale(Locale).Traits.Contains(extraSymbols))
                        m_String.Data.GetLocale(Locale).AddTraitInternal(extraSymbols);
                }
                else
                {
                    Color = Brushes.LightSlateGray;
                    if (!m_String.Data.GetLocale(Locale).Traits.Contains(extraSymbols))
                        m_String.Data.GetLocale(Locale).RemoveTraitInternal("ExtraSymbols");
                }

                SymbolsBordersAndCount = $"{SymbolsCount}/{AppConfig.Instance.SymbolsBorders.ShortAnswer}";

            }
            else if (m_String.Data.Kind == StringKind.DialogCue)
            {
                if (m_String.Data.GetLocale(Locale).Locale.ToString() == "en")
                {
                    if (SymbolsCount > AppConfig.Instance.SymbolsBorders.En)
                    {
                        inlinesCollection[InlineCollectionType.MaxLength] = MaxLength.MakeInlines(Inlines.ToString(), AppConfig.Instance.SymbolsBorders.En);
                        Inlines = Inlines.MergeWith(inlinesCollection[InlineCollectionType.MaxLength]);
                        Color = Brushes.Red;
                        if (!m_String.Data.GetLocale(Locale).Traits.Contains(extraSymbols))
                            m_String.Data.GetLocale(Locale).AddTraitInternal(extraSymbols);
                    }

                    SymbolsBordersAndCount = $"{SymbolsCount}/{AppConfig.Instance.SymbolsBorders.En}";
                }
                else if (SymbolsCount > AppConfig.Instance.SymbolsBorders.Common)
                {
                    inlinesCollection[InlineCollectionType.MaxLength] = MaxLength.MakeInlines(Inlines.ToString(), AppConfig.Instance.SymbolsBorders.Common);
                    Inlines = Inlines.MergeWith(inlinesCollection[InlineCollectionType.MaxLength]);
                    Color = Brushes.Red;
                    SymbolsBordersAndCount = $"{SymbolsCount}/{AppConfig.Instance.SymbolsBorders.Common}";
                    if (!m_String.Data.GetLocale(Locale).Traits.Contains(extraSymbols))
                        m_String.Data.GetLocale(Locale).AddTraitInternal(extraSymbols);

                }
                else
                {
                    Color = Brushes.LightSlateGray;
                    SymbolsBordersAndCount = $"{SymbolsCount}/{AppConfig.Instance.SymbolsBorders.Common}";

                    if (m_String.Data.GetLocale(Locale).Traits.Contains(extraSymbols))
                        m_String.Data.GetLocale(Locale).RemoveTraitInternal("ExtraSymbols");
                }
            }
            else
            {
                Color = Brushes.LightSlateGray;
                SymbolsBordersAndCount = $"{SymbolsCount}";

            }
        }

        public void ApplyFilter(string targetText, Dictionary<InlineCollectionType, InlinesWrapper> inlinesCollection)
        {
            if (!string.IsNullOrEmpty(targetText))
            {
                inlinesCollection[InlineCollectionType.Filter] = FilterFits.MakeInlines(Inlines.ToString(), StringsFilter.Filter.SelectedColor);
                Inlines = Inlines.MergeWith(inlinesCollection[InlineCollectionType.Filter]);
            }
        }
    }
}