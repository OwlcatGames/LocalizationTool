using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using JetBrains.Annotations;
using LocalizationTracker.Components;
using WeCantSpell.Hunspell;
using LocalizationTracker.Utility;
using System.Text.RegularExpressions;
using System.Linq;
using System.Windows;

namespace LocalizationTracker.Tools
{
	public static class SpellCheck
	{
		
		public static HashSet<char> WordTeminations = new()
		{
			' ', ',', '.', '!', '?', ';', '{', '}', '|', '\'', '(', '"'
		};
		
		private static readonly IReadOnlyDictionary<Locale, WordList> Dictionaries =
			(from locale in Locale.Values
			let dic = CreateDictionary(locale)
			where dic != null
			select (Locale: locale, WordList: dic))
            .ToDictionary(v => v.Locale, v => v.WordList);

		private static WordList? CreateDictionary(string lang)
		{
			var affFile = $"dict/{lang}/index.aff";
            var dicFile = $"dict/{lang}/index.dic";

			if (!File.Exists(affFile) || !File.Exists(dicFile))
				return null;

            using var affixStream = File.OpenRead(affFile);
            using var mainStream = File.OpenRead(dicFile);

            string customDictFile = $"dict/{lang}/custom.txt";
            if (File.Exists(customDictFile))
            {
                var combined = new MemoryStream();
                mainStream.CopyTo(combined);

                using var customStream = File.OpenRead(customDictFile);
                customStream.CopyTo(combined);
                combined.Seek(0, SeekOrigin.Begin);
                var worldList = WordList.CreateFromStreams(combined, affixStream);
                return worldList;
            }
            else
            {
                var worldList = WordList.CreateFromStreams(mainStream, affixStream);
                return worldList;
            }
        }

        [NotNull]
		public static InlinesWrapper MakeInlines(Locale locale, string text)
		{
			if (!CanCheckLocale(locale))
				return new InlinesWrapper(text);

			var result = new List<InlineTemplate>();

			int blockStart = 0;
			var matches = StringUtils.SplitWordsTags().Matches(text);
			var lastWordIndex = 0;
			for (int i = 0; i < matches.Count; i++)
			{
				Match match = matches[i];
				var wordCapture = match.Groups["Word"].Captures.SingleOrDefault();
				if (wordCapture == null)
					continue;

				var word = wordCapture.Value;
				if (!IsCorrect(locale, word) || !IsCaseCorrect(i, lastWordIndex, matches, word))
				{
					var block = text[blockStart..wordCapture.Index];
					result.Add(MakeInline(block, true));
					result.Add(MakeInline(word, false));
					blockStart = wordCapture.Index + wordCapture.Length;
				}
				
				lastWordIndex = i;
			}

			var lastBlock = text[blockStart..];
			if(lastBlock.Length > 0)
				result.Add(MakeInline(lastBlock, true));

			return new InlinesWrapper(result.ToArray());
		}

		private static bool IsCaseCorrect(int currentWordIndex, int lastWordIndex, MatchCollection matches, string word)
		{
			if (currentWordIndex == 0)
			{
				return word[0] == Char.ToUpper(word[0]);
			}

			for (int i = lastWordIndex + 1; i < currentWordIndex; i++)
			{
				string value = matches[i].Value;
				if (value.Trim() == "." || value.Contains('!') || value.Contains('?'))
				{
					return word[0] == Char.ToUpper(word[0]);
				}
			}

			return true;
		}

		public static bool IsTermCaseCorrect(char charInText, char charInTerm, string? previousText = null)
		{
			if (charInText != charInTerm &&
				char.ToUpper(charInText) == charInTerm)
				return false;
			
			if (charInText == charInTerm &&
				char.ToUpper(charInTerm) == charInTerm)
				return true;
			
			if (previousText == null || 
				previousText.Trim() == "." || 
				previousText.Contains('!') || 
				previousText.Contains('?') ||
				previousText.Contains('}') ||
				previousText.Contains('"')
				)
			{
				return char.ToUpper(charInText) == charInText;
			}

			return char.ToLower(charInText) == charInText;
		}

		private static bool CanCheckLocale(Locale locale) => Dictionaries.ContainsKey(locale);
        private static bool IsCorrect(Locale locale, string word) => !Dictionaries.TryGetValue(locale, out var dic) || dic.Check(word);

        private static InlineTemplate MakeInline(string text, bool correct) => new(text) { Foreground = !correct ? Brushes.Red.Color : null, InlineType = correct ? InlineType.Default : InlineType.SpellCheckError};
		
    }
}