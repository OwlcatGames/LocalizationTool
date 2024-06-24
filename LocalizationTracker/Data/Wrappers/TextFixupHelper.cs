using Kingmaker.Localization.Shared;

namespace LocalizationTracker.Data.Wrappers;

public class TextFixupHelper
{
    public static string ApplyFixups(Locale locale, string text)
    {
        if (locale == Locale.ruRU || locale == Locale.enGB) // todo: this needs some sort of config
        {
            text = text.Replace("«", "\"");
            text = text.Replace("»", "\"");
            text = text.Replace("“", "\"");
            text = text.Replace("”", "\"");
            text = text.Replace("’", "'");
        }

        text = text.Replace(" - ", " — ");
        text = text.Replace("\r", "");
        //text = text.TrimEnd();
        while (text.Contains("  "))
        {
            text = text.Replace("  ", " ");
        }
        while (text.Contains(" \n"))
        {
            text = text.Replace(" \n", "\n");
        }
        while (text.Contains("\n "))
        {
            text = text.Replace("\n ", "\n");
        }
        while (text.Contains("\n\n"))
        {
            text = text.Replace("\n\n", "\n");
        }

        return text;
    }
}