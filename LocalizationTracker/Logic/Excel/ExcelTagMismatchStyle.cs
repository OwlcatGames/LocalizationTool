using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Spreadsheet;
using LocalizationTracker.Components;
using LocalizationTracker.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalizationTracker.Logic.Excel
{
    public class ExcelTagMismatchStyle : ExcelStyles
    {
        public override SharedStringItem DiffToSharedString(InlinesWrapper inlineWrapper)
        {
            SharedStringItem item = new SharedStringItem();
            foreach (var inline in inlineWrapper.InlineTemplates)
            {
                var run = inline;
                var xmlRun = new Run();

                CheckColor(run, xmlRun);

                var text = new Text() { Space = SpaceProcessingModeValues.Preserve };
                text.Text = run.Text;
                xmlRun.Append(text);
                item.Append(xmlRun);
            }

            return item;
        }

        public override void CheckColor(InlineTemplate baseRun, Run xmlRun)
        {
            if (baseRun.Foreground != null && baseRun.Background == null)
            {
                var foreProp = GetColorProp(baseRun.Foreground == ColorUtility.Green ? ColorUtility.ExportDiffGreen : baseRun.Foreground.Value);
                xmlRun.Append(foreProp);
            }

            if (baseRun.Background != null)
            {
                var backProp = GetColorProp(System.Windows.Media.Colors.Orange);
                xmlRun.Append(backProp);
            }
        }

        public override RunProperties GetColorProp(System.Windows.Media.Color color)
        {
            var colorProp = new RunProperties();
            var redColor = ColorUtility.MediaColorToOXMLColor(color);

            colorProp.Append(redColor);

            if (color == System.Windows.Media.Colors.Red || color == System.Windows.Media.Colors.Orange)
            {
                colorProp.Append(new Bold());
            }

            return colorProp;
        }

    }
}
