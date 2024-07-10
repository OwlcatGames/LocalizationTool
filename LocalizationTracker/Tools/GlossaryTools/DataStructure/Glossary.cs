using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using common4gp;
using Kingmaker.Localization.Shared;
using LocalizationTracker.Components;
using LocalizationTracker.Data;
using LocalizationTracker.Logic;
using LocalizationTracker.Utility;
using LocalizationTracker.Windows;

namespace LocalizationTracker.Tools.GlossaryTools;

public class Glossary
{
    private ConcurrentDictionary<string, ConcurrentDictionary<Locale, List<TermEntry>>> m_TermsEntryMap = new();
    private ConcurrentDictionary<string, ConcurrentDictionary<Locale, List<TermEntry>>> m_TermsEntryNoTagsMap = new();
    
    private Dictionary<string, GlossarySheet.Term> m_Glossary = new();
    private ConcurrentDictionary<string, HashSet<Locale>> m_ProcessedStrings = new();
    
    private TermTemplateCollection m_TermTemplateCollection = new();
    
    private SheetImporter m_Importer = new();

    private const char TERMS_SEPARATOR = '|';
    private const char TERM_PARTS_SEPARATOR = '*';
    
    public static void SetupInstance()
    {
        Instance = new Glossary();
        if (!AppConfig.Instance.Glossary.GlossaryIsEnabled)
            return;

        Instance.ReadSheetFromJson();
    }
    
    public void ReadSheetFromJson() => m_Importer.ReadSheetFromJson();

    public void UpdateGlossary()
    {
        if (!AppConfig.Instance.Glossary.GlossaryIsEnabled)
            return;
        
        m_Importer.ReadSheetFromGoogle();
    }
    
    public static Glossary Instance { get; private set; }

    public event Action GlossaryUpdatedEvent;

    public void Initialize(GlossarySheet sheet, bool updateRequired)
    {
        m_Glossary.Clear();
        foreach (var term in sheet)
        {
            m_Glossary[term.Id] = term;
        }
        
        RecalculateTerms();
        if (updateRequired)
            GlossaryUpdatedEvent?.Invoke();
    }

    public void RecalculateTerms()
    {
        m_ProcessedStrings.Clear();
        m_TermTemplateCollection = new();
        m_TermsEntryMap.Clear();
        m_TermsEntryNoTagsMap.Clear();
        PrepareTermTemplates();
    }

    private void PrepareTermTemplates()
    {
        foreach (var term in m_Glossary.Values)
        {
            foreach (var locale in Locale.Values)
            {
                if (!term.TryGetTranslation(locale, out var translation))
                    continue;

                var splittedTerms = new List<TermTemplate.Splitted>();
                
                var terms = translation.Split(TERMS_SEPARATOR, StringSplitOptions.TrimEntries);
                foreach (string s in terms)
                {
                    var splittedTerm = new TermTemplate.Splitted();
                    var split = s.Split(TERM_PARTS_SEPARATOR, StringSplitOptions.TrimEntries).ToList();
                    if (string.IsNullOrEmpty(split.Last()))
                    {
                        splittedTerm.EndsWithAsterisk = true;
                        split.Remove(split.Last());
                    }
                    
                    splittedTerm.Pieces =
                        new List<string>(split);
                    
                    splittedTerms.Add(splittedTerm);
                }
                
                var newTermTemplate = new TermTemplate(term.Id, splittedTerms);
                m_TermTemplateCollection.AddTermTemplate(term.Id, locale, newTermTemplate);
            }
        }
    }

    private InlineTemplate MakeInline(string text, bool mismatched = false, bool caseError = false)
    {
        var inline = new InlineTemplate(text);
        inline.Underline = true;
        inline.InlineType = InlineType.GlossaryTerm;
        
        if (mismatched && StringManager.Filter.Mode == FilterMode.Glossary_Mismatch)
            inline.FontWeight = FontWeights.Bold;

        inline.Foreground = caseError ? Colors.Red : Colors.Black;
        return inline;
    }

    public InlinesWrapper MakeInlines(string text, LocaleEntry localeEntry)
    {
        if (!m_ProcessedStrings.TryGetValue(localeEntry.StringKey, out var locales) || 
            !locales.Contains(localeEntry.Locale))
            AnalyzeLocaleEntryForTerms(localeEntry);
        
        var result = new List<InlineTemplate>();
        var textEnd = text.Length;
        int cursor = 0;
        var targetEntriesCollection = StringManager.Filter.HideTags ? m_TermsEntryNoTagsMap : m_TermsEntryMap;
        if (!targetEntriesCollection.ContainsKey(localeEntry.StringKey) ||
            !targetEntriesCollection[localeEntry.StringKey].ContainsKey(localeEntry.Locale))
            return new InlinesWrapper();

        targetEntriesCollection[localeEntry.StringKey][localeEntry.Locale].Sort(delegate(TermEntry s1, TermEntry s2)
            {
                if (s1.StartIndex < s2.StartIndex) 
                    return -1;

                return 1;
            });
        
        foreach (var termEntry in targetEntriesCollection[localeEntry.StringKey][localeEntry.Locale])
        {
            string substring;
            if (termEntry.StartIndex > cursor && text.TryGetSubstring(cursor, termEntry.StartIndex - cursor, out var s1))
            {
                result.Add(new InlineTemplate(s1));
            }

            if (termEntry.EndIndex <= cursor ||
                termEntry.StartIndex <= cursor &&
                termEntry.EndIndex > cursor)
                continue;

            var startIndex = termEntry.StartIndex;
            var length = termEntry.EndIndex - termEntry.StartIndex;
            if (!text.TryGetSubstring(startIndex, length, out substring))
                continue;

            bool mismatch = localeEntry.TryGetPairedLocale(out var pairedLocale) &&
                            !TermHasSiblingInOtherLocale(termEntry.TermId, localeEntry.StringKey, pairedLocale);
            
            result.Add(MakeInline(substring, mismatch, termEntry.CaseError));
            cursor = termEntry.EndIndex;
        }

        if (cursor < textEnd && text.TryGetSubstring(cursor, textEnd - cursor, out var subs) )
        {
            result.Add(new InlineTemplate(subs));    
        }
        
        return new InlinesWrapper(result.ToArray());
    }
    
    public void AnalyzeLocaleEntryForTerms(LocaleEntry localeEntry)
    {
        if (!m_TermsEntryMap.TryGetValue(localeEntry.StringKey, out var localesMap))
        {
            localesMap = new ConcurrentDictionary<Locale, List<TermEntry>>();
            m_TermsEntryMap[localeEntry.StringKey] = localesMap;
        }
        
        localesMap[localeEntry.Locale] = new List<TermEntry>();
        
        if (!m_TermsEntryNoTagsMap.TryGetValue(localeEntry.StringKey, out var localesHiddenTagsMap))
        {
            localesHiddenTagsMap = new ConcurrentDictionary<Locale, List<TermEntry>>();
            m_TermsEntryNoTagsMap[localeEntry.StringKey] = localesHiddenTagsMap;
        }
        
        localesHiddenTagsMap[localeEntry.Locale] = new List<TermEntry>();
        
        var regularText = localeEntry.Text;
        var textWithoutEncyclopediaTags = localeEntry.TextWithoutEncyclopediaTags;
        
        foreach (var termId in m_Glossary.Keys){
            if (!m_Glossary.TryGetValue(termId, out var term))
                continue;
            
            if (!m_TermTemplateCollection.TryGetTermTemplates(termId, localeEntry.Locale, out var templates))
                continue;

            foreach (TermTemplate termTemplate in templates)
            {
                termTemplate.TryFindTermEntries(regularText, out var entries);
                foreach (var entry in entries)
                {
                    AddTermEntry(localeEntry, term, termId, entry, localesMap);
                }
                
                termTemplate.TryFindTermEntries(textWithoutEncyclopediaTags, out var entriesNoTags);
                foreach (var entry in entriesNoTags)
                {
                    AddTermEntry(localeEntry, term, termId, entry, localesHiddenTagsMap);
                }
            }
        }

        localesMap[localeEntry.Locale] = FilterInnerTerms(localesMap[localeEntry.Locale]);
        localesMap[localeEntry.Locale] = localesMap[localeEntry.Locale].OrderBy(l => l.StartIndex).ToList();

        if (!m_ProcessedStrings.ContainsKey(localeEntry.StringKey))
            m_ProcessedStrings[localeEntry.StringKey] = new HashSet<Locale>();
        
        m_ProcessedStrings[localeEntry.StringKey].Add(localeEntry.Locale);
    }

    private List<TermEntry> FilterInnerTerms(List<TermEntry> TermEntries)
    {
        var result = new List<TermEntry>();
        var tempList = new List<TermEntry>(TermEntries).OrderBy(s => s.Length).ToList();

        for (int i = 0; i < tempList.Count; i++)
        {
            var elementToCompare = tempList[i];
            bool innerTerm = false;
            for (int j = i + 1; j < tempList.Count; j++)
            {
                var secondElement = tempList[j];
                if (elementToCompare.StartIndex >= secondElement.StartIndex &&
                    elementToCompare.EndIndex <= secondElement.EndIndex)
                {
                    innerTerm = true;
                    break;
                }
            }
            if (!innerTerm)
                result.Add(elementToCompare);
        }
        
        return result;
    }

    private static void AddTermEntry(
        LocaleEntry localeEntry, 
        GlossarySheet.Term term, 
        string termId, 
        (int startIndex, int endIndex, bool caseError) entry,
        ConcurrentDictionary<Locale, List<TermEntry>> localesMap)
    {
        var termEntry = new TermEntry
        {
            StringKey = localeEntry.StringKey,
            Comment = term.Comment,
            TermId = termId,
            StartIndex = entry.startIndex,
            EndIndex= entry.endIndex,
            CaseError = entry.caseError,
        };

        if (!localesMap.TryGetValue(localeEntry.Locale, out var termsList))
        {
            termsList = new List<TermEntry> { termEntry };
            localesMap[localeEntry.Locale] = termsList;
        }

       /* foreach (var t in termsList)
        {
            if (t.StartIndex <= termEntry.StartIndex &&
                t.EndIndex >= termEntry.StartIndex)
                return;
        }*/
        
        termsList.Add(termEntry);
    }

    public bool TryGetTermsInStringEntry(StringEntry stringEntry, out List<TermEntry> termEntries)
    {
        termEntries = new List<TermEntry>();
        var sourceEntries = new List<TermEntry>();
        if (stringEntry.SourceLocaleEntry.Locale != null && m_TermsEntryMap.TryGetValue(stringEntry.Key, out var sourceLocalesMap))
            sourceLocalesMap.TryGetValue(stringEntry.SourceLocaleEntry.Locale, out sourceEntries);
        
        var targetEntries = new List<TermEntry>();
        if (stringEntry.TargetLocaleEntry.Locale != null && m_TermsEntryMap.TryGetValue(stringEntry.Key, out var targetLocalesMap))
               targetLocalesMap.TryGetValue(stringEntry.TargetLocaleEntry.Locale, out targetEntries);

        if (sourceEntries != null && targetEntries != null)
            termEntries = sourceEntries.Union(targetEntries).ToList();
        
        if (!sourceEntries.IsNullOrEmpty())
            termEntries.AddRange(sourceEntries);
        
        if (!targetEntries.IsNullOrEmpty())
            termEntries.AddRange(targetEntries);

        termEntries = termEntries.DistinctBy(s => s.TermId).ToList();
        return termEntries.Any();
    }

    public List<TermEntry> FilterDuplicates(List<TermEntry> termEntries)
    {
        var result = new List<TermEntry>();
        var existingTerms = new HashSet<string>();
        foreach (TermEntry entry in termEntries)
        {
            if (existingTerms.Contains(entry.TermId))
                continue;

            existingTerms.Add(entry.TermId);
            result.Add(entry);
        }

        return result;
    }

    public string GetTermLocale(string termId, StringEntry stringEntry)
    {
        if (!m_Glossary.TryGetValue(termId, out var term))
            return $"Glossary error, no term {termId}";

        return $"{term.GetExampleTranslation(stringEntry.SourceLocaleEntry?.Locale)} / {term.GetExampleTranslation(stringEntry.TargetLocaleEntry?.Locale)}";
    }

    public bool StringEntryHasTermsMismatch(StringEntry stringEntry)
    {
        if (!m_TermsEntryMap.TryGetValue(stringEntry.Key, out var termsMap))
            return false;

        var sourceLocTerms = new List<TermEntry>();
        if (stringEntry.SourceLocaleEntry.Locale != null)
            termsMap.TryGetValue(stringEntry.SourceLocaleEntry.Locale, out sourceLocTerms);
        
        var targetLocTerms = new List<TermEntry>();
        if (stringEntry.TargetLocaleEntry.Locale != null)
            termsMap.TryGetValue(stringEntry.TargetLocaleEntry.Locale, out targetLocTerms);
        
        if (sourceLocTerms.IsNullOrEmpty() && targetLocTerms.IsNullOrEmpty())
            return false;
            
        if (sourceLocTerms.IsNullOrEmpty() && !targetLocTerms.IsNullOrEmpty() ||
            !sourceLocTerms.IsNullOrEmpty() && targetLocTerms.IsNullOrEmpty())
            return true;

        var sourceTermsIds = sourceLocTerms!.Select(x => x.TermId).ToArray();
        var targetTermsIds = targetLocTerms!.Select(x => x.TermId).ToArray();
        
        var intersect = sourceTermsIds.Except(targetTermsIds).Union( targetTermsIds.Except(sourceTermsIds));
        return intersect.Any();
    }

    private bool TermHasSiblingInOtherLocale(string termId, string stringEntryId, Locale otherLocale)
    {
        if (!m_TermsEntryMap.TryGetValue(stringEntryId, out var termsMap))
            return false;

        if (!termsMap.TryGetValue(otherLocale, out var sourceLocTerms))
            return false;
        
        if (sourceLocTerms.Find(x=>x.TermId == termId) == null)
            return false;

        return true;
    }
}   