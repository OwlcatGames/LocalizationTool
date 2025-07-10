using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using LocalizationTracker.Components;
using LocalizationTracker.Data;
using LocalizationTracker.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalizationTracker.Logic.Excel.Exporters
{
    public class ExcelUpdatedTraitExporter : ExcelLocalizationExporter
    {
        ExcelStyles _style;

        public ExcelUpdatedTraitExporter(ExcelStyles excelStyles)
        {
            _style = excelStyles;
        }

        public override ExportData PrepareDataToExport(ExportData data)
        {
            return data;
        }

        protected override void AddTextCells(in ExportData data, SpreadsheetDocument doc, Row row, StringEntry entry, ExportResults result)
        {
            if (entry.SourceLocaleEntry.Inlines != null)
            {
                var sourceSharedString = _style.DiffToSharedString(entry.SourceLocaleEntry.Inlines);

                row.Append(new Cell()
                    .SetSharedText(doc.AddSharedString(sourceSharedString))
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

        protected override void AddHeader(in ExportData data, SheetData sheet)
        {
            var traits = string.Join(", ", data.ExportParams.Traits);

            var row = new Row();
            sheet.AppendRow(row);

            row.AppendCell(new Cell().SetText("key"));
            row.AppendCell(new Cell().SetText("name"));
            row.AppendCell(new Cell().SetText("speaker"));
            row.AppendCell(new Cell().SetText($"source [{data.ExportParams.Source}] trait [{string.Join(", ", data.Traits)}]"));

            if (data.ExportParams.IncludeComment)
            {
                row.AppendCell(new Cell().SetText("comment"));
            }

            row.AppendCell(new Cell().SetText("Shared"));
        }

    }
}
