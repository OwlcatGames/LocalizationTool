using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DeepL;
using LocalizationTracker.Data;
using LocalizationTracker.Windows;

namespace LocalizationTracker.Logic;

public static class TranslateUtility
{
    private static readonly TextTranslateOptions TranslateOptions = new TextTranslateOptions()
    {
        TagHandling = "xml"
    };
    private static string PrepareTagsForTranslation(StringManager.EngineType engine, string text)
        => engine switch
        {
            StringManager.EngineType.Unreal => PrepareTagsForTranslationUnreal(text),
            StringManager.EngineType.Unity => PrepareTagsForTranslationUnity(text),
            _ => throw new NotImplementedException()
        };

    private static string PrepareTagsForTranslationUnreal(string text)
    {
        // DeepL cannot handle our tags, but it knows how to treat xml tags. So we translate our tags into similar-looking
        // xml markup to restore them back in the translated string
        // basic idea:
        //  replace opening tags like {x with <tag id="x"> (same with {x}
        //  replace closing } with </tag>
        //  replace params separator with <pipe/>
        //  add special logic for {mf} and {g}

        var partsList = new List<string>();

        var builder = new StringBuilder();
        string currentTag;
        var idx = 0;
        while (idx < text.Length)
        {
            var c = text[idx];
            if (c != '{')
            {
                builder.Append(c);
                idx++;
                continue;
            }

            var tagStart = idx;
            var tagOpen = idx;
            do
            {
                idx++;
            } while (idx < text.Length && text[idx] != '}' && text[idx] != '|' && text[idx] != '{');

            if (idx >= text.Length || text[idx] == '{') // not closed tag
            {
                builder.Append(text, tagStart, idx - tagStart); // can't be nested here: nesting can only happen in parameters
                continue;
            }

            currentTag = text.Substring(tagStart + 1, idx - tagStart - 1);

            partsList.Clear();
            if (text[idx] == '}') // tag without parameters: {name} becomes <tag id="name"/>  
            {
                builder.Append($"<tag id=\"{currentTag}\"/>");
                idx++;
                continue;
            }

            tagStart = idx; // tag with parameters: read params
            while (idx < text.Length - 1)
            {
                idx++;
                if (text[idx] == '|' || text[idx] == '}')
                {
                    partsList.Add(text.Substring(tagStart + 1, idx - tagStart - 1));
                    tagStart = idx;
                }

                if (text[idx] == '}')
                    break;
                if (text[idx] == '{')
                    break;
            }

            if (text[idx] == '}') // closed tag with params
            {
                //builder.Append(template.Generate(capitalized, s_ParsList));
                if (AppConfig.Instance.DeepL.MFTags.Contains(currentTag.ToLowerInvariant()))
                {
                    // mf|a|b just becomes a - we don't try and translate both gendered versions (most often it's not even the whole word so)
                    builder.Append(partsList[0]);
                }
                else if (AppConfig.Instance.DeepL.GlossaryTags.Contains(currentTag.ToLowerInvariant()))
                {
                    // g|a|b becomes <tag id="g"><pipe/><ignore text="a"/><pipe/>b</tag> - first param is not translated
                    builder.Append($"<tag id=\"{currentTag}\"><pipe/><ignore text=\"{partsList[0]}\"/><pipe/>{partsList[1]}</tag>");
                }
                else
                {
                    // bc|a|b becomes <tag id="bc"><pipe/>a<pipe/>b</tag>
                    builder.Append($"<tag id=\"{currentTag}\">");
                    foreach (var par in partsList)
                    {
                        builder.Append("<pipe/>").Append(par);
                    }
                    builder.Append("</tag>");
                }
                idx++;
                continue;
            }

            if (idx >= text.Length - 1 || text[idx] == '{') // tag did not close - ignore all previous text
            {
                builder.Append(text, tagOpen, idx - tagOpen);
                // todo: handle nested tags here
            }
        }

        return builder.ToString();
    }

    private static string PrepareTagsForTranslationUnity(string text)
    {
        // DeepL cannot handle our tags, but it knows how to treat xml tags. So we translate our tags into similar-looking
        // xml markup to restore them back in the translated string
        // basic idea:
        //  replace {tag} with <{tag}> and {/tag} with </{tag}>. Tag separators | are ignored and passed as is.
        //  add special logic for {mf} and {g}

        var partsList = new List<string>();

        var builder = new StringBuilder();
        string currentTag;
        var idx = 0;
        while (idx < text.Length)
        {
            var c = text[idx];
            if (c != '{')
            {
                builder.Append(c);
                idx++;
                continue;
            }

            var tagStart = idx;
            var tagOpen = idx;
            do
            {
                idx++;
            } while (idx < text.Length && text[idx] != '}' && text[idx] != '|' && text[idx] != '{');

            if (idx >= text.Length || text[idx] == '{') // not closed tag
            {
                builder.Append(text, tagStart, idx - tagStart); // can't be nested here: nesting can only happen in parameters
                continue;
            }

            currentTag = text.Substring(tagStart + 1, idx - tagStart - 1);

            partsList.Clear();
            if (text[idx] == '}')
            {
                // tag without parameters:
                // {name} becomes <{name}>
                // {/name} becomes </{name}>

                if (currentTag[0] == '/')
                    builder.Append($"</{{{currentTag[1..]}");
                else
                    builder.Append($"<{{{currentTag}");
                builder.Append("}>");
                idx++;
                continue;
            }

            tagStart = idx; // tag with parameters: read params
            while (idx < text.Length - 1)
            {
                idx++;
                if (text[idx] == '|' || text[idx] == '}')
                {
                    partsList.Add(text.Substring(tagStart + 1, idx - tagStart - 1));
                    tagStart = idx;
                }

                if (text[idx] == '}')
                    break;
                if (text[idx] == '{')
                    break;
            }

            if (text[idx] == '}') // closed tag with params
            {
                if (AppConfig.Instance.DeepL.MFTags.Contains(currentTag.ToLowerInvariant()))
                {
                    // mf|a|b just becomes a - we don't try and translate both gendered versions (most often it's not even the whole word so)
                    builder.Append(partsList[0]);
                }
                else
                {
                    // {g|a} becomes <{g|a}>
                    // {/g|a} becomes </{g|a}>

                    if (currentTag[0] == '/')
                        builder.Append($"</{{{currentTag[1..]}");
                    else
                        builder.Append($"<{{{currentTag}");
                    foreach (var par in partsList)
                    {
                        builder.Append('|').Append(par);
                    }
                    builder.Append("}>");
                }
                idx++;
                continue;
            }

            if (idx >= text.Length - 1 || text[idx] == '{') // tag did not close - ignore all previous text
            {
                builder.Append(text, tagOpen, idx - tagOpen);
                // todo: handle nested tags here
            }
        }

        text = builder.ToString();
        text = text.Replace("[", "<sq>");
        text = text.Replace("]", "</sq>");

        return text;
    }

    private static string RestoreTagsAfterTranslation(StringManager.EngineType engine, string text)
        => engine switch
        {
            StringManager.EngineType.Unreal => RestoreTagsAfterTranslationUnreal(text),
            StringManager.EngineType.Unity => RestoreTagsAfterTranslationUnity(text),
            _ => throw new NotImplementedException()
        };

    private static string RestoreTagsAfterTranslationUnreal(string text)
    {
        // <pipe/> back to |
        text = text.Replace("<pipe/>", "|");
        // replace all closing tags with }
        text = text.Replace("</tag>", "}");
        // restore ignored text (glossary link ids)
        text = Regex.Replace(text, "<ignore text=\"(\\w+)\"/>", "$1");
        // replace all opening tags (before params) 
        text = Regex.Replace(text, "<tag id=\"(\\w+)\">", "{$1");
        // replace all single tags
        text = Regex.Replace(text, "<tag id=\"(\\w+)\"/>", "{$1}");
        return text;
    }

    private static string RestoreTagsAfterTranslationUnity(string text)
    {
        text = text.Replace("<{", "{");
        text = text.Replace("</{", "{/");
        text = text.Replace("}>", "}");
        text = text.Replace("<sq>", "[");
        text = text.Replace("</sq>", "]");
        return text;
    }

    private static string MapSourceLocale(Locale locale)
    {
        if (AppConfig.Instance.DeepL.SourceLocaleMap.TryGetValue(locale, out var mappedLocale))
            return mappedLocale;
        throw new Exception($"Cant find source locale {locale} mapping in Config.json");
    }

    private static string MapTargetLocale(Locale locale)
    {
        if (AppConfig.Instance.DeepL.TargetLocaleMap.TryGetValue(locale, out var mappedLocale))
            return mappedLocale;

        throw new Exception($"Cant find target locale {locale} mapping in Config.json");
    }

    public static async Task Translate(List<StringEntry> entries, Translator translator, bool addTagToString, StringManager.EngineType engine, IProgress<int> progress)
    {
        entries.RemoveAll(e => string.IsNullOrEmpty(e.SourceLocaleEntry.Text)); // deepl fails on empty strings
        if (entries.Count == 0)
            return;

        var countLeft = entries.Count;

        var sourceLocale = entries[0].SourceLocaleEntry.Locale;
        var targetLocale = entries[0].TargetLocaleEntry.Locale;

        var fromLocale = MapSourceLocale(sourceLocale);
        var toLocale = MapTargetLocale(targetLocale);

        var allTasks = new List<Task>(entries.Count);

        const int MaxRequests = 5;
        const int MaxSymbolsInTask = 10_000;

        var symbolsInTask = 0;
        var stringsToSend = new List<string>();
        for (int ii = 0; ii < entries.Count; ii++)
        {
            // combine strings into one request until we reach MaxSymbolsInTask in one request
            StringEntry entry = entries[ii];
            var source = PrepareTagsForTranslation(engine, entry.SourceLocaleEntry.Text);
            stringsToSend.Add(source);
            symbolsInTask += source.Length;
            if (symbolsInTask < MaxSymbolsInTask && ii < entries.Count - 1)
            {
                continue;
            }

            // send request
            int from = ii + 1 - stringsToSend.Count;
            var task = TranslateBatch(stringsToSend, from);

            async Task TranslateBatch(List<string> stringsToSend, int from)
            {
                int to = from + stringsToSend.Count;
                var translationResult = await translator.TranslateTextAsync(stringsToSend, fromLocale, toLocale, TranslateOptions);
                for (int jj = from; jj < to; jj++)
                {
                    var resultText = RestoreTagsAfterTranslation(engine, translationResult[jj - from].Text);

                    entries[jj].Data.UpdateTranslation(
                        targetLocale,
                        addTagToString ? "[ForRetranslation]" + resultText : resultText,
                        sourceLocale,
                        entries[jj].SourceLocaleEntry.Text);
                    entries[jj].Data.AddTrait(targetLocale, "AIGenerated");
                    entries[jj].Save();

                    if (StringsFilter.Filter.Mode == FilterMode.Tags_Mismatch)
                    {
                        // rerun comparison immediately if we're comparing tags
                        TagsList.Compare(entries[jj].SourceLocaleEntry.TagsList, entries[jj].TargetLocaleEntry.TagsList);
                        entries[jj].UpdateInlines();
                    }
                    else
                    {
                        entries[jj].TargetLocaleEntry.UpdateInlines();
                    }

                    countLeft--;
                }

                progress.Report(countLeft);
            }

            // prepare for the next one
            stringsToSend = new List<string>();
            symbolsInTask = 0;
            allTasks.Add(task);

            // wait for the first request if we already have more that MaxRequests in flight
            if (allTasks.Count >= MaxRequests)
            {
                await allTasks[0];
                allTasks.RemoveAt(0);
            }
        }

        await Task.WhenAll(allTasks);
    }

    public static async Task TranslateComment(List<StringEntry> entries, Translator translator, IProgress<int> progress, FilterMode mode)
    {

        if (mode == FilterMode.Voice_Comments)
            entries.RemoveAll(e => string.IsNullOrEmpty(e.SourceLocaleEntry.VoiceComment)); // deepl fails on empty strings
        else
            entries.RemoveAll(e => string.IsNullOrEmpty(e.SourceLocaleEntry.TranslatorComment)); // deepl fails on empty strings

        if (entries.Count == 0)
            return;

        var m_CountLeft = entries.Count;

        var sourceLocale = entries[0].SourceLocaleEntry.Locale;
        var targetLocale = entries[0].TargetLocaleEntry.Locale;

        var fromLocale = MapSourceLocale(sourceLocale);
        var toLocale = MapTargetLocale(targetLocale);

        var allTasks = new List<Task>(entries.Count);

        const int MaxRequests = 5;
        const int MaxSymbolsInTask = 10_000;

        var symbolsInTask = 0;
        var stringsToSend = new List<string>();
        for (int ii = 0; ii < entries.Count; ii++)
        {
            // combine strings into one request until we reach MaxSymbolsInTask in one request
            StringEntry entry = entries[ii];
            var source = entry.SourceLocaleEntry.TranslatorComment;

            if (mode == FilterMode.Voice_Comments)
                source = entry.SourceLocaleEntry.VoiceComment;

            stringsToSend.Add(source);
            symbolsInTask += source.Length;
            if (symbolsInTask < MaxSymbolsInTask && ii < entries.Count - 1)
            {
                continue;
            }

            // send request
            int from = ii + 1 - stringsToSend.Count;
            var task = TranslateBatch(stringsToSend, from);

            async Task TranslateBatch(List<string> stringsToSend, int from)
            {
                int to = from + stringsToSend.Count;
                var translationResult = await translator.TranslateTextAsync(stringsToSend, fromLocale, toLocale);
                for (int jj = from; jj < to; jj++)
                {
                    var resultText = translationResult[jj - from];

                    if (mode == FilterMode.Voice_Comments)
                    {
                        entries[jj].TargetLocaleEntry.VoiceComment = "[ForRetranslation]" + resultText.Text;
                    }
                    else
                    {
                        entries[jj].TargetLocaleEntry.TranslatorComment = "[ForRetranslation]" + resultText.Text;
                    }

                    m_CountLeft--;
                }

                progress.Report(m_CountLeft);
            }

            // prepare for the next one
            stringsToSend = new List<string>();
            symbolsInTask = 0;
            allTasks.Add(task);

            // wait for the first request if we already have more that MaxRequests in flight
            if (allTasks.Count >= MaxRequests)
            {
                await allTasks[0];
                allTasks.RemoveAt(0);
            }
        }

        await Task.WhenAll(allTasks);
    }
}