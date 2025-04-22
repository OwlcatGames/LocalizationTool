using Aspose.Svg.Builder;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using LocalizationTracker.Data;
using LocalizationTracker.Excel;
using LocalizationTracker.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalizationTracker.Logic.Excel.Exporters
{
    public class VoiceCommentsExport : IExporter
    {
        public ExportData PrepareDataToExport(ExportData data) => data;
        public string FileFilter => "Excel document|*.xlsx";

        protected ColumnSettings[] ColumnsSettings => new[]
        {
            new ColumnSettings()
            {
                MinIndex = 4,
                MaxIndex = 8,
                Width = 60
            }
        };

        public ExportResults Export(ExportData data, Action<int, int> OnExportProgressEvent)
        {
            if (data.DistFolder == "") return null;

            UnityAssets.ClearFoldersCache();
            var path = data.DistFolder;
            var strings = data.Items;
            var exportFileName = Path.GetFileNameWithoutExtension(path);
            var exportResult = new ExportResults(data.ExportParams.Source, data.ExportParams.Target.First(), exportFileName);

            using var wrapper = new WorkbookWrapper(path, WrapperMod.ExportNewFile, ColumnsSettings);
            var doc = wrapper.Document;
            var sharedStrings = doc.GetSharedTable().SharedStringTable;
            var workSheet = doc.WorkbookPart.WorksheetParts.Single().Worksheet;
            var sheetData = doc.GetSheet(0);

            OnExportProgressEvent(0, strings.Length + 1);
            var parents = data.ExportParams.ExtraContext ? strings.AsParallel().Select(UnityAssets.FindParent).ToArray() : Array.Empty<StringEntry>();

            AddHeader(data, sheetData);

            for (int row = 0; row < strings.Length; row++)
            {
                OnExportProgressEvent(row + 1, strings.Length + 1);
                StringEntry s = strings[row];

                AddRow(data, doc, sheetData, s, exportResult);
            }

            wrapper.Save();
            return exportResult;
        }

        private void AddHeader(in ExportData data, SheetData sheet)
        {
            var traits = string.Join(", ", data.ExportParams.Traits);
            var row = new Row();
            sheet.AppendRow(row);

            row.AppendCell(new Cell().SetText("key"));
            row.AppendCell(new Cell().SetText("name"));
            row.AppendCell(new Cell().SetText("speaker"));
            row.AppendCell(new Cell().SetText($"source [{data.ExportParams.Source}]"));

            foreach (var target in data.ExportParams.Target)
            {
                row.AppendCell(new Cell().SetText($"current [{target}]"));
                if (data.ExportParams.Target.Count == 1)
                    row.AppendCell(new Cell().SetText($"result [{target}:{traits}]"));
                row.AppendCell(new Cell().SetText($"source text [{data.ExportParams.Source}]"));
                row.AppendCell(new Cell().SetText($"target text [{target}]"));
            }
        }

        private void AddRow(in ExportData data, SpreadsheetDocument doc, SheetData sheet, StringEntry s, ExportResults result)
        {
            if (data.ExportParams.ExportTarget != ExportTarget.SpeakersStrings)
            {
                var row = new Row();
                sheet.AppendRow(row);

                row.AppendCell(new Cell().SetText(s.Data.Key));
                row.AppendCell(new Cell().SetText(s.PathRelativeToStringsFolder));

                var gender = string.IsNullOrEmpty(s.Data.SpeakerGender) ? "unknown" : s.Data.SpeakerGender;
                var speaker = string.IsNullOrEmpty(s.Data.Speaker) ? string.Empty : $"{s.Data.Speaker}:{gender}";
                row.AppendCell(new Cell().SetText(speaker));
                row.AppendCell(new Cell().SetText(s.SourceLocaleEntry.VoiceComment));
                row.AppendCell(new Cell().SetText(s.TargetLocaleEntry.VoiceComment));
                row.AppendCell(new Cell().SetText(""));
                AddTextCells(data, doc, row, s, result);

                result.AddText(s.SourceLocaleEntry.VoiceComment);

            }
            else
            {
                var row = new Row();
                sheet.AppendRow(row);
                AddTextCells(data, doc, row, s, result);
            }

        }

        protected void AddTextCells(in ExportData data, SpreadsheetDocument doc, Row row, StringEntry s, ExportResults result)
        {
            var sourceText = s.Data.GetText(data.ExportParams.Source);
            string targetText = "";
            List<string> textList = new List<string>();

            foreach (var target in data.ExportParams.Target)
            {
                textList.Add(s.Data.GetText(target));
            }

            if (data.ExportParams.TagRemovalPolicy == TagRemovalPolicy.RetainNone)
            {
                sourceText = StringUtils.RemoveTags(sourceText, Array.Empty<string>());
                row.AppendCell(new Cell().SetText(sourceText));
                foreach (var text in textList)
                {
                    targetText = StringUtils.RemoveTags(text, Array.Empty<string>());
                    row.AppendCell(new Cell().SetText(text));
                }
            }
            else if (data.ExportParams.TagRemovalPolicy == TagRemovalPolicy.DeleteUpdatedTag)
            {
                StringBuilder stringBuilder = new StringBuilder();

                System.Windows.Media.Color color = new System.Windows.Media.Color
                {
                    R = 215,
                    G = 227,
                    B = 188,
                    A = 255
                };

                var line = data.Items.Where(w => w.SourceLocaleEntry.Text == sourceText).First();
                var inlines = line.SourceLocaleEntry.Inlines.InlineTemplates.ToList();

                for (int i = 0; i < inlines.Count; i++)
                {
                    if (inlines[i].Background == color)
                    {
                        var text = StringUtils.RemoveTags(inlines[i].Text, LocalizationExporter.TagsEncyclopedia);
                        stringBuilder.Append(text);
                    }
                    else if (inlines[i].Background == null)
                    {
                        stringBuilder.Append(inlines[i].Text);
                    }
                }

                sourceText = stringBuilder.ToString();
                row.AppendCell(new Cell().SetText(sourceText));

                foreach (var text in textList)
                {
                    targetText = StringUtils.RemoveTags(text, Array.Empty<string>());
                    row.AppendCell(new Cell().SetText(text));
                }
            }
            else
            {
                row.AppendCell(new Cell().SetText(sourceText));
                foreach (var text in textList)
                {
                    row.AppendCell(new Cell().SetText(text));
                }
            }

        }

    }
}
