using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using LocalizationTracker.Components;
using LocalizationTracker.Data;
using LocalizationTracker.Utility;
using LocalizationTracker.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace LocalizationTracker.Excel
{
    public class SpeakersExport : ExcelLocalizationExporter
    {
        public Locale SourceLocale
        {
            get => StringEntry.SourceLocale;
            set
            {
                StringEntry.SourceLocale = value;
            }
        }

        public Locale[] SourceLocaleValues => Locale.SourceLocaleValues;

        public Locale TargetLocale
        {
            get => StringEntry.TargetLocale;
            set
            {
                StringEntry.TargetLocale = value;
            }
        }

        protected override ColumnSettings[] ColumnsSettings => new[]
{
            new ColumnSettings()
            {
                MinIndex = 1,
                MaxIndex = 11,
                Width = 30
            }
        };

        public override ExportData PrepareDataToExport(ExportData data)
        {
            List<SpeakerData> speakerDatas = new List<SpeakerData>();
            string directoryName;

            var speakersGroup = data.Items
                .GroupBy(item => item.Speaker);

            StringEntry[] stringEntries = speakersGroup
                .Select(group => group.First())
                .ToArray();

            var speakers = speakersGroup
                .Select(group => new
                {
                    Speaker = group.Key,
                    Count = group.Count(),
                    CuesCount = group.Where(w => !w.AbsolutePath.Contains("Answer")).Count(), //так как куями считаются все строки, не являющиеся ансверами
                    AnswersCount = group.Where(w => w.AbsolutePath.Contains("Answer")).Count(),
                    Key = group.Select(s => s.Key).First()
                })
                .ToList();

            foreach (var item in speakers)
            {
                StringBuilder stringBuilder = new StringBuilder();
                SpeakerData speakerData = new SpeakerData
                {
                    Speaker = item.Speaker,
                    Count = item.Count,
                    CueCount = item.CuesCount,
                    AnswersCount = item.AnswersCount,
                    Key = item.Key,
                };

                StringEntry[] localeWordsCount = data.Items.Where(s => s.Speaker == item.Speaker).ToArray();

                speakerData.CueSourceLocaleWordsCount = GetWordCount(localeWordsCount.Where(s => !s.AbsolutePath.Contains("Answer")).ToArray(), data.ExportParams.Source) ?? 0; //так как куями считаются все строки, не являющиеся ансверами
                speakerData.AnswersSourceLocaleWordsCount = GetWordCount(localeWordsCount.Where(s => s.AbsolutePath.Contains("Answer")).ToArray(), data.ExportParams.Source) ?? 0;
                speakerData.CueTargetLocaleWordsCount = GetWordCount(localeWordsCount.Where(s => !s.AbsolutePath.Contains("Answer")).ToArray(), data.ExportParams.Target.FirstOrDefault()) ?? 0;
                speakerData.AnswersTargetLocaleWordsCount = GetWordCount(localeWordsCount.Where(s => s.AbsolutePath.Contains("Answer")).ToArray(), data.ExportParams.Target.FirstOrDefault()) ?? 0;
                speakerData.StringsWordsCountTargetLocale = GetWordCount(localeWordsCount, data.ExportParams.Target.FirstOrDefault()) ?? 0;
                speakerData.StringsWordsCountSourceLocale = GetWordCount(localeWordsCount, data.ExportParams.Source) ?? 0;

                List<string> uniqueDirectories = new List<string>();
                foreach (var line in localeWordsCount)
                {
                    string[] directory = line.DirectoryRelativeToStringsFolder.Trim().Split("/");
                    if (string.IsNullOrEmpty(directory.Last()))
                    {
                        directoryName = directory[directory.Length - 2];
                    }
                    else
                    {
                        directoryName = directory[directory.Length - 1];
                    }

                    if (!uniqueDirectories.Contains(directoryName))
                    {
                        uniqueDirectories.Add(directoryName);
                    }
                }

                var test = uniqueDirectories.ToArray();
                stringBuilder.AppendJoin(",",uniqueDirectories.ToArray());
                speakerData.StringsDirectories = stringBuilder.ToString().Trim();

                if (speakerDatas != null)
                {
                    speakerDatas.Add(speakerData);
                }
            }

            data.Items = stringEntries;
            return new SpeakerExportData(data, speakerDatas);
        }

        private int? GetWordCount(StringEntry[] filteredStrings, Locale locale)
        {
            if (filteredStrings == null || filteredStrings.Count() == 0) return 0;

            int count = 0;

            foreach (var item in filteredStrings)
            {
                var lsd = item.Data;

                var lang = lsd.GetLocale(locale);
                if (lang == null || string.IsNullOrEmpty(lang.Text))
                    return 0;

                var text = lang.Text;
                int wordCount = StringUtils.CountTotalWords(text);

                count += wordCount;

            }

            return count;

        }
        protected override void AddTextCells(in ExportData data, SpreadsheetDocument doc, Row row, StringEntry s, ExportResults result)
        {
            if (data is SpeakerExportData exportData)
            {
                var speakerData = exportData.SpeakerDatas.FirstOrDefault(sd => sd.Key == s.Key);

                if (speakerData != null)
                {
                    row.Append(new Cell().SetText(speakerData.StringsDirectories ?? ""));
                    row.Append(new Cell().SetText(!string.IsNullOrEmpty(speakerData.Speaker?.ToString()) ? speakerData.Speaker?.ToString() : "None"));
                    row.Append(CreateNumberCell(speakerData.Count));
                    row.Append(CreateNumberCell(speakerData.StringsWordsCountSourceLocale));
                    row.Append(CreateNumberCell(speakerData.StringsWordsCountTargetLocale));
                    row.Append(CreateNumberCell(speakerData.CueCount));
                    row.Append(CreateNumberCell(speakerData.CueSourceLocaleWordsCount));
                    row.Append(CreateNumberCell(speakerData.CueTargetLocaleWordsCount));
                    row.Append(CreateNumberCell(speakerData.AnswersCount));
                    row.Append(CreateNumberCell(speakerData.AnswersSourceLocaleWordsCount));
                    row.Append(CreateNumberCell(speakerData.AnswersTargetLocaleWordsCount));
                }
            }
        }

        private Cell CreateNumberCell(int? value)
        {
            var cell = new Cell
            {
                DataType = CellValues.Number,
                CellValue = new CellValue((value ?? 0).ToString())
            };
            return cell;
        }
    }
    public class SpeakerExportData : ExportData
    {
        public SpeakerExportData(ExportData data, List<SpeakerData> speakerDatas)
        {
            DistFolder = data.DistFolder;
            Items = data.Items;
            ExportParams = data.ExportParams;
            SpeakerDatas = speakerDatas;
        }

        public List<SpeakerData> SpeakerDatas = new List<SpeakerData>();
    }

    public class SpeakerData
    {
        public string? Speaker;
        public int? Count;
        public int? CueCount;
        public int? AnswersCount;
        public string? Key;
        public int? CueSourceLocaleWordsCount;
        public int? CueTargetLocaleWordsCount;
        public int? AnswersSourceLocaleWordsCount;
        public int? AnswersTargetLocaleWordsCount;
        public int? StringsWordsCountSourceLocale;
        public int? StringsWordsCountTargetLocale;
        public string? StringsDirectories;

    }
}