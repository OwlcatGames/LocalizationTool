using DocumentFormat.OpenXml.Spreadsheet;
using LocalizationTracker.Data;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace LocalizationTracker.Logic
{
    public class JsonImporter : IImporter
    {
        private Locale m_TargetLocale;

        private string m_Key;

        private string m_Result;

        public ImportResult Import(string fileName)
        {
            var results = new List<ImportEntry>();
            JsonLocalizationData strings;

            m_TargetLocale = Locale.Values.Where(w => w.Code == Path.GetFileNameWithoutExtension(fileName)).FirstOrDefault();

            using (var sr = new StreamReader(fileName))
            {
                var text = sr.ReadToEnd();
                strings = JsonSerializer.Deserialize<JsonLocalizationData>(text);
            }

            foreach (var str in strings.Strings)
            {
                var result = ImportString(str);

                if (result != null && !string.IsNullOrEmpty(result.Key))
                {
                    result.MakeDiffs();
                    results.Add(result);
                }
            }

            var importResult = new ImportResult(fileName, $"\"\":{m_TargetLocale.Code}", results);
            return importResult;
        }

        private ImportEntry ImportString(KeyValuePair<string, string> str)
        {
            var result = new ImportEntry();
            StringEntry se = null;

            if (!string.IsNullOrEmpty(str.Key))
            {
                result.Key = str.Key;
                if (StringManager.StringsByKey.TryGetValue(result.Key, out se))
                {
                    se.Reload();
                    result.Path = se.AbsolutePath;
                    result.ImportResult = se.Data.GetText(m_TargetLocale);
                }
            }

            result.ImportSource = "";
            result.ImportTarget = "";

            if (!string.IsNullOrEmpty(str.Value))
            {
                result.ImportResult = str.Value;
            }

            if (string.IsNullOrEmpty(str.Key))
            {
                result.Status = ImportStatus.Error;
                result.AddMessage("String key is empty");
            }
            else if (se == null)
            {
                result.Status = ImportStatus.Error;
                result.AddMessage("Key doesn't exist in database.");
            }

            if (string.IsNullOrEmpty(str.Value))
            {
                result.Status = ImportStatus.Error;
                result.AddMessage("Imported string is empty");
            }

            if (se == null || result.Status == ImportStatus.Error)
            {
                result.Status = ImportStatus.Error;
                return result;
            }

            se.Data.UpdateTranslation(m_TargetLocale, result.ImportResult, null, result.ImportSource);

            se.Save();

            return result;
        }
    }
}

