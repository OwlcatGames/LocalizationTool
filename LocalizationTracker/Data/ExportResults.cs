using System.Text;
using StringsCollector.Data;
using LocalizationTracker.Utility;

namespace LocalizationTracker.Data
{
	public class ExportResults
	{
		public Locale SourceLocale { get; }

		public Locale TargetLocale { get; }

		public string FileName { get; }

		public int SourceVocalWordCount { get; private set; }
        public int SourceTotalWordCount { get; private set; }

        public int SourceSymbolCount { get; private set; }

		public ExportResults(Locale sourceLocale, Locale targetLocale, string fileName)
		{
			SourceLocale = sourceLocale;
			TargetLocale = targetLocale;
			FileName = fileName;
		}

		public void AddText(string text)
		{
			SourceSymbolCount += text.Length;
			var wordCount = StringUtils.CountTotalWords(text);
			if(TargetLocale == Locale.Sound)
				SourceVocalWordCount += wordCount - StringUtils.CountNonVocalWords(text);
            SourceTotalWordCount += wordCount;
		}

		public string GenerateText()
		{
			var sb = new StringBuilder();
			if (SourceLocale == TargetLocale)
			{
				sb.Append($"Exported for correction: {SourceLocale}\n");
			}
			else
			{
				sb.Append($"Exported for translation: {SourceLocale} => {TargetLocale}\n");
			}

			sb.Append($"File: {FileName}\n");
            if (TargetLocale == Locale.Sound)
                sb.Append($"Wordcount for VO: {SourceVocalWordCount}\n");
            sb.Append($"Total Word Count: {SourceTotalWordCount}\n");
            sb.Append($"Symbols Count: {SourceSymbolCount}\n");
			return sb.ToString();
		}
	}
}