using LocalizationTracker.Data;
using LocalizationTracker.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using wpf4gp;

namespace LocalizationTracker.Logic
{
    static class ImportDiffExporter
    {
        static ExcelStyles ExcelStyles = new ExcelStyles();

        static ColumnSettings[] _columnSettings = new[]
        {
            new ColumnSettings()
            {
                MinIndex = 5,
                MaxIndex = 7,
                Width = 60
            }
        };

        public static void SaveResultAsFile(ImportResult result)
        {
            if (TryGetPath(true, out var rootPath))
            {
                SaveResultAsFile(result, rootPath);
            }
        }

        public static void SaveResultsAsFile(ICollection<ImportResult> results)
        {
            if (results.Count == 0)
                return;

            if (TryGetPath(results.Count == 1, out var rootPath))
            {
                foreach (var result in results)
                {
                    SaveResultAsFile(result, rootPath);
                }
            }
        }

        static void SaveResultAsFile(ImportResult result, string rootPath)
        {
            try
            {
                var path = Path.Combine(rootPath, $"{result.LanguageGroup.Replace(':', '_')}_diffs.xlsx");
                ExportResults(path, result);
            }
            catch (Exception exc)
            {
                exc.ShowMessageBox();
            }
        }

        static void ExportResults(string path, ImportResult result)
        {
            using (var wrapper = new WorkbookWrapper(path, WrapperMod.ExportNewFile, _columnSettings))
            {
                SheetDataExtentions.NextRowID = 0;

                AddHeader(wrapper);
                var entries = result.ImportEntries;
                for (uint i = 0; i < entries.Count; i++)
                {
                    var pair = entries[(int)i];
                    AddImportEntry(pair, wrapper);
                }

                wrapper.Save();
            }
        }

        static void AddHeader(WorkbookWrapper wrapper)
        {
            wrapper.NewRow();

            wrapper.AddCell("Key");
            wrapper.AddCell("Path");
            wrapper.AddCell("Status");
            wrapper.AddCell("Messages");
            wrapper.AddCell("Source");
            wrapper.AddCell("Old Target");
            wrapper.AddCell("Result");
        }

        static void AddImportEntry(ImportEntry entry, WorkbookWrapper wrapper)
        {
            wrapper.NewRow();

            wrapper.AddCell(entry.Key);
            wrapper.AddCell(entry.Path);
            wrapper.AddCell(entry.Status.ToString(), GetStyleForStatus(entry.Status));
            wrapper.AddCell(entry.Messages);
            wrapper.AddCell(ExcelStyles.DiffToSharedString(entry.SourceDiffs), CellStyle.WordWrap);
            wrapper.AddCell(ExcelStyles.DiffToSharedString(entry.TargetDiffs), CellStyle.WordWrap);
            wrapper.AddCell(ExcelStyles.DiffToSharedString(entry.ResultDiffs), CellStyle.WordWrap);
        }

        static CellStyle GetStyleForStatus(ImportStatus status)
        {
            switch (status)
            {
                case ImportStatus.Ok:
                    return CellStyle.GreenSolid;

                case ImportStatus.Warning:
                    return CellStyle.YellowSolid;

                case ImportStatus.Error:
                    return CellStyle.RedSolid;

                default:
                    return CellStyle.Base;
            }
        }

        static bool TryGetPath(bool isSingle, out string path)
        {
            using (var dlg = new System.Windows.Forms.FolderBrowserDialog())
            {
                dlg.SelectedPath = Directory.GetCurrentDirectory();
                dlg.Description = $"Укажите папку для {(!isSingle ? "файлов" : "файла")} сравнения";
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    path = dlg.SelectedPath;
                    return true;
                }
            }

            path = string.Empty;
            return false;
        }
    }
}
