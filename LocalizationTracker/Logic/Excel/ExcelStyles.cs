using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Spreadsheet;
using LocalizationTracker.Components;
using LocalizationTracker.Logic.Excel;
using System.Windows.Media;

namespace LocalizationTracker.Excel
{
    public class ExcelStyles: IExcelStyler
    {
        public virtual SharedStringItem DiffToSharedString(InlinesWrapper inlineWrapper)
        {
            SharedStringItem item = new SharedStringItem();
            foreach (var inline in inlineWrapper.InlineTemplates)
            {
                var run = inline;
                var xmlRun = new Run();
                if (!CheckRedStrike(run, xmlRun))
                    CheckColor(run, xmlRun);

                var text = new Text() { Space = SpaceProcessingModeValues.Preserve };
                text.Text = run.Text;
                xmlRun.Append(text);
                item.Append(xmlRun);
            }

            return item;
        }

        protected virtual bool CheckRedStrike(InlineTemplate baseRun, Run xmlRun)
        {
            if (baseRun.Background == ColorUtility.Red)
            {
                xmlRun.Append(GetRedStriked());
                return true;
            }

            return false;
        }

        public virtual void CheckColor(InlineTemplate baseRun, Run xmlRun)
        {
            if (baseRun.Background != null)
            {
                var prop = GetColorProp(baseRun.Background == ColorUtility.Green ? ColorUtility.ExportDiffGreen : baseRun.Background.Value);
                xmlRun.Append(prop);
            }
        }

        protected virtual RunProperties GetRedStriked()
        {
            RunProperties runProperties1 = new RunProperties();
            var strike = new Strike();
            var color = ColorUtility.MediaColorToOXMLColor(ColorUtility.ExportDiffRed);
            runProperties1.Append(strike);
            runProperties1.Append(color);

            return runProperties1;
        }

        public virtual RunProperties GetColorProp(System.Windows.Media.Color color)
        {
            var colorProp = new RunProperties();
            var redColor = ColorUtility.MediaColorToOXMLColor(color);
            colorProp.Append(redColor);

            return colorProp;
        }
    }
}
