using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using DocumentFormat.OpenXml.Spreadsheet;
using JetBrains.Annotations;
using LocalizationTracker.Data;
using LocalizationTracker.Excel;
using LocalizationTracker.Logic.Excel;
using LocalizationTracker.Logic.Excel.Exporters;
using LocalizationTracker.Logic.Excel.Wrappers;
using LocalizationTracker.OpenOffice;
using LocalizationTracker.Utility;
using LocalizationTracker.Windows;
using StringsCollector.Data.Unity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Forms;
using static Microsoft.FSharp.Core.ByRefKinds;

namespace LocalizationTracker
{
    public static class LocalizationExporter
    {
        private static readonly Dictionary<ExportTarget, IExporter> _exporters = new()
        {
            [ExportTarget.LocalizationToExcel] = new ExcelLocalizationExporter(),
            [ExportTarget.StringDiffToExcel] = new ExcelSourceUpdateExporter(new ExcelStyles()),
            [ExportTarget.TagsMismatchToExcel] = new ExcelTagsMismathExporter(new ExcelTagMismatchStyle()),
            [ExportTarget.LocalizationToOpenOffice] = new OpenOfficeExporter(),
            [ExportTarget.SpeakersStrings] = new SpeakersExport(),
            [ExportTarget.VoiceComments] = new VoiceCommentsExport(),
            [ExportTarget.UpdatedTraitToExcel] = new ExcelUpdatedTraitExporter(new ExcelStyles())
        };


        #region Methods

        [CanBeNull]
        public static void Export(string selectedDirName, StringEntry[] items, Window owner, string traits)
        {
            ExportResultsWindow? win = null;
            string savedPath = string.Empty;

            if (WinFormsUtility.TryGetExportParams(owner) is { } param)
            {
                if (param.SeparateFiles == true)
                {
                    foreach (var target in param.Target)
                    {
                        var res = ExportSingleFile(param, owner, items, selectedDirName, string.Empty, target, savedPath);
                        win = res.win;
                        savedPath = res.savedPath;
                    }

                }
                else
                {
                    if (param.ExportTarget == ExportTarget.UpdatedTraitToExcel)
                    {
                        selectedDirName = $"{selectedDirName}_{string.Join(", ", traits)}";
                    }

                    win = ExportSingleFile(param, owner, items, selectedDirName, traits).win;
                }
            }

            if (win != null)
                win.ShowDialog();
        }

        private static (ExportResultsWindow? win, string savedPath) ExportSingleFile(ExportParams param, Window owner, StringEntry[] items, string selectedDirName, string traits, Locale? target = null, string? savedPath = null)
        {
            ExportResultsWindow? win = null;
            ExportWrapper? wrapper = default;
            string filePath = string.Empty;

            if (_exporters.TryGetValue(param.ExportTarget, out var exporter))
            {
                ExportData data = new ExportData() { ExportParams = param, Items = items, Traits = traits };
                data = exporter.PrepareDataToExport(data);

                if (target != null)
                    data.ExportParams.Target = new List<Locale>() { target };

                if (param.UseFolderHierarchy)
                {
                    var rootPath = TryGetSaveFolderPath(Directory.GetCurrentDirectory());
                    if (!string.IsNullOrEmpty(rootPath))
                    {
                        if (string.IsNullOrEmpty(savedPath))
                        {
                            savedPath = rootPath;
                        }

                        data.DistFolder = rootPath;
                        wrapper = new ExportHierarchyWrapper(exporter, data);
                    }
                }
                else
                {
                    filePath = TryGetSavePath(selectedDirName, exporter.FileFilter, data.ExportParams, savedPath);

                    if (string.IsNullOrEmpty(savedPath))
                    {
                        savedPath = Path.GetDirectoryName(filePath);
                    }

                    data.DistFolder = filePath;
                    wrapper = new ExportSingleFileWrapper(exporter, data);
                }
                if (wrapper != null)
                {
                    win = new ExportResultsWindow(owner, wrapper);
                }
            }

            return (win, savedPath);
        }

        private static string TryGetSavePath(string selectedDirName, string filter, ExportParams param, string? savedPath = null)
        {
            string path = string.Empty;

            var fileName = $"{selectedDirName}_{param.Source}_{string.Join(",", param.Target)}";

            if (param.ExportTarget == ExportTarget.VoiceComments)
                fileName = $"{fileName}_comments";

            if (param.Traits.Length > 0)
            {
                var traits = string.Join("_", param.Traits);
                fileName += "_" + traits;
            }

            var fileDialog = new SaveFileDialog
            {
                Filter = filter,
                FileName = fileName
            };

            if (!string.IsNullOrEmpty(savedPath))
            {
                fileDialog.FileName = $"{savedPath}//{fileName}.xlsx";
                path = fileDialog.FileName;
                if (File.Exists(path))
                    File.Delete(path);
            }
            else
            {
                if (fileDialog.ShowDialog() == DialogResult.OK)
                {
                    path = fileDialog.FileName;
                    if (File.Exists(path))
                        File.Delete(path);
                }
                else
                {
                    path = string.Empty;
                }
            }

            return path;
        }

        public static string TryGetSaveFolderPath(string rootPath)
        {
            string path = string.Empty;

            var fileDialog = new FolderBrowserDialog()
            {
                SelectedPath = rootPath
            };

            using (fileDialog)
            {
                path = fileDialog.ShowDialog() == DialogResult.OK ? fileDialog.SelectedPath : string.Empty;
            }

            return path;
        }

        #endregion Methods

        public static readonly string[] TagsToRetain =
        {
            "mf",
            "n"
        };

        public static readonly string[] TagsEncyclopedia =
        {
            "d",
            "g"
        };

    }

    public class ExportData
    {
        public string DistFolder;
        public StringEntry[] Items;
        public ExportParams ExportParams;
        public string Traits = string.Empty;
    }

    public interface IExporter
    {
        public ExportData PrepareDataToExport(ExportData data);
        ExportResults Export(ExportData data, Action<int, int> OnExportProgressEvent);
        string FileFilter { get; }
    }

    public enum ExportTarget
    {
        LocalizationToExcel,
        LocalizationToOpenOffice,
        StringDiffToExcel,
        TagsMismatchToExcel,
        UpdatedTraitToExcel,
        SpeakersStrings,
        VoiceComments
    }

    public enum TagRemovalPolicy
    {
        RetainAll,
        RetainMfN,
        RetainNone,
        DeleteUpdatedTag
    }
}
