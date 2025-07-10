using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Office2013.ExcelAc;
using DocumentFormat.OpenXml.Office2016.Drawing.Command;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Vml.Office;
using LocalizationTracker.Data;
using LocalizationTracker.Utility;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using LocalizationTracker.Logic;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace LocalizationTracker.Excel
{
    public class ExcelLocalizationExporter : IExporter
    {
        private uint m_RowIndex;
        public string FileFilter => "Excel document|*.xlsx";

        public virtual ExportData PrepareDataToExport(ExportData data)
        {
            if (data.ExportParams.SortAsSvg == true)
            {
                data.Items = SortStrings(data);
            }

            return data;
        }

        public StringEntry[] SortStrings(ExportData data)
        {
            List<StringEntry> sortedStrings = new List<StringEntry>();
            Dictionary<DialogsData?, List<StringEntry>> strings = new();

            //non-shared strings
            var singleDialogStrings = data.Items
                .Where(w => w.DialogsDataList != null)
                .Where(w => w.DialogsDataList.Any(dialog =>
                    dialog.Nodes.Any(node =>
                        node.Text != null &&
                        node.Text.Key == (w.Key.Contains(":") ? w.Key.Split(":")[1] : w.Key) &&
                        node.Shared == false)))
                .GroupBy(grp => grp.DialogsDataList.FirstOrDefault())
                .ToDictionary(group => group.Key, group => group.ToList());


            //shared strings
            var sharedStrings = data.Items
                .Where(w => w.DialogsDataList != null)
                .Where(w => w.DialogsDataList
                    .Any(dialog => dialog.Nodes
                        .Any(node => node.Text != null
                            && node.Text.Key == (w.Key.Contains(":") ? w.Key.Split(":")[1] : w.Key) 
                            && node.Shared == true))) 
                .ToList();

            foreach (var sharedString in sharedStrings)
            {
                foreach (var dialog in sharedString.DialogsDataList)
                {
                    if (singleDialogStrings.ContainsKey(dialog)) 
                    {
                        singleDialogStrings[dialog].Add(sharedString);
                    }
                }
            }

            foreach (var key in singleDialogStrings.Keys)
            {
                foreach (var node in key.Nodes.Where(w => w.Text != null))
                {
                    string nodeKey = node.Text.Namespace != null ? $"{node.Text.Namespace}:{node.Text.Key}" : node.Text.Key;

                    if (node.Shared == true)
                    {
                        if (AppConfig.Instance.Engine == StringManager.EngineType.Unreal
                            && singleDialogStrings[key].Any(s => s.Key == nodeKey))
                        {
                            sortedStrings.Add(singleDialogStrings[key].First(s => s.Key == nodeKey));
                        }
                        else if (singleDialogStrings[key].Any(s => s.Key == node.Text.Key))
                        {
                            sortedStrings.Add(singleDialogStrings[key].First(s => s.Key == node.Text.Key));
                        }
                    }
                    else if (singleDialogStrings[key].Any(s => s.Key.Replace(":", "") == node.Text.Key))
                    {
                        sortedStrings.Add(singleDialogStrings[key].First(s => s.Key.Replace(":", "") == node.Text.Key));
                    }
                }
            }

            sortedStrings = sortedStrings.Distinct().ToList();

            // Добавляем строки, которые не связаны с диалогами
            sortedStrings.AddRange(data.Items.Where(w => w.DialogsDataList == null || !w.DialogsDataList.Any()));
            sortedStrings.AddRange(data.Items.Where(w => w.AssetStatus == AssetStatus.NotUsed));

            return sortedStrings.ToArray();
        }

        protected virtual ColumnSettings[] ColumnsSettings => new[]
        {
            new ColumnSettings()
            {
                MinIndex = 4,
                MaxIndex = 6,
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
                if (!CheckStringIsValidForExport(s))
                    continue;

                if (data.ExportParams.ExtraContext && parents[row] is { } parent)
                    AddContextRows(data, doc, sheetData, parent);

                AddRow(data, doc, sheetData, s, exportResult);
            }

            wrapper.Save();
            return exportResult;
        }

        protected virtual bool CheckStringIsValidForExport(StringEntry e) => true;

        protected virtual void AddHeader(in ExportData data, SheetData sheet)
        {
            var traits = string.Join(", ", data.ExportParams.Traits);
            var row = new Row();
            sheet.AppendRow(row);

            if (data.ExportParams.ExportTarget == ExportTarget.SpeakersStrings)
            {
                row.AppendCell(new Cell().SetText("Dialogues"));
                row.AppendCell(new Cell().SetText("Speaker"));
                row.AppendCell(new Cell().SetText("Strings"));
                row.AppendCell(new Cell().SetText($"Words (in Strings) in SourceLocale [{data.ExportParams.Source}]"));
                row.AppendCell(new Cell().SetText($"Words (in Strings) in TargetLocale [{data.ExportParams.Target.FirstOrDefault()}]"));
                row.AppendCell(new Cell().SetText("Cues"));
                row.AppendCell(new Cell().SetText($"Words (in Cues) in SourceLocale [{data.ExportParams.Source}]"));
                row.AppendCell(new Cell().SetText($"Words (in Cues) in TargetLocale [{data.ExportParams.Target.FirstOrDefault()}]"));
                row.AppendCell(new Cell().SetText("Answers"));
                row.AppendCell(new Cell().SetText($"Words (in Answers) in SourceLocale [{data.ExportParams.Source}]"));
                row.AppendCell(new Cell().SetText($"Words (in Answers) in TargetLocale [{data.ExportParams.Target.FirstOrDefault()}]"));
            }
            else
            {
                row.AppendCell(new Cell().SetText("key"));
                row.AppendCell(new Cell().SetText("name"));
                row.AppendCell(new Cell().SetText("speaker"));
                row.AppendCell(new Cell().SetText($"source [{data.ExportParams.Source}]"));

                foreach (var target in data.ExportParams.Target)
                {
                    row.AppendCell(new Cell().SetText($"current [{target}]"));
                    if (data.ExportParams.Target.Count == 1)
                        row.AppendCell(new Cell().SetText($"result [{target}:{traits}]"));
                }

                if (data.ExportParams.IncludeComment)
                {
                    row.AppendCell(new Cell().SetText("comment"));
                }

                row.AppendCell(new Cell().SetText("Shared"));

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
                AddTextCells(data, doc, row, s, result);
                row.AppendCell(new Cell().SetText(""));
                if (data.ExportParams.IncludeComment)
                {
                    row.AppendCell(new Cell().SetText(s.Data.GetLocale(data.ExportParams.Source)?.TranslatedComment ?? ""));
                }

                if (s.DialogsDataList != null && s.DialogsDataList.Count() != 0 && s.DialogsDataList.FirstOrDefault().Nodes
                        .Where(w => w.Text != null
                                && w.Text.Key == (s.Key.Contains(":") ? s.Key.Split(":")[1] : s.Key))
                        .FirstOrDefault().Shared == true)
                {
                    row.AppendCell(new Cell().SetText("shared string"));
                }
            }
            else
            {
                var row = new Row();
                sheet.AppendRow(row);
                AddTextCells(data, doc, row, s, result);
            }

        }

        protected virtual void AddTextCells(in ExportData data, SpreadsheetDocument doc, Row row, StringEntry s, ExportResults result)
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
            else if (data.ExportParams.TagRemovalPolicy == TagRemovalPolicy.RetainMfN)
            {
                sourceText = StringUtils.RemoveTagsExcept(sourceText, LocalizationExporter.TagsToRetain);
                var sharedSourceText = MarkNonVocalWords(sourceText);
                row.AppendCell(new Cell().SetSharedText(doc.AddSharedString(sharedSourceText)));

                foreach (var text in textList)
                {
                    targetText = StringUtils.RemoveTags(text, LocalizationExporter.TagsToRetain);
                    var sharedTargetText = MarkNonVocalWords(text);
                    row.AppendCell(new Cell().SetSharedText(doc.AddSharedString(sharedTargetText)));

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

            result.AddText(sourceText);
        }

        private SharedStringItem MarkNonVocalWords(string text)
        {
            SharedStringItem item = new SharedStringItem();

            int blockStart = 0;
            var matches = StringUtils.NonVocalWordMatcher().Matches(text);

            foreach (Match match in matches)
            {
                if (match.Index > blockStart)
                {
                    var substring = text[blockStart..match.Index];
                    var xmlRun = new Run();

                    xmlRun.PrependChild(Bold);

                    var blockText = new Text
                    {
                        Space = SpaceProcessingModeValues.Preserve,
                        Text = substring
                    };

                    xmlRun.Append(blockText);
                    item.Append(xmlRun);
                }

                var matchedSubstring = match.Value;
                var matchedRun = new Run();

                var matchedText = new Text
                {
                    Space = SpaceProcessingModeValues.Preserve,
                    Text = matchedSubstring
                };

                matchedRun.Append(matchedText);
                item.Append(matchedRun);

                blockStart = match.Index + match.Length;
            }

            if (text.Length - blockStart > 0)
            {
                var remainingSubstring = text[blockStart..text.Length];
                var xmlRun = new Run();
                xmlRun.PrependChild(Bold);
                var remainingText = new Text
                {
                    Space = SpaceProcessingModeValues.Preserve,
                    Text = remainingSubstring
                };
                xmlRun.Append(remainingText);
                item.Append(xmlRun);
            }

            return item;
        }


        private static RunProperties Bold
        {
            get
            {
                RunProperties props = new();
                props.Append(new Bold());
                return props;
            }
        }

        private void AddContextRows(in ExportData data, SpreadsheetDocument doc, SheetData sheet, StringEntry s)
        {
            var row = new Row();
            sheet.AppendRow(row);

            row.AppendCell(new Cell().SetText("[context]"));
            row.AppendCell(new Cell().SetText("[context]"));
            var gender = string.IsNullOrEmpty(s.Data.SpeakerGender) ? "unknown" : s.Data.SpeakerGender;
            var speaker = string.IsNullOrEmpty(s.Data.Speaker) ? string.Empty : $"{s.Data.Speaker}:{gender}";
            row.AppendCell(new Cell().SetText(speaker));

            var sourceText = s.Data.GetText(data.ExportParams.Source);
            if (data.ExportParams.TagRemovalPolicy == TagRemovalPolicy.RetainNone)
            {
                sourceText = StringUtils.RemoveTagsExcept(sourceText, Array.Empty<string>());
                row.AppendCell(new Cell().SetText(sourceText).SetStyle(CellStyle.Context));
            }
            else if (data.ExportParams.TagRemovalPolicy == TagRemovalPolicy.RetainMfN)
            {
                sourceText = StringUtils.RemoveTagsExcept(sourceText, LocalizationExporter.TagsToRetain);
                var sharedText = MarkNonVocalWords(sourceText);


                row.AppendCell(new Cell()
                .SetSharedText(doc.AddSharedString(sharedText))
                .SetStyle(CellStyle.Context));
            }
            else
                row.AppendCell(new Cell().SetText(sourceText).SetStyle(CellStyle.Context));

        }
    }


}