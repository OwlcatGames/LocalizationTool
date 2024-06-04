using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Linq;

namespace LocalizationTracker.Excel
{
    public static class SheetDataExtentions
    {
        public static Cell CreateCellWithSharedText(this Row row, string cellRef, int sharedTextIndex)
        {
            Cell cell = CreateOrGetCellInRow(cellRef, row);
            cell.CellValue = new CellValue(sharedTextIndex.ToString());
            cell.DataType = new EnumValue<CellValues>(CellValues.SharedString);

            return cell;
        }

        public static Cell CreateCellWithText(this Row row, string cellRef, string text)
        {
            Cell cell = CreateOrGetCellInRow(cellRef, row);
            cell.CellValue = new CellValue(text);
            cell.DataType = new EnumValue<CellValues>(CellValues.SharedString);

            return cell;
        }

        public static Cell CreateEmptyCell(this Row row, string cellRef)
        {
            Cell cell = CreateOrGetCellInRow(cellRef, row);
            return cell;
        }

        public static Row CreateOrGetRow(this SheetData sheetData, uint rowIndex)
        {
            /*if (!sheetData.Elements<Row>().TryFind(c => c.RowIndex.Value == rowIndex, out var row))
            {
                row = new Row() { RowIndex = rowIndex };
                sheetData.Append(row);
            }*/

            var row = new Row() { RowIndex = rowIndex };
            sheetData.Append(row);
            return row;
        }

        static Cell CreateOrGetCellInRow(string cellReference, Row row)
        {
            /*Cell refCell = null;
            foreach (var cell in row.Elements<Cell>())
            {
                if (cell.CellReference.Value == cellReference)
                    return cell;
                if (string.Compare(cell.CellReference.Value, cellReference, true) > 0)
                {
                    refCell = cell;
                    break;
                }
            }

            var newCell = new Cell() { CellReference = cellReference };
            row.InsertBefore(newCell, refCell);*/

            var newCell = new Cell() { CellReference = cellReference };
            row.InsertBefore(newCell, null);
            return newCell;
        }

        public static int NextRowID = 0;

        public static int GetIDForSharedText(this SharedStringTablePart shareStringPart, string text)
        {
            if (shareStringPart.SharedStringTable == null)
            {
                shareStringPart.SharedStringTable = new SharedStringTable();
            }

            //int count = shareStringPart.SharedStringTable.Elements<SharedStringItem>().Count();
            shareStringPart.SharedStringTable.AppendChild(new SharedStringItem(new Text(text)));
            return NextRowID++;
            //return count;

            //int i = 0;
            //foreach (SharedStringItem item in shareStringPart.SharedStringTable.Elements<SharedStringItem>())
            //{
            //    if (item.InnerText == text)
            //    {
            //        return i;
            //    }

            //    i++;
            //}

            //shareStringPart.SharedStringTable.AppendChild(new SharedStringItem(new Text(text)));
            //return i;
        }

        public static int GetIDForSharedText(this SharedStringTablePart shareStringPart, SharedStringItem textItem)
        {
            if (shareStringPart.SharedStringTable == null)
            {
                shareStringPart.SharedStringTable = new SharedStringTable();
            }

            //int count = shareStringPart.SharedStringTable.Elements<SharedStringItem>().Count();
            shareStringPart.SharedStringTable.AppendChild(textItem);
            return NextRowID++;
            //return count;

            //int i = 0;
            //foreach (SharedStringItem item in shareStringPart.SharedStringTable.Elements<SharedStringItem>())
            //{
            //    if (item.InnerText == textItem.InnerText)
            //    {
            //        return i;
            //    }

            //    i++;
            //}

            //shareStringPart.SharedStringTable.AppendChild(textItem);
            //return i;
        }
    }
}