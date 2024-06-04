using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace LocalizationTracker.OpenOffice
{
    public static class OpenOfficeXmlHelper
	{
		public static XmlNode GetTable(this XmlDocument document)
			=> document
					["office:document-content"]?
					["office:body"]?
					["office:spreadsheet"]?
					["table:table"];
		
		public static IEnumerable<XmlNode> GetRows(this XmlNode table)
			=> table.OfType<XmlNode>().Where(n => n.Name == "table:table-row");

		public static IEnumerable<XmlNode> GetCells(this XmlNode row)
			=> row.OfType<XmlNode>().Where(n => n.Name == "table:table-cell");

		public static XmlNode GetText(this XmlNode cell)
			=> cell.OfType<XmlNode>().FirstOrDefault(i => i.Name == "text:p");

		[CanBeNull]
		public static string ReadCellValueAt(this IEnumerable<XmlNode> source, int i)
		{
			int ci = 0;
			foreach (var cell in source)
			{
				var repeatAttribute = cell.Attributes?.GetNamedItem("table:number-columns-repeated");
				if (repeatAttribute != null &&
					int.TryParse(repeatAttribute.InnerText, out int repeat) &&
					repeat > 0)
				{
					ci += repeat;
				}
				else
				{
					ci += 1;
				}

				if (ci > i)
				{
					return cell.GetText()?.InnerText ?? "";
				}
			}

			return null;
		}
	}
}