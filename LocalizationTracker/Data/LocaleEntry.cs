using JetBrains.Annotations;
using Kingmaker.Localization.Shared;
using LocalizationTracker.Components;
using LocalizationTracker.Data.Wrappers;
using LocalizationTracker.Logic;
using LocalizationTracker.Tools;
using LocalizationTracker.Utility;
using LocalizationTracker.Windows;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;
using System.Windows.Media;
using LocalizationTracker.Tools.GlossaryTools;
using System.ComponentModel;
using DocumentFormat.OpenXml.Drawing;
using static LocalizationTracker.Data.Unreal.UnrealStringData;
using System.Reflection.Metadata;
using LocalizationTracker.Data.Unreal;

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

                return m_String.Data.GetText(m_Locale);
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

                if (StringManager.Filter.Mode == FilterMode.Tags_Mismatch)
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
            get { return Text.Length; }
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

        public string CurrentlyVisibleText => StringManager.Filter.HideTags
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
                return localeData.OriginalText != Text;
            }
        }

        public Locale Locale => m_Locale;

        public LocaleEntry(StringEntry stringEntry, Locale locale)
        {
            m_String = stringEntry;
            m_Locale = locale;
            UpdateInlines();
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
            m_InlinesCollection.Clear();

            if (m_Empty)
                return;

            var targetText = StringManager.Filter.HideTags ? TextWithoutEncyclopediaTags : Text;

            if (string.IsNullOrEmpty(targetText))
                return;

            ApplyMode(targetText);
            ApplyGlossary(targetText);
            ApplyMaxSymbols();
            ApplyFilter(targetText);
        }

        public void ApplyMode(string targetText)
        {
            if (StringManager.Filter.Mode == FilterMode.Updated_Trait && !string.IsNullOrEmpty(MismatchedTraitString))
            {
                m_InlinesCollection[InlineCollectionType.DiffTrait] = Diff.MakeInlines(MismatchedTraitString, targetText);
                Inlines = m_InlinesCollection[InlineCollectionType.DiffTrait];
            }
            else if (StringManager.Filter.Mode == FilterMode.Tags_Mismatch)
            {
                m_InlinesCollection[InlineCollectionType.TagsMismatch] = TagsList.MakeInlines(targetText);
                Inlines = m_InlinesCollection[InlineCollectionType.TagsMismatch];
            }
            else
            {
                if (Translation == null)
                {
                    m_InlinesCollection[InlineCollectionType.SpellCheck] = SpellCheck.MakeInlines(m_Locale, targetText);
                    Inlines = m_InlinesCollection[InlineCollectionType.SpellCheck];
                }
                else
                {
                    var localeData = m_String.Data.GetLocale(Translation.m_Locale);
                    if (localeData == null)
                    {
                        m_InlinesCollection[InlineCollectionType.Default] = new InlinesWrapper(targetText);
                        Inlines = m_InlinesCollection[InlineCollectionType.Default];
                    }
                    else
                    {
                        m_InlinesCollection[InlineCollectionType.DiffSource] = Diff.MakeInlines(localeData.OriginalText, Text);
                        if (StringManager.Filter.HideTags)
                        {
                            string originalText = StringManager.Filter.HideTags ?
                                StringUtils.RemoveTags(localeData.OriginalText, LocalizationExporter.TagsEncyclopedia) :
                                localeData.OriginalText;

                            m_InlinesCollection[InlineCollectionType.DiffSourceNoTags] = Diff.MakeInlines(originalText, targetText);
                        }

                        Inlines = StringManager.Filter.HideTags
                            ? m_InlinesCollection[InlineCollectionType.DiffSourceNoTags]
                            : m_InlinesCollection[InlineCollectionType.DiffSource];
                    }
                }
            }
        }

        public void ApplyGlossary(string targetText)
        {
            if (string.IsNullOrEmpty(targetText))
                return;
            
            m_InlinesCollection[InlineCollectionType.Glossary] = Glossary.Instance.MakeInlines(targetText, this);
            Inlines = Inlines.MergeWith(m_InlinesCollection[InlineCollectionType.Glossary]);
        }

        public void ApplyMaxSymbols()
        {
            if (m_String.Data.Kind == StringKind.DialogAnswer)
            {
                if (SymbolsCount > AppConfig.Instance.SymbolsBorders.ShortAnswer)
                {
                    m_InlinesCollection[InlineCollectionType.MaxLength] = MaxLength.MakeInlines(Inlines.ToString(), AppConfig.Instance.SymbolsBorders.ShortAnswer);
                    Inlines = Inlines.MergeWith(m_InlinesCollection[InlineCollectionType.MaxLength]);
                    Color = Brushes.Red;
                    if (!m_String.Data.GetLocale(Locale).Traits.Contains(extraSymbols))
                        m_String.Data.GetLocale(Locale).AddTraitInternal(extraSymbols);
                }
                else
                {
                    Color = Brushes.LightSlateGray;
                    if(!m_String.Data.GetLocale(Locale).Traits.Contains(extraSymbols))
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
                        m_InlinesCollection[InlineCollectionType.MaxLength] = MaxLength.MakeInlines(Inlines.ToString(), AppConfig.Instance.SymbolsBorders.En);
                        Inlines = Inlines.MergeWith(m_InlinesCollection[InlineCollectionType.MaxLength]);
                        Color = Brushes.Red;
                        if (!m_String.Data.GetLocale(Locale).Traits.Contains(extraSymbols))
                            m_String.Data.GetLocale(Locale).AddTraitInternal(extraSymbols);
                    }

                    SymbolsBordersAndCount = $"{SymbolsCount}/{AppConfig.Instance.SymbolsBorders.En}";
                }
                else if (SymbolsCount > AppConfig.Instance.SymbolsBorders.Common)
                {
                    m_InlinesCollection[InlineCollectionType.MaxLength] = MaxLength.MakeInlines(Inlines.ToString(), AppConfig.Instance.SymbolsBorders.Common);
                    Inlines = Inlines.MergeWith(m_InlinesCollection[InlineCollectionType.MaxLength]);
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

        public void ApplyFilter(string targetText)
        {
            if (!string.IsNullOrEmpty(targetText))
            {
                m_InlinesCollection[InlineCollectionType.Filter] = FilterFits.MakeInlines(Inlines.ToString(), StringManager.Filter.SelectedColor);
                Inlines = Inlines.MergeWith(m_InlinesCollection[InlineCollectionType.Filter]);
            }
        }
    }
}