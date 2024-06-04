using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Kingmaker.Localization.Shared;
using LocalizationTracker.Components;
using LocalizationTracker.Data;
using LocalizationTracker.Utility;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace LocalizationTracker.Excel
{
	public class ExcelSourceUpdateExporter : ExcelLocalizationExporter
	{
		public override ExportData PrepeareDataToExport(ExportData data)
		{
			var prevSrcLocale = StringEntry.SourceLocale;
			var prevTrgLocale = StringEntry.TargetLocale;

			StringEntry.SourceLocale = Locale.TranslationSource;
			StringEntry.TargetLocale = data.ExportParams.Target.FirstOrDefault();

			Dictionary<StringEntry, DiffPair> pairs = new Dictionary<StringEntry, DiffPair>(data.Items.Length);
			var builder = new StringBuilder();
			foreach (var line in data.Items)
			{
                line.UpdateInlines();
				if (data.ExportParams.TagRemovalPolicy == TagRemovalPolicy.DeleteUpdatedTag)
                {

                    System.Windows.Media.Color color = new System.Windows.Media.Color
                    {
                        R = 215,
                        G = 227,
                        B = 188,
                        A = 255
                    };

                    var inlines = line.SourceLocaleEntry.Inlines.InlineTemplates.ToList();

                    for (int i = 0; i < inlines.Count; i++)
                    {
                        if (inlines[i].Background == color || inlines[i].StrikeThrough == true)
                        {
                            var text = StringUtils.RemoveTags(inlines[i].Text, LocalizationExporter.TagsEncyclopedia);
                            line.SourceLocaleEntry.Inlines.InlineTemplates[i].Text = text;
                        }
                    }

                }

				var source = ExcelStyles.DiffToSharedString(line.SourceLocaleEntry.Inlines);
				var target = ExcelStyles.DiffToSharedString(line.TargetLocaleEntry.Inlines);
				builder.Clear();
				AddClearText(builder, line.SourceLocaleEntry.Inlines);
				pairs.Add(line, new DiffPair(source, target, builder.ToString()));
			}

			StringEntry.SourceLocale = prevSrcLocale;
			StringEntry.TargetLocale = prevTrgLocale;

			return new DiffExportData(data, pairs);
		}

		void AddClearText(StringBuilder builder, InlinesWrapper inlineWrapper)
		{
			foreach (var inline in inlineWrapper.InlineTemplates)
			{
				builder.Append(inline.Text);
			}
		}

		protected override void AddTextCells(in ExportData data, SpreadsheetDocument doc, Row row, StringEntry s, ExportResults result)
		{
            if (data is DiffExportData exportData && exportData.DiffPairs.TryGetValue(s, out var pair))
			{
				row.Append(new Cell()
				.SetSharedText(doc.AddSharedString(pair.SourceLocale))
				.SetStyle(CellStyle.WordWrap));
				row.Append(new Cell()
				.SetSharedText(doc.AddSharedString(pair.TargetLocaleEntry))
				.SetStyle(CellStyle.WordWrap)
				);
                
				result.AddText(pair.SoruceText);
			}
		}
	}

	public class DiffExportData : ExportData
	{
		public DiffExportData(ExportData data, Dictionary<StringEntry, DiffPair> diffPairs)
		{
			DistFolder = data.DistFolder;
			Items = data.Items;
			ExportParams = data.ExportParams;
			DiffPairs = diffPairs;
		}

		public Dictionary<StringEntry, DiffPair> DiffPairs;
	}

	public class DiffPair
	{
		public DiffPair(SharedStringItem source, SharedStringItem target, string sourceText)
		{
			SourceLocale = source;
			TargetLocaleEntry = target;
			SoruceText = sourceText;
		}

		public string SoruceText { get; }
		public SharedStringItem SourceLocale { get; }
		public SharedStringItem TargetLocaleEntry { get; }
	}
}
