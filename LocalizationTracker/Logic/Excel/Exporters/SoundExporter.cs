using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using LocalizationTracker.Data;
using LocalizationTracker.Excel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LocalizationTracker.Logic.Excel.Exporters
{
    public class SoundExporter : ExcelLocalizationExporter
    {
        private uint m_RowIndex;
        public string FileFilter => "Excel document|*.xlsx";

        protected virtual ColumnSettings[] ColumnsSettings => new[]
        {
            new ColumnSettings()
            {
                MinIndex = 4,
                MaxIndex = 6,
                Width = 150
            }
        };

        public override ExportData PrepareDataToExport(ExportData data)
        {

            if (data.Items.Where(w => w.Data.Kind == Data.Unreal.UnrealStringData.StringKind.DialogAnswer).Any())
            {
                return PrepareDialogWithAnswersToExport(data);
            }

            List<StringEntry> OrderedDialog = new List<StringEntry>();
            StringEntry nextItem = null;
            var startDialog = data.Items.Where(w => string.IsNullOrEmpty(w.Data.ParentId.Key));

            if (startDialog.Count() == 1)
            {
                OrderedDialog.Add(startDialog.FirstOrDefault());
                nextItem = data.Items.Where(w => w.ParentId.Key == startDialog.FirstOrDefault().Key.Replace(":", "")).FirstOrDefault();
            }
            else if (startDialog.Count() > 1)
            {
                foreach (var line in startDialog)
                {
                    OrderedDialog.Add(line);
                    nextItem = data.Items.Where(w => w.ParentId.Key == line.Key.Replace(":", "")).FirstOrDefault();
                    while (nextItem != null)
                    {
                        OrderedDialog.Add(nextItem);
                        nextItem = data.Items.Where(w => w.ParentId.Key == nextItem.Key.Replace(":", "")).FirstOrDefault();
                    }

                    OrderedDialog.Add(new StringEntry(string.Empty));
                }
            }

            return new SoundExportData(data, OrderedDialog);
        }

        private ExportData PrepareDialogWithAnswersToExport(ExportData data)
        {
            List<StringEntry> OrderedDialog = new List<StringEntry>();

            var answersDictionary = data.Items
                .Where(w => w.Data.Kind == Data.Unreal.UnrealStringData.StringKind.DialogAnswer)
                .GroupBy(grp => grp.ParentId.Key)
                .ToDictionary(
                    grp => grp.Key,
                    grp => grp.ToList()
                );

            var startDialog = data.Items.Where(w => string.IsNullOrEmpty(w.ParentId.Key)).FirstOrDefault();
            OrderedDialog.Add(startDialog);
            var nextItem = data.Items.Where(w => w.ParentId.Key == startDialog.Key.Replace(":", "")).FirstOrDefault();

            while (nextItem != null)
            {
                OrderedDialog.Add(nextItem);

                if (answersDictionary.ContainsKey(nextItem.Key.Replace(":", "")))
                {
                    foreach (var answer in answersDictionary[nextItem.Key.Replace(":", "")])
                    {
                        OrderedDialog.Add(new StringEntry(string.Empty));

                        var nextAnswer = data.Items.Where(w => w.ParentId != null && w.ParentId.Key.Replace(":", "") == answer.Key.Replace(":", "")).FirstOrDefault();
                        
                        while (nextAnswer != null)
                        {
                            OrderedDialog.Add(nextAnswer);
                            if (answersDictionary.ContainsKey(nextAnswer.Key.Replace(":", "")))
                            {
                                nextItem = nextAnswer;
                            }
                            nextAnswer = data.Items.Where(w => w.ParentId != null && w.ParentId.Key.Replace(":", "") == nextAnswer.Key.Replace(":", "")).FirstOrDefault();
                        }

                        OrderedDialog.Add(new StringEntry(string.Empty));
                    }
                }

                nextItem = data.Items.Where(w => w.ParentId != null && w.ParentId.Key.Replace(":", "") == nextItem.Key.Replace(":", "")).FirstOrDefault();
            }

            return new SoundExportData(data, OrderedDialog);

        }

        private void AddRow(in ExportData data, SpreadsheetDocument doc, SheetData sheet, StringEntry s, ExportResults result)
        {
            var row = new Row();
            sheet.AppendRow(row);
            AddTextCells(data, doc, row, s, result);
        }

        protected override void AddTextCells(in ExportData data, SpreadsheetDocument doc, Row row, StringEntry s, ExportResults result)
        {
            if (data is SoundExportData exportData)
            {
                row.Append(new Cell().SetText(s.SelectedFolderPath).SetStyle(LocalizationTracker.Excel.CellStyle.WordWrap));
                row.Append(new Cell().SetText(s.Speaker).SetStyle(LocalizationTracker.Excel.CellStyle.WordWrap));
                row.Append(new Cell().SetText(s.TargetLocaleEntry.Text).SetStyle(LocalizationTracker.Excel.CellStyle.WordWrap));
                row.Append(new Cell().SetText(s.Data.Comment).SetStyle(LocalizationTracker.Excel.CellStyle.WordWrap));
                row.Append(new Cell().SetStyle(LocalizationTracker.Excel.CellStyle.WordWrap));
                row.Append(new Cell().SetStyle(LocalizationTracker.Excel.CellStyle.WordWrap));
            }
        }
    }

    public class SoundExportData : ExportData
    {
        public SoundExportData(ExportData data, List<StringEntry> orderedDialog)
        {
            DistFolder = data.DistFolder;
            Items = orderedDialog.ToArray();
            ExportParams = data.ExportParams;
        }

    }
}

