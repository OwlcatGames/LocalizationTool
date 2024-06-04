using System.Collections.Generic;

namespace LocalizationTracker.Data
{
    public class ImportResult
    {
        public ImportResult(string filePath, string langGroup, List<ImportEntry> entries)
        {
            LanguageGroup = langGroup;
            _importEntries = entries;
            _sourceFiles = new List<string>() { filePath };
        }

        List<ImportEntry> _importEntries;
        List<string> _sourceFiles;

        public readonly string LanguageGroup;
        public IReadOnlyList<string> SourceFiles => _sourceFiles;
        public IReadOnlyList<ImportEntry> ImportEntries => _importEntries;

        public void UnionWith(ImportResult fileResult)
        {
            if (fileResult.LanguageGroup.Equals(LanguageGroup))
            {
                _sourceFiles.AddRange(fileResult._sourceFiles);
                _importEntries.AddRange(fileResult._importEntries);
            }
        }

        public override string ToString()
            => $"{LanguageGroup}. Строк: {ImportEntries.Count}";
    }
}
