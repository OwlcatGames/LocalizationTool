using LocalizationTracker.Excel;
using LocalizationTracker.Data;
using LocalizationTracker.OpenOffice;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using LocalizationTracker.Utility;
using System.Text;
using System;
using System.Windows.Controls;
using LocalizationTracker.Windows;
using DocumentFormat.OpenXml.Drawing.Charts;
using LocalizationTracker.Logic;
using System.Linq;
using System.Windows.Forms;
using System.Text.Json;
using LocalizationTracker.Properties;


namespace LocalizationTracker
{
    static class LocalizationImporter
    {
        public static Guid? ImportGuid { get; private set; }
        static LocalizationImporter()
        {
            _importers = new Dictionary<string, IImporter>
            {
                [".xlsx"] = new ExcelImporter(),
                [".ods"] = new OpenOfficeImporter(),
                [".json"] = new JsonImporter(),
                ["_comments.xlsx"] = new VoiceCommentsImporter()
            };

            _formatFilters = GetSupportedFilterFormat();

            LoadGuid();
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

        private static void SaveGuid()
        {
            var guid = ImportGuid;
            var savedColor = JsonSerializer.Serialize(guid);
            Settings.Default.ImportGuid = savedColor;

        }
        private static void LoadGuid()
        {
            if (!string.IsNullOrWhiteSpace(Settings.Default.ImportGuid))
            {
                var savedColor = JsonSerializer.Deserialize<Guid>(Settings.Default.ImportGuid);
                ImportGuid = savedColor;
            }
            else
            {
                ImportGuid = Guid.NewGuid();
                SaveGuid();
            }
        }

        public static ImportRequestResult Import(Window window)
        {
            var result = new ImportRequestResult();
            var files = TryGetImportFolderPath();

            if (files.Count() != 0)
            {
                var failResults = new Dictionary<string, Exception>();
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
                            failResults.Add(filePath, ex);
                        }
                    }
                }

                ProcessFails(window, failResults);
            }

            return result;
        }

        private static string[] TryGetImportFolderPath(string? savedPath = null)
        {
            string path = string.Empty;

            var fileDialog = new OpenFileDialog
            {
                Filter = "All Files (*.*)|*.*",
                Multiselect = true,
                Title = "Select files to import",
                ClientGuid = ImportGuid
            };

            if (!string.IsNullOrEmpty(savedPath))
            {
                fileDialog.InitialDirectory = savedPath;
            }

            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                var selectedFiles = fileDialog.FileNames;
                if (selectedFiles.Length > 0)
                {
                    path = Path.GetDirectoryName(selectedFiles[0]) ?? string.Empty;
                    return fileDialog.FileNames;
                }
            }

            return Array.Empty<string>();
        }


        static void ProcessFails(Window window, Dictionary<string, Exception> fails)
        {
            if (fails.Count == 0)
                return;

            StringBuilder stringBuilder = new StringBuilder();

            foreach (var file in fails)
            {
                var name = Path.GetFileName(file.Key);
                stringBuilder.Append($"{name} : {file.Value.Message}");
            }

            var errorWindow = new ImportErrorWindow(fails)
            {
                Owner = window,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };
            errorWindow.ShowDialog();
        }

        static IImporter GetImporter(string filePath)
        {
            var ext = Path.GetExtension(filePath);

            if (Path.GetFileName(filePath).Contains("_comments.xlsx"))
            {
                _importers.TryGetValue("_comments.xlsx", out var commentsImporter);
                return commentsImporter;
            }

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
