using System.Collections.Generic;
using Cathei.BakingSheet;
using DocumentFormat.OpenXml.Drawing.Diagrams;

namespace LocalizationTracker.Tools.GlossaryTools;

public interface IGlossary
{
    string Id { get; }
    public string ruRU { get; }
    public string Example_ruRU { get; }
    public string enGB { get; }
    public string Example_enGB { get; }

    public Dictionary<string, string> Example { get; }
    public Dictionary<string, string> Exact { get; }
    public string Comment { get; }
    bool TryGetTranslation(Locale locale, out string translation);
    string GetExampleTranslation(Locale? locale);

}


public class GlossarySheet : Sheet<GlossarySheet.Term>
{
    /// <summary>
    /// Table markdown
    /// </summary>
    public class Term : SheetRow, IGlossary
    {
        public string ruRU { get; private set; }
        public string Example_ruRU { get; private set; }
        public string enGB { get; private set; }
        public string Example_enGB { get; private set; }

        public Dictionary<string, string> Example { get; private set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Exact { get; private set; } = new Dictionary<string, string>();
        public string Comment { get; private set; }

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

    public class AmberGlossarySheet : Sheet<AmberGlossarySheet.AmberTerm>
    {
        /// <summary>
        /// Table markdown
        /// </summary>
        public class AmberTerm : SheetRow, IGlossary
        {
            public string ruRU { get; private set; }
            public string Example_ruRU { get; private set; }
            public string enUS { get; private set; }
            public string Example_enUS { get; private set; }
            public string Description { get; private set; }
            public string enGB  => enUS;
            public string Example_enGB => Example_enUS;
            public string Comment => Description;

            public Dictionary<string, string> Example { get; private set; } = new Dictionary<string, string>();
            public Dictionary<string, string> Exact { get; private set; } = new Dictionary<string, string>();

            public bool TryGetTranslation(Locale locale, out string translation)
            {
                translation = string.Empty;
                if (!Exact.TryGetValue(locale, out translation))
                {
                    if (locale == Locale.enUS)
                        translation = enUS;
                    if (locale == Locale.ruRU)
                        translation = ruRU;
                }

                return !string.IsNullOrEmpty(translation);
            }

            public string GetExampleTranslation(Locale? locale)
            {
                var translation = string.Empty;
                if (locale == null)
                    return Example_enUS;

                if (Exact.TryGetValue(locale, out translation))
                    return translation;

                if (locale == Locale.enUS)
                    return Example_enUS;
                if (locale == Locale.ruRU)
                    return Example_ruRU;

                return Example_enUS;
            }
        }
    }
}


