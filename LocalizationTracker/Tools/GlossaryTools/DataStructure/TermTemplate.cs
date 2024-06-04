using System;
using System.Collections.Generic;
using System.Linq;

namespace LocalizationTracker.Tools.GlossaryTools;

/// <summary>
/// Processed table term definition for easy lookup
/// </summary>
public class TermTemplate
{
    private const int MAX_SYMBOLS_IN_ASTERISK = 5;
    
    public class Splitted
    {
        public List<string> Pieces = new();
        public bool EndsWithAsterisk;
    }
    
    public string TermId;
    public List<Splitted> SplittedTerms = new();

    public TermTemplate(string termId, List<Splitted> splittedTerms)
    {
        TermId = termId;
        SplittedTerms = splittedTerms;
    }
    
    public bool TryFindTermEntries(string text, out List<(int startIndex, int endIndex, bool caseError)> entries)
    {
        entries = new List<(int startIndex, int endIndex, bool caseError)>();
        foreach (Splitted splittedTerm in SplittedTerms)
        {
            var termParts = splittedTerm.Pieces;
            var cursor = 0;
            var initialText = text;
            bool needToSearchFurther;
            do
            {
                var searchResult = TryFindTermEntry(
                    text,
                    termParts,
                    splittedTerm.EndsWithAsterisk,
                    cursor,
                    out var entry);
                
                if (searchResult == TermSearchResult.FoundFull)
                    entries.Add(entry);
                
                cursor = entry.endIndex;
                text = initialText.Substring(entry.endIndex);
                needToSearchFurther = 
                    searchResult is TermSearchResult.FoundFull or TermSearchResult.FoundPart;
            } while (needToSearchFurther);
        }

        return entries.Count > 0;
    }
    
    private TermSearchResult TryFindTermEntry(
        string text, List<string> termParts, bool endsWithAsterisk, int offset,
        out (int startIndex, int endIndex, bool caseError) entry)
    {
        entry = (0, 0, false);
        if (!TryFindTermPart(text, termParts[0], 0, true, out var termPartEntry))
            return TermSearchResult.NotFound;

        entry.caseError |= termPartEntry.caseError;
        var previousIndex = termPartEntry.index;
        entry.endIndex = previousIndex + termParts[0].Length + offset;
        if (previousIndex != 0 &&
            !SpellCheck.WordTeminations.Contains(text[previousIndex - 1]))
            return TermSearchResult.FoundPart;
        
        entry.startIndex = previousIndex + offset;
        for (int i = 1; i < termParts.Count; i++)
        {
            string termPart = termParts[i];
            if (string.IsNullOrEmpty(termPart))
                continue;
            
            if (!TryFindTermPart(text, termParts[i], previousIndex, false, out termPartEntry))
                return TermSearchResult.FoundPart;
            
            entry.endIndex = termPartEntry.index + termParts[i].Length + offset;
            if ((termPartEntry.index - previousIndex - termParts[i - 1].Length) > MAX_SYMBOLS_IN_ASTERISK)
                return TermSearchResult.FoundPart;

            entry.caseError |= termPartEntry.caseError;
            previousIndex = termPartEntry.index;
        }

        int additionalSymbols = 0;
        if (endsWithAsterisk)
            for (int i = 0; i < MAX_SYMBOLS_IN_ASTERISK; i++)
            {
                int charIndex = previousIndex + termParts.Last().Length + i;
                if (text.Length <= charIndex)
                    break;
                
                if (!SpellCheck.WordTeminations.Contains(text.ElementAt(charIndex)))
                {
                    additionalSymbols++;
                }
                else
                {
                    break;
                }
            }

        var endIndex = previousIndex + termParts.Last().Length + additionalSymbols;
        entry.endIndex = endIndex + offset;
        if (endIndex != text.Length &&
            !SpellCheck.WordTeminations.Contains(text[endIndex]))
            return TermSearchResult.FoundPart;

        return TermSearchResult.FoundFull;
    }

    private bool TryFindTermPart(string text, string termPart, int searchStartIndex, bool isFirstPart, out (int index, bool caseError) termPartEntry)
    {
        termPartEntry = (0, false);
        var index = text.ToLower().IndexOf(termPart.ToLower(), searchStartIndex, StringComparison.Ordinal);
        if (index == -1)
            return false;

        if (isFirstPart)
        {
            var trueCaseIndex = text.IndexOf(termPart[1..], searchStartIndex, StringComparison.Ordinal);
            int startOfPrefix = Math.Max(0, trueCaseIndex - 3);
            string? previousString = trueCaseIndex > 1 ? text.Substring(startOfPrefix, trueCaseIndex - startOfPrefix) : null;
            if (trueCaseIndex == -1 || 
                trueCaseIndex > 1 && !SpellCheck.IsTermCaseCorrect(text[trueCaseIndex - 1], termPart[0], previousString))
                termPartEntry.caseError = true;
        }
        else
        {
            var trueCaseIndex = text.IndexOf(termPart, searchStartIndex, StringComparison.Ordinal);
            if (trueCaseIndex == -1)
                termPartEntry.caseError = true;
        }
        
        termPartEntry.index = index;
        return true;
    }
}

public enum TermSearchResult
{
    NotFound,
    FoundPart,
    FoundFull,
}