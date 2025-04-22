using System.Collections.Generic;

namespace LocalizationTracker.Tools.GlossaryTools;

public class TermTemplateCollection
{
    private Dictionary<string, Dictionary<Locale, List<TermTemplate>>> m_IdToLocaleToTemplateMap = new();

    public void AddTermTemplate(string termId, Locale locale, TermTemplate template)
    {
        if (!m_IdToLocaleToTemplateMap.ContainsKey(termId))
            m_IdToLocaleToTemplateMap[termId] = new Dictionary<Locale, List<TermTemplate>>();
        
        if (!m_IdToLocaleToTemplateMap[termId].ContainsKey(locale))
            m_IdToLocaleToTemplateMap[termId][locale] = new (){};
        
        m_IdToLocaleToTemplateMap[termId][locale].Add(template);
    }

    public bool TryGetTermTemplates(string termId, Locale locale, out List<TermTemplate> template)
    {
        template = new List<TermTemplate>();
        return m_IdToLocaleToTemplateMap.TryGetValue(termId, out var locales) &&
               locales.TryGetValue(locale, out template);
    }
}