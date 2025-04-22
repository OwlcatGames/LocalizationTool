using DocumentFormat.OpenXml.Spreadsheet;
using LocalizationTracker.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalizationTracker.Logic.Excel
{
    public interface IExcelStyler
    {
        public SharedStringItem DiffToSharedString(InlinesWrapper inlineWrapper);
        protected void CheckColor(InlineTemplate baseRun, Run xmlRun);
        protected RunProperties GetColorProp(System.Windows.Media.Color color);


    }
}
