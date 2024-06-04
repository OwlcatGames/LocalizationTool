using System.Collections.Generic;

namespace LocalizationTracker.Data
{
    public class ImportRequestResult
    {
        public ImportRequestResult()
        {
            _importResults = new Dictionary<string, ImportResult>();
        }

        Dictionary<string, ImportResult> _importResults;

        public IReadOnlyDictionary<string, ImportResult> ImportResults => _importResults;

        public void AppendResult(ImportResult fileResult)
        {
            if (_importResults.TryGetValue(fileResult.LanguageGroup, out var existResult))
                existResult.UnionWith(fileResult);
            else _importResults.Add(fileResult.LanguageGroup, fileResult);
        }
    }
}
