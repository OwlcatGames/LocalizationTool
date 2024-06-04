using LocalizationTracker.Excel;
using LocalizationTracker.Data;
using LocalizationTracker.OpenOffice;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using LocalizationTracker.Utility;
using System.Text;
using System;


namespace LocalizationTracker
{
    static class LocalizationImporter
    {
        static LocalizationImporter()
        {
            _importers = new Dictionary<string, IImporter>
            {
                [".xlsx"] = new ExcelImporter(),
                [".ods"] = new OpenOfficeImporter()
            };

            _formatFilters = GetSupportedFilterFormat();
        }

        static string GetSupportedFilterFormat()
        {
            var builder = new StringBuilder();
            builder.Append("Supported Formats |");
            foreach (var pair in _importers)
            {
                builder.Append('*');
                builder.Append(pair.Key);
                builder.Append(';');
            }

            return builder.ToString();
        }

        #region Fileds

        static Dictionary<string, IImporter> _importers;

        static string _formatFilters;

        #endregion Fields
        #region Methods

        public static ImportRequestResult Import()
        {
            var result = new ImportRequestResult();
            var rootPath = Directory.GetCurrentDirectory();
            if (WinFormsUtility.TryGetOpenFilePath(rootPath, _formatFilters, out var files))
            {
                var failResults = new LinkedList<string>();
                foreach (var filePath in files)
                {
                    var importer = GetImporter(filePath);
                    if (importer != default)
                    {
                        try
                        {
                            var fileResult = importer.Import(filePath);
                            if (fileResult != null)
                            {
                                result.AppendResult(fileResult);
                            }
                        }
                        catch (Exception ex)
                        {
                            failResults.AddLast(filePath);
                        }
                    }
                }

                ProcessFails(failResults);
            }

            return result;
        }

        static void ProcessFails(LinkedList<string> fails)
        {
            if (fails.Count == 0)
                return;

            StringBuilder stringBuilder = new StringBuilder();

            foreach (var file in fails)
            {
                var name = Path.GetFileName(file);
                stringBuilder.Append($"{name}\r\n");
            }

            MessageBox.Show($"Не удалось импортировать файлы в количестве {fails.Count}. Попробуйте открыть их в Excel и сохранить снова или перепроверить название. Список неимпортированных файлов:\r\n{stringBuilder.ToString()}");
        }

        static IImporter GetImporter(string filePath)
        {
            var ext = Path.GetExtension(filePath);
            _importers.TryGetValue(ext, out var importer);
            return importer;
        }

        #endregion Methods
    }

    internal interface IImporter
    {
        ImportResult Import(string fileName);
    }
}
