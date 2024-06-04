using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Spreadsheet;
using LocalizationTracker.Components;
using System.Windows.Media;

namespace LocalizationTracker.Excel
{
	public static class ExcelStyles
	{
		public static SharedStringItem DiffToSharedString(InlinesWrapper inlineWrapper)
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

		static bool CheckRedStrike(InlineTemplate baseRun, Run xmlRun)
		{
			if (baseRun.Background == ColorUtility.Red)
			{
				xmlRun.Append(GetRedStriked());
				return true;
			}

			return false;
		}

		static void CheckColor(InlineTemplate baseRun, Run xmlRun)
		{
			if (baseRun.Background != null)
			{
				var prop = GetColorProp(baseRun.Background == ColorUtility.Green ? ColorUtility.ExportDiffGreen : baseRun.Background.Value);
				xmlRun.Append(prop);
			}
		}

		static RunProperties GetRedStriked()
		{
			RunProperties runProperties1 = new RunProperties();
			var strike = new Strike();
			var color = ColorUtility.MediaColorToOXMLColor(ColorUtility.ExportDiffRed);
			runProperties1.Append(strike);
			runProperties1.Append(color);

			return runProperties1;
		}

		static RunProperties GetColorProp(System.Windows.Media.Color color)
		{
			var colorProp = new RunProperties();
			var redColor = ColorUtility.MediaColorToOXMLColor(color);
			colorProp.Append(redColor);

			return colorProp;
		}
	}
}
