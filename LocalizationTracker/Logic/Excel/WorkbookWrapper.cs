using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using LocalizationTracker.Utility;
using System;
using System.Linq;

namespace LocalizationTracker.Excel
{
    public class WorkbookWrapper : IDisposable
    {
        public WorkbookWrapper(string path, WrapperMod wrapperMod, ColumnSettings[]? columnDatas = null)
        {
            _path = path;
            _sheet = SpreadsheetHelper.GetSpreadsheet(path, wrapperMod, columnDatas);
            _wrapperMod = wrapperMod;
            if (_sheet != null)
            {
                try
                {
                    SharedString = _sheet.GetSharedTable();
                    _worksheet = _sheet.WorkbookPart.WorksheetParts.First().Worksheet;
                    var sheetData = _worksheet.GetFirstChild<SheetData>();
                    _currentSheet = new SheetWrapper(sheetData, SharedString);

                    IsReadyToWork = SharedString != null && _worksheet != null && _currentSheet.IsReadyToWork;
                }
                catch
                {
                    _sheet.Dispose();
                    IsReadyToWork = false;
                }
            }
        }

        #region Fields

        private readonly WrapperMod _wrapperMod;

        private readonly string _path;

        SpreadsheetDocument? _sheet;
        private readonly Worksheet _worksheet;

        private readonly SheetWrapper _currentSheet;

        #endregion Fields
        #region Properties

        public OpenXmlElementList ChildElementsOfCurrentSheet => _currentSheet?.ChildElements;

        public SharedStringTablePart SharedString { get; }

        public bool IsReadyToWork { get; }

        public SpreadsheetDocument? Document => _sheet;

        #endregion Properties
        #region Methods

        public void NewRow()
            => _currentSheet.NewRow();

        public void AddCell(string text, CellStyle cellStyle = CellStyle.Base)
            => _currentSheet.AddCell(text, cellStyle);

        public void AddCell(SharedStringItem textItem, CellStyle cellStyle = CellStyle.Base)
            => _currentSheet.AddCell(textItem, cellStyle);

        public void Save()
        {
            SharedString.SharedStringTable?.Save();
            _sheet.WorkbookPart.Workbook.RemoveAllChildren<WorkbookExtensionList>();
            _sheet.WorkbookPart.Workbook.Save();
            _worksheet.Save();
            _sheet.Save();
        }

        void IDisposable.Dispose()
        {
            if (IsReadyToWork)
            {
                _sheet.Dispose();
                if (_wrapperMod != WrapperMod.Import)
                    RepairKit.RemovePrefixs(_path);
            }
        }

        #endregion Methods
    }

    public enum WrapperMod
    {
        Import,
        ExportAdd,
        ExportNewFile,
        ExportTemplateFile
    }

    public enum CellStyle
    {
        Default = 0,
        Base = 1,
        WordWrap = 2,
        YellowSolid = 3,
        RedSolid = 4,
        GreenSolid = 5,
        Context = 6
    }
}
