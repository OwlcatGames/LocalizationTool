using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;
using LocalizationTracker.Data;
using LocalizationTracker.Properties;
using LocalizationTracker.Utility;

namespace LocalizationTracker.OpenOffice
{
    public class OpenOfficeExporter : IExporter
    {
        public string FileFilter => "OpenOffice spreadsheet|*.ods";

        ExportData IExporter.PrepareDataToExport(ExportData data)
            => data;

        /// <summary>
        /// Template: 7 columns (0-7), first row is header, second row is prototype
        /// key(0); name(1); speaker(2); source(3); current(4); result(5); comment(6) 
        /// </summary>
        public ExportResults Export(ExportData data, Action<int, int> OnExportProgressEvent)
        {
            UnityAssets.ClearFoldersCache();
            var fileName = data.DistFolder;
            var strings = data.Items;

            XmlDocument document;
            XmlNode tableXml;

            var exportFileName = Path.GetFileName(fileName);
            var exportResults = new ExportResults(data.ExportParams.Source, data.ExportParams.Target.FirstOrDefault(), exportFileName);
            File.Copy(Settings.Default.OpenOfficeTemplate, fileName, true);
            using var file = ZipFile.Open(fileName, ZipArchiveMode.Update);
            using (var contentStream = file.GetEntry("content.xml")?.Open())
            {
                if (contentStream == null)
                {
                    throw new Exception($"Can't find 'content.xml' file in {fileName}");
                }

                document = new XmlDocument();
                document.Load(contentStream);

                tableXml = document.GetTable();
            }

            file.GetEntry("content.xml")?.Delete();

            using (var contentStream = file.CreateEntry("content.xml").Open())
            {
                SetupHeader(data, tableXml);

                var m_RowIndex = 1;
                foreach (var node in tableXml.GetRows().Skip(2).ToArray())
                {
                    tableXml.RemoveChild(node);
                }

                int current = 0;
                int target = strings.Length;
                foreach (var s in strings)
                {
                    current++;
                    if (data.ExportParams.ExtraContext)
                    {
                        if (UnityAssets.FindParent(s) is { } parent)
                            AddContextRows(data, ref m_RowIndex, tableXml, parent);
                    }

                    AddRow(data, tableXml, ref m_RowIndex, s, exportResults);
                    OnExportProgressEvent(current, target);
                }

                document.Save(contentStream);
            }
            return exportResults;
        }

        private void SetupHeader(in ExportData data, XmlNode tableXml)
        {
            var fromLoc = data.ExportParams.Source;
            var toLoc = data.ExportParams.Target;
            var traits = data.ExportParams.Traits;
            var header = tableXml.GetRows().ElementAt(0);

            int i = 0;
            foreach (var cell in header.GetCells())
            {
                switch (i++)
                {
                    case 0:
                        cell.GetText().InnerText = "key";
                        break;
                    case 1:
                        cell.GetText().InnerText = "name";
                        break;
                    case 2:
                        cell.GetText().InnerText = "speaker";
                        break;
                    case 3:
                        cell.GetText().InnerText = $"source [{fromLoc}]";
                        break;
                    case 4:
                        cell.GetText().InnerText = $"current [{toLoc}]";
                        break;
                    case 5:
                        var traitsText = string.Join(", ", traits);
                        cell.GetText().InnerText = $"result [{toLoc}:{traitsText}]";
                        break;
                    case 6:
                        if (data.ExportParams.IncludeComment)
                        {
                            cell.GetText().InnerText = "Comment";
                        }

                        break;
                }
            }
        }

        private void AddRow(in ExportData data, XmlNode tableXml, ref int rowIndex, StringEntry s, ExportResults results)
        {
            SetupRow(ref rowIndex, tableXml);

            SetupCell(tableXml, 0, s.Data.Key);
            SetupCell(tableXml, 1, s.PathRelativeToStringsFolder);
            var gender = string.IsNullOrEmpty(s.Data.SpeakerGender) ? "unknown" : s.Data.SpeakerGender;
            var speaker = string.IsNullOrEmpty(s.Data.Speaker) ? string.Empty : $"{s.Data.Speaker}:{gender}";
            SetupCell(tableXml, 2, speaker);

            var sourceText = s.Data.GetText(data.ExportParams.Source);
            var targetText = "";

            foreach (var text in data.ExportParams.Target)
            {
                if (data.ExportParams.TagRemovalPolicy == TagRemovalPolicy.RetainNone)
                {
                    sourceText = StringUtils.RemoveTags(sourceText, Array.Empty<string>());
                    targetText = StringUtils.RemoveTags(text, Array.Empty<string>());
                }
                else if (data.ExportParams.TagRemovalPolicy == TagRemovalPolicy.RetainMfN)
                {
                    sourceText = StringUtils.RemoveTags(sourceText, LocalizationExporter.TagsToRetain);
                    targetText = StringUtils.RemoveTags(text, LocalizationExporter.TagsToRetain);
                }

                results.AddText(sourceText);
                SetupCell(tableXml, 3, sourceText, true);
                SetupCell(tableXml, 4, text, true);
                SetupCell(tableXml, 5, "");

                if (data.ExportParams.IncludeComment)
                {
                    SetupCell(tableXml, 6, s.Data.GetLocale(data.ExportParams.Source)?.TranslatedComment ?? "");
                }
            }

        }

        private void AddContextRows(in ExportData data, ref int rowIndex, XmlNode tableXml, StringEntry s)
        {
            // always add empty row
            SetupRow(ref rowIndex, tableXml);
            SetupCell(tableXml, 0, "[context]");

            SetupRow(ref rowIndex, tableXml);
            SetupCell(tableXml, 0, "[context]");
            var gender = string.IsNullOrEmpty(s.Data.SpeakerGender) ? "unknown" : s.Data.SpeakerGender;
            var speaker = string.IsNullOrEmpty(s.Data.Speaker) ? string.Empty : $"{s.Data.Speaker}:{gender}";
            SetupCell(tableXml, 1, speaker);

            string sourceText = s.Data.GetText(data.ExportParams.Source);
            if (data.ExportParams.TagRemovalPolicy == TagRemovalPolicy.RetainNone)
            {
                sourceText = StringUtils.RemoveTagsExcept(sourceText, Array.Empty<string>());
            }
            else if (data.ExportParams.TagRemovalPolicy == TagRemovalPolicy.RetainMfN)
            {
                sourceText = StringUtils.RemoveTagsExcept(sourceText, LocalizationExporter.TagsToRetain);
            }
            SetupCell(tableXml, 2, sourceText);
        }

        private void SetupRow(ref int rowIndex, XmlNode tableXml)
        {
            if (rowIndex++ > tableXml.GetRows().Count())
            {
                tableXml.AppendChild(tableXml.GetRows().ElementAt(1).Clone());
            }

            foreach (var cell in tableXml.GetRows().Last().GetCells())
            {
                cell.GetText().InnerText = "";
            }
        }

        private void SetupCell(XmlNode tableXml, int column, string text, bool wordWrap = false)
        {
            tableXml.GetRows().Last().ChildNodes[column].GetText().InnerText = text;
        }
    }
}