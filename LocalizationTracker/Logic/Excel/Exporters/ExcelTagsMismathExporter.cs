using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using LocalizationTracker.Components;
using LocalizationTracker.Data;
using LocalizationTracker.Excel;
using LocalizationTracker.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalizationTracker.Logic.Excel.Exporters
{
    public class ExcelTagsMismathExporter : ExcelLocalizationExporter
    {
        ExcelTagMismatchStyle _style;
        public ExcelTagsMismathExporter(ExcelTagMismatchStyle excelStyle) 
        {
            _style = excelStyle;
        }

        public override ExportData PrepareDataToExport(ExportData data)
        {
            foreach (var line in data.Items)
            {
                TagsList.Compare(line.SourceLocaleEntry.TagsList, line.TargetLocaleEntry.TagsList);
                line.UpdateInlines();

            }

            return data;
        }

        protected override void AddTextCells(in ExportData data, SpreadsheetDocument doc, Row row, StringEntry entry, ExportResults result)
        {
            if (entry.SourceLocaleEntry.Inlines != null && entry.TargetLocaleEntry.Inlines != null)
            {
                var sourceSharedString = _style.DiffToSharedString(entry.SourceLocaleEntry.Inlines);
                var targetSharedString = _style.DiffToSharedString(entry.TargetLocaleEntry.Inlines);

                row.Append(new Cell()
                    .SetSharedText(doc.AddSharedString(sourceSharedString))
                    .SetStyle(LocalizationTracker.Excel.CellStyle.WordWrap));

                row.Append(new Cell()
                    .SetSharedText(doc.AddSharedString(targetSharedString))
                    .SetStyle(LocalizationTracker.Excel.CellStyle.WordWrap));

                StringBuilder builder = new StringBuilder();
                AddClearText(builder, entry.SourceLocaleEntry.Inlines);
                result.AddText(builder.ToString());
            }
        }

        private void AddClearText(StringBuilder builder, InlinesWrapper inlineWrapper)
        {
            foreach (var inline in inlineWrapper.InlineTemplates)
            {
                builder.Append(inline.Text);
            }
        }
    }
}

