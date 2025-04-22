using StringsCollector.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LocalizationTracker.Data
{
    public class ExportRequestResult
    {
        public ExportRequestResult(Locale source, Locale target)
        {
            SourceLocale = source;
            TargetLocale = target;
        }

        #region Fields

        LinkedList<string> _fileNames = new LinkedList<string>();

        #endregion Fields
        #region Properties

        public Locale SourceLocale { get; }

        public Locale TargetLocale { get; }

        public int SourceVocalWordCount { get; private set; }
        public int SourceTotalWordCount { get; private set; }

        public int SourceSymbolCount { get; private set; }

        public IReadOnlyCollection<string> FileNames => _fileNames;

        public bool IsEmpty => FileNames.Count == 0;

        #endregion Properties
        #region Methods

        public void Append(ExportResults result)
        {
            if (result == default)
                return;

            if (result.SourceLocale == SourceLocale && result.TargetLocale == TargetLocale)
            {
                _fileNames.AddLast(result.FileName);
                SourceVocalWordCount += result.SourceVocalWordCount;
                SourceTotalWordCount += result.SourceTotalWordCount;
                SourceSymbolCount += result.SourceSymbolCount;
            }
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

            if (FileNames.Count == 1)
                sb.Append($"File: {FileNames.First()}\n");
            else
                sb.Append($"Files: {FileNames.Count}\n");
            if(TargetLocale == Locale.Sound)
                sb.Append($"Wordcount for VO: {SourceVocalWordCount}\n");
            sb.Append($"Total Word Count: {SourceTotalWordCount}\n");
            sb.Append($"Symbol Count: {SourceSymbolCount}\n");
            return sb.ToString();
        }

        #endregion Methods
    }
}
