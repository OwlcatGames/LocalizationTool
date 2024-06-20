using System.Collections.Generic;
using Cathei.BakingSheet;
using Kingmaker.Localization.Shared;

namespace LocalizationTracker.Tools.GlossaryTools;

public class GlossarySheet : Sheet<GlossarySheet.Term>
{
    /// <summary>
    /// Table markdown
    /// </summary>
    public class Term : SheetRow
    {
        public string ruRU { get; private set; }
        public string Example_ruRU { get; private set; }
        public string enGB { get; private set; }
        public string Example_enGB { get; private set; }

        public Dictionary<string, string> Example { get; private set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Exact { get; private set; } = new Dictionary<string, string>();
        public string Comment { get; private set; }
        public TermSourceType TermSource { get; private set; }
        public TermType TermType { get; private set; }
        public Category Category { get; private set; }


        public bool TryGetTranslation(Locale locale, out string translation)
        {
            translation = string.Empty;
            if (!Exact.TryGetValue(locale, out translation))
            {
                if (locale == Locale.enGB)
                    translation = enGB;
                if (locale == Locale.ruRU)
                    translation = ruRU;
            }

            return !string.IsNullOrEmpty(translation);
        }
        
        public string GetExampleTranslation(Locale? locale)
        {
            var translation = string.Empty;
            if (locale == null)
                return Example_enGB;
            
            if (Exact.TryGetValue(locale, out translation))
                return translation;
            
            if (locale == Locale.enGB)
                    return Example_enGB;
            if (locale == Locale.ruRU)
                    return Example_ruRU;

            return Example_enGB;
        }
    }
}

public enum TermSourceType
{
    ND,
    GW,
}

public enum TermType
{
    Monster,
    Mechanic,
}

public enum Category
{
    Waho_speak,
    Capital_Letters,
    Named_NPCs,
    Universe,
    Colonization,
    Imperium,
    Koronus_Expanse,
    Xenos,
    Chaos,
    CultMechanicus,
    Navy,
    Misc,
    High_Gothic,
    Yrliet_Jae_Speak,
    Owlcat_Speak,
    Space_Wolves,
    Abilities,
    Missing_Term
}
