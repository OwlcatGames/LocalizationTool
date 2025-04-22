using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using JetBrains.Annotations;
using LocalizationTracker.Components;
using LocalizationTracker.Logic;
using LocalizationTracker.Windows;

namespace LocalizationTracker.Tools;

public class FilterFits
{
    [NotNull]
    public static InlinesWrapper MakeInlines(string text, Color? selectedColor = null)
    {
        return MakeInlines(new[] { new InlineTemplate(text) }, selectedColor);
    }

    public static InlinesWrapper MakeInlines(InlineTemplate[] inlineTemplates, Color? selectedColor = null)
    {
        if (string.IsNullOrWhiteSpace(StringsFilter.Filter.Text)) return new InlinesWrapper(inlineTemplates);

        var result = new List<InlineTemplate>();

        var searchText = StringsFilter.Filter.Text;

        if (StringsFilter.Filter.IgnoreCase == true)
        {
            searchText = searchText.ToLower();
        }

        int index;

        Color? filterColor = selectedColor ?? Brushes.LightBlue.Color;

        foreach (var i in inlineTemplates)
        {
            if (string.IsNullOrWhiteSpace(StringsFilter.Filter.Text)) searchText = "";

            var text = StringsFilter.Filter.IgnoreCase == true ? i.Text.ToLower() : i.Text;
            var foregroundColor = i.Foreground;
            Color? backgroundColor = i.Background;
            bool strikeThrough = i.StrikeThrough;
            FontWeight? fontWeight = i.FontWeight;

            if (text.Contains(searchText))
            {
                int startIndex = 0;

                while ((index = text.IndexOf(searchText, startIndex)) != -1)
                {
                    if (index > startIndex)
                    {
                        string beforeMatch = i.Text.Substring(startIndex, index - startIndex);
                        result.Add(MakeInline(beforeMatch, foregroundColor, backgroundColor, strikeThrough, fontWeight));
                    }

                    string match = i.Text.Substring(index, searchText.Length);

                    if (!backgroundColor.HasValue)
                    {
                        result.Add(MakeInline(match, foregroundColor, filterColor, strikeThrough, fontWeight));
                    }
                    else
                    {
                        result.Add(MakeInline(match, foregroundColor, backgroundColor, strikeThrough, fontWeight));
                    }

                    startIndex = index + searchText.Length;
                }

                if (startIndex < i.Text.Length)
                {
                    string remaining = i.Text.Substring(startIndex);
                    result.Add(MakeInline(remaining, foregroundColor, backgroundColor, strikeThrough, fontWeight));
                }
            }
            else
            {
                result.Add(i);
            }

        }

        return new InlinesWrapper(result.ToArray());
    }

    private static InlineTemplate MakeInline(string text,
                                             Color? foregroundColor = null,
                                             Color? color = null,
                                             bool strikeThrough = false,
                                             FontWeight? fontWeight = null)
        => new(text)
        {
            Background = color,
            Foreground = foregroundColor,
            StrikeThrough = strikeThrough,
            FontWeight = fontWeight
        };


}

