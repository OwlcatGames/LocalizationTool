using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Math;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using LocalizationTracker.Data;
using LocalizationTracker.Utility;
using System.IO;
using System.Linq;

namespace LocalizationTracker.Excel
{
    public static class SpreadsheetHelper
    {
        public static SpreadsheetDocument? GetSpreadsheet(string path, WrapperMod wrapperMod, ColumnSettings[]? columnDatas = null)
        {
            if (!File.Exists(path))
            {
                if (wrapperMod != WrapperMod.ExportNewFile && wrapperMod != WrapperMod.ExportTemplateFile)
                {
                    throw new System.Exception("Файл не существует и не должен быть создан согласно параметрам");
                }

                CreateDoc(path, wrapperMod == WrapperMod.ExportTemplateFile, columnDatas);
            }
            else if (wrapperMod != WrapperMod.Import || wrapperMod == WrapperMod.ExportAdd)
            {
                var ext = Path.GetExtension(path);
                var dir = Path.GetDirectoryName(path);
                var name = Path.GetFileNameWithoutExtension(path);
                var rndName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
                var newPath = Path.Combine(dir, $"{name}_{rndName}{ext}");
                return GetSpreadsheet(newPath, wrapperMod, columnDatas);
            }

            return OpenSpreadsheet(path, wrapperMod != WrapperMod.Import, wrapperMod);
        }

        static void CreateDoc(string path, bool useTemplate, ColumnSettings[]? columnDatas)
        {
            if (useTemplate)
                CopyTemplateTo(path);
            else
                CreateDefaultSpreadsheet(path, columnDatas);
        }

        static void CopyTemplateTo(string path)
        {
            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "template.xlsx");
            File.Copy(templatePath, path);
        }

        public static SpreadsheetDocument? OpenSpreadsheet(string path, bool isEditable, WrapperMod wrapperMod)
        {
            if (wrapperMod == WrapperMod.Import)
            {
                var tempPath = Path.GetTempPath();
                var pathArray = path.Split("\\");
                var fullPath = Path.Combine(tempPath, pathArray[pathArray.Length - 1]);

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }

                File.Copy(path, fullPath);

                path = fullPath;
            }

            SpreadsheetDocument? doc;

            try
            {
                doc = SpreadsheetDocument.Open(path, isEditable);
            }
            catch (OpenXmlPackageException)
            {
                RepairKit.RepairPathInExportXLSX(path);
                doc = SpreadsheetDocument.Open(path, isEditable);
            }
            catch
            {
                doc = default;
            }

            return doc;
        }

        public static void CreateDefaultSpreadsheet(string filepath, ColumnSettings[]? columnDatas)
        {
            SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.
                Create(filepath, SpreadsheetDocumentType.Workbook);

            WorkbookPart workbookPart = spreadsheetDocument.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            worksheetPart.Worksheet = new Worksheet();
            if (columnDatas != null)
                ExcelStylesCreator.AddCustomColumn(worksheetPart, columnDatas);
            worksheetPart.Worksheet.Append(new SheetData());
            worksheetPart.Worksheet.Save();

            Sheets sheets = workbookPart.Workbook.AppendChild(new Sheets());
            string partId = workbookPart.GetIdOfPart(worksheetPart);

            uint sheetId = 1;
            if (sheets.Elements<Sheet>().Count() > 0)
            {
                sheetId = sheets.Elements<Sheet>().Select(s => s.SheetId.Value).Max() + 1;
            }

            string sheetName = "Sheet" + sheetId;
            Sheet sheet = new Sheet()
            {
                Id = partId,
                SheetId = sheetId,
                Name = sheetName
            };

            sheets.Append(sheet);

            ExcelStylesCreator.AddStylesPart(spreadsheetDocument);

            workbookPart.Workbook.Save();
            spreadsheetDocument.Save();
            spreadsheetDocument.Close();
        }

        public static SheetData CreateNewSheet(SpreadsheetDocument doc, Sheets sheets, StringEntry s, ColumnSettings[]? columnDatas)
        {
            var worksheetPart = doc.WorkbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            var worksheet = new Worksheet(sheetData);
            worksheetPart.Worksheet = worksheet;

            if (columnDatas != null)
                ExcelStylesCreator.AddCustomColumn(worksheetPart, columnDatas);

            uint sheetId = (uint)(sheets.Elements<Sheet>().Count() + 1);
            var firstSheet = new Sheet
            {
                Id = doc.WorkbookPart.GetIdOfPart(worksheetPart),
                SheetId = sheetId,
                Name = GetFolderName(s.AbsolutePath)
            };

            sheets.Append(firstSheet);

            return sheetData;
        }

        public static string GetFolderName(string absolutePath)
        {
            var address = absolutePath.Split("/");

            if (address.Count() > 1)
            {
                return address[address.Count() - 2];
            }

            return "unknown";
        }

        public static Row GetRow(this SheetData sheet, int rowIdx)
        {
            while (sheet.Elements<Row>().Count() is var rowCount && rowCount <= rowIdx)
            {
                sheet.Append(new Row());
            }
            var row = sheet.Elements<Row>().ElementAt(rowIdx);
            return row;
        }

        public static Cell GetCell(this SheetData sheet, int rowIdx, int colIdx)
        {
            var row = GetRow(sheet, rowIdx);

            while (row.Elements<Cell>().Count() is var cellCount && cellCount <= colIdx)
            {
                row.Append(new Cell());
            }
            var cell = row.Elements<Cell>().ElementAt(colIdx);
            return cell;
        }

        public static Cell SetText(this Cell cell, string text)
        {

            cell.DataType = CellValues.String;
            cell.CellValue = new CellValue(text);
            return cell;
        }

        public static Cell SetSharedText(this Cell cell, int sharedTextId)
        {
            cell.DataType = CellValues.SharedString;
            cell.CellValue = new CellValue(sharedTextId);
            return cell;
        }

        public static Cell SetStyle(this Cell cell, CellStyle cellStyle)
        {
            cell.StyleIndex = new UInt32Value((uint)cellStyle);
            return cell;
        }

        public static int AddSharedString(this SpreadsheetDocument doc, SharedStringItem text)
        {
            var sharedTbl = doc.GetSharedTable().SharedStringTable;
            sharedTbl.AppendChild(text);
            var count = sharedTbl.Elements<SharedStringItem>().Count();
            return count - 1;
        }

        public static SheetData GetSheet(this SpreadsheetDocument doc, int sheetId)
        {
            var workSheet = doc.WorkbookPart.WorksheetParts.Single().Worksheet;
            while (workSheet.Elements<SheetData>().Count() is var sheetCount && sheetCount <= sheetId)
            {
                workSheet.Append(new SheetData());
            }
            workSheet.Save();
            var sheet = workSheet.Elements<SheetData>().ElementAt(sheetId);
            return sheet;
        }

        public static string GetCellRef(uint row, int col)
        {
            const char A = 'A';
            char columnChar = A;
            columnChar += (char)col;

            var cellRef = $"{columnChar}{row}";
            return cellRef;
        }

        public static void AppendRow(this SheetData sheet, Row row)
        {
            sheet.Append(row);
            row.RowIndex = (uint)sheet.OfType<Row>().Count();
        }
        public static void AppendCell(this Row row, Cell cell)
        {
            row.Append(cell);
            cell.CellReference = GetCellRef(row.RowIndex ?? 0, row.OfType<Cell>().Count() - 1);
        }

    }
}
