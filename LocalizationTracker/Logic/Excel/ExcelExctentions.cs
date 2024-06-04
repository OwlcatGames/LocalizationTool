using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Linq;

namespace LocalizationTracker.Excel
{
    public static class ExcelExctentions
    {
        public static SharedStringTablePart GetSharedTable(this SpreadsheetDocument spreadSheet)
        {
            SharedStringTablePart sharedStringPart;
            if (spreadSheet.WorkbookPart.GetPartsOfType<SharedStringTablePart>().Count() > 0)
            {
                sharedStringPart = spreadSheet.WorkbookPart.GetPartsOfType<SharedStringTablePart>().First();
            }
            else
            {
                sharedStringPart = spreadSheet.WorkbookPart.AddNewPart<SharedStringTablePart>();
                sharedStringPart.SharedStringTable = new SharedStringTable();
            }

            return sharedStringPart;
        }
    }
}
