using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;

namespace LocalizationTracker.Excel
{
    class SheetWrapper
    {
        public SheetWrapper(SheetData sheet, SharedStringTablePart sharedStrings)
        {
            _worksheet = sheet;
            _sharedStrings = sharedStrings;
            IsReadyToWork = _worksheet != null && _sharedStrings != null;
        }

        #region Fields

        private readonly SheetData _worksheet;

        private readonly SharedStringTablePart _sharedStrings;

        uint _rowIndex = 0;

        int _columnIndex = 0;

        Row _currentRow;

        #endregion Fields
        #region Properties

        public bool IsReadyToWork { get; }

        public OpenXmlElementList ChildElements => _worksheet.ChildElements;

        #endregion Properties
        #region Methods

        public void NewRow()
        {
            _rowIndex++;
            _columnIndex = 0;
            _currentRow = _worksheet.CreateOrGetRow(_rowIndex);
        }

        public void AddCell(string text, CellStyle cellStyle)
        {
            var refID = _sharedStrings.GetIDForSharedText(text);
            AddCell(refID, cellStyle);
        }

        public void AddCell(SharedStringItem stringItem, CellStyle cellStyle)
        {
            var refID = _sharedStrings.GetIDForSharedText(stringItem);
            AddCell(refID, cellStyle);
        }

        void AddCell(int refID, CellStyle cellStyle)
        {
            var cellRef = GetCellRef();
            var cell = _currentRow.CreateCellWithSharedText(cellRef, refID);
            cell.StyleIndex = new UInt32Value((uint)cellStyle);
        }

        string GetCellRef()
        {
            if (_currentRow == null)
                throw new Exception("Попытка вставить ячейку в несуществующий столбец");

            return SpreadsheetHelper.GetCellRef(_rowIndex, _columnIndex++);
        }

        #endregion Methods
    }
}
