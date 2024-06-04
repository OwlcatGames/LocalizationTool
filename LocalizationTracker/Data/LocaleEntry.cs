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

namespace LocalizationTracker.Data
{
    public class LocaleEntry
    {
        public static readonly LocaleEntry Empty = new LocaleEntry();

        [NotNull]
        private readonly StringEntry m_String;

        public string StringKey => m_String.Key;

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
            
            m_InlinesCollection[InlineCollectionType.Glossary] = Glossary.Instance.MakeInlines(targetText, this);
            Inlines = Inlines.MergeWith(m_InlinesCollection[InlineCollectionType.Glossary]);
            
            if (!string.IsNullOrEmpty(targetText))
            {
                m_InlinesCollection[InlineCollectionType.Filter] = FilterFits.MakeInlines(Inlines.ToString(), StringManager.Filter.SelectedColor);
                Inlines = Inlines.MergeWith(m_InlinesCollection[InlineCollectionType.Filter]);
            }
        }
    }
}