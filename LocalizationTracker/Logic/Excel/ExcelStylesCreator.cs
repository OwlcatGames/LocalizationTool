using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace LocalizationTracker.Excel
{
    public class ColumnSettings
    {
        public uint MinIndex;
        public uint MaxIndex;
        public int Width;
    }

    public static class ExcelStylesCreator
    {
        public static void AddCustomeColumn(WorksheetPart wsPart, ColumnSettings[] datas)
        {
            Columns columns = new Columns();
            foreach (var data in datas)
            {
                columns.Append(new Column() { Min = data.MinIndex, Max = data.MaxIndex, Width = data.Width, CustomWidth = true });
            }

            wsPart.Worksheet.Append(columns);
        }

        public static void AddStylesPart(SpreadsheetDocument spreadsheet)
        {
            var stylesPart = spreadsheet.WorkbookPart.AddNewPart<WorkbookStylesPart>();
            stylesPart.Stylesheet = new Stylesheet();

            // blank font list
            stylesPart.Stylesheet.Fonts = new Fonts();
            stylesPart.Stylesheet.Fonts.AppendChild(new Font());

            // create fills
            stylesPart.Stylesheet.Fills = new Fills();

            stylesPart.Stylesheet.Fills.AppendChild(new Fill { PatternFill = new PatternFill { PatternType = PatternValues.None } }); // required, reserved by Excel
            stylesPart.Stylesheet.Fills.AppendChild(new Fill { PatternFill = new PatternFill { PatternType = PatternValues.Gray125 } }); // required, reserved by Excel

            // blank border list
            stylesPart.Stylesheet.Borders = new Borders();
            stylesPart.Stylesheet.Borders.AppendChild(new Border());

            // blank cell format list
            stylesPart.Stylesheet.CellStyleFormats = new CellStyleFormats();
            stylesPart.Stylesheet.CellStyleFormats.AppendChild(new CellFormat());

            // cell format list
            stylesPart.Stylesheet.CellFormats = new CellFormats();

            //CellStyle.Default = 0
            stylesPart.Stylesheet.CellFormats.AppendChild(new CellFormat());

            AddCustomStyles(stylesPart);

            stylesPart.Stylesheet.Fonts.Count = (uint)stylesPart.Stylesheet.Fonts.ChildElements.Count;
            stylesPart.Stylesheet.Fills.Count = (uint)stylesPart.Stylesheet.Fills.ChildElements.Count;
            stylesPart.Stylesheet.Borders.Count = (uint)stylesPart.Stylesheet.Borders.ChildElements.Count;
            stylesPart.Stylesheet.CellStyleFormats.Count = (uint)stylesPart.Stylesheet.CellStyleFormats.ChildElements.Count;
            stylesPart.Stylesheet.CellFormats.Count = (uint)stylesPart.Stylesheet.CellFormats.ChildElements.Count;

            stylesPart.Stylesheet.Save();
        }

        static void AddCustomStyles(WorkbookStylesPart stylesPart)
        {
            CreateBaseStyle(stylesPart);
            CreateWordWrapStyle(stylesPart);
            CreateYellowSolidStyle(stylesPart);
            CreateRedSolidStyle(stylesPart);
            CreateGreenSolidStyle(stylesPart);
            CreateContextStyle(stylesPart);
        }

        //CellStyle.Base = 1
        static void CreateBaseStyle(WorkbookStylesPart stylesPart)
        {
            var baseFormat = new CellFormat { FormatId = 0, FontId = 0, BorderId = 0, FillId = 0, ApplyFill = false };
            var baseFormatAlligment = new Alignment
            {
                Horizontal = HorizontalAlignmentValues.Left,
                Vertical = VerticalAlignmentValues.Center,
            };
            baseFormat.AppendChild(baseFormatAlligment);
            stylesPart.Stylesheet.CellFormats.AppendChild(baseFormat);
        }

        //CellStyle.WordWrap = 2
        static void CreateWordWrapStyle(WorkbookStylesPart stylesPart)
        {
            var worldWrapFormat = new CellFormat { FormatId = 0, FontId = 0, BorderId = 0, FillId = 0, ApplyFill = false };
            var alligment = new Alignment
            {
                Horizontal = HorizontalAlignmentValues.Left,
                Vertical = VerticalAlignmentValues.Top,
                WrapText = true
            };
            worldWrapFormat.AppendChild(alligment);
            stylesPart.Stylesheet.CellFormats.AppendChild(worldWrapFormat);
        }

        //CellStyle.YellowSolid = 3
        static void CreateYellowSolidStyle(WorkbookStylesPart stylesPart)
        {
            var solidYellow = new PatternFill() { PatternType = PatternValues.Solid };
            solidYellow.ForegroundColor = new ForegroundColor { Rgb = ColorUtility.MediaColorToHEX(ColorUtility.ExportDiffYellow) };
            solidYellow.BackgroundColor = new BackgroundColor { Indexed = 64 };
            stylesPart.Stylesheet.Fills.AppendChild(new Fill { PatternFill = solidYellow });

            var redSolidFormat = new CellFormat { FormatId = 0, FontId = 0, BorderId = 0, FillId = 2, ApplyFill = true };
            redSolidFormat.AppendChild(new Alignment { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center });
            stylesPart.Stylesheet.CellFormats.AppendChild(redSolidFormat);
        }

        //CellStyle.RedSolid = 4
        static void CreateRedSolidStyle(WorkbookStylesPart stylesPart)
        {
            var solidRed = new PatternFill() { PatternType = PatternValues.Solid };
            solidRed.ForegroundColor = new ForegroundColor { Rgb = ColorUtility.MediaColorToHEX(ColorUtility.ExportDiffRed) };
            solidRed.BackgroundColor = new BackgroundColor { Indexed = 64 };
            stylesPart.Stylesheet.Fills.AppendChild(new Fill { PatternFill = solidRed });

            var yellowSolidFormat = new CellFormat { FormatId = 0, FontId = 0, BorderId = 0, FillId = 3, ApplyFill = true };
            yellowSolidFormat.AppendChild(new Alignment { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center });
            stylesPart.Stylesheet.CellFormats.AppendChild(yellowSolidFormat);
        }

        //CellStyle.GreenSolid = 5
        static void CreateGreenSolidStyle(WorkbookStylesPart stylesPart)
        {
            var solidGreen = new PatternFill() { PatternType = PatternValues.Solid };
            solidGreen.ForegroundColor = new ForegroundColor { Rgb = ColorUtility.MediaColorToHEX(ColorUtility.ExportDiffGreen) };
            solidGreen.BackgroundColor = new BackgroundColor { Indexed = 64 };
            stylesPart.Stylesheet.Fills.AppendChild(new Fill { PatternFill = solidGreen });

            var greenSolidFormat = new CellFormat { FormatId = 0, FontId = 0, BorderId = 0, FillId = 4, ApplyFill = true };
            greenSolidFormat.AppendChild(new Alignment { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center });
            stylesPart.Stylesheet.CellFormats.AppendChild(greenSolidFormat);
        }

        //CellStyle.Context = 6
        static void CreateContextStyle(WorkbookStylesPart stylesPart)
        {
            var solidYellow = new PatternFill() { PatternType = PatternValues.Solid };
            solidYellow.ForegroundColor = new ForegroundColor { Rgb = ColorUtility.MediaColorToHEX(ColorUtility.ContextYellow) };
            solidYellow.BackgroundColor = new BackgroundColor { Indexed = 64 };
            stylesPart.Stylesheet.Fills.AppendChild(new Fill { PatternFill = solidYellow });

            var contextFormat = new CellFormat { FormatId = 0, FontId = 0, BorderId = 0, FillId = 5, ApplyFill = true };
            contextFormat.AppendChild(new Alignment { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center });
            
            stylesPart.Stylesheet.CellFormats.AppendChild(contextFormat);
        }
    }
}
