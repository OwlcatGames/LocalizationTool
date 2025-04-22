using LocalizationTracker.Data;
using LocalizationTracker.Logic;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace LocalizationTracker.OpenOffice
{
    public class OpenOfficeImporter : IImporter
	{
		private static readonly string[] s_InvalidTexts =
		{
			"[insert your text here]",
			"[insert translation here]"
		};

		private Locale m_SourceLocale;

		private Locale m_TargetLocale;

		private List<string> m_Traits;

		public ImportResult Import(string filePath)
		{
			var results = new List<ImportEntry>();
			using (var document = ZipFile.Open(filePath, ZipArchiveMode.Read))
			{
				using (var contentStream = document.GetEntry("content.xml")?.Open())
				{
					if (contentStream == null)
					{
						throw new Exception($"Can't find 'content.xml' file in {filePath}");
					}

					var contentXml = new XmlDocument();
					contentXml.Load(contentStream);

					var tableXml = contentXml.GetTable();

					var rows = tableXml.GetRows().ToList();
					ParseHeader(rows.First());

					foreach (var row in rows.Skip(1))
					{
						var result = ImportRow(row);
						if (result != null)
						{
							result.MakeDiffs();
							results.Add(result);
						}
					}

					var importResult = new ImportResult(filePath, $"{m_SourceLocale.Code}:{m_TargetLocale.Code}", results);
					return importResult;
				}
			}
		}

		private void ParseHeader(XmlNode headerRow)
		{
			if (headerRow == null)
			{
				throw new IOException("Could not find header in excel file.");
			}

			var cells = headerRow.GetCells();

			var keyCell = cells.FirstOrDefault(c => GetCellValue(c) == "key");
			if (keyCell == null)
			{
				throw new IOException("Could not find 'key' column in header.");
			}

			var sourceCell = cells.FirstOrDefault(c => GetCellValue(c).StartsWith("source ["));
			if (sourceCell == null)
			{
				throw new IOException("Could not find 'source' column in header.");
			}

			var currentCell = cells.FirstOrDefault(c => GetCellValue(c).StartsWith("current ["));
			if (currentCell == null)
			{
				throw new IOException("Could not find 'current' column in header.");
			}

			var resultCell = cells.FirstOrDefault(c => GetCellValue(c).StartsWith("result ["));
			if (resultCell == null)
			{
				throw new IOException("Could not find 'result' column in header.");
			}

			var m = Regex.Match(GetCellValue(sourceCell), @"source \[(\w*)\]");
			m_SourceLocale = m.Groups[1].Value;
			
			m = Regex.Match(GetCellValue(currentCell), @"current \[(\w*)\]");
			m_TargetLocale = m.Groups[1].Value;

			m = Regex.Match(GetCellValue(resultCell), @"result \[(\w*)[,:]([\w\s,]*)\]");
			var resultLocale = m.Groups[1].Value;
			if (resultLocale != m_TargetLocale)
			{
				throw new IOException($"Locales in 'current' [{m_TargetLocale}] and 'result' [{resultLocale}] headers do not match.");
			}
			if (m.Groups[2].Value != "")
			{
				m_Traits = m.Groups[2].Value
					.Split(',')
					.Select(t => t.Trim())
					.ToList();
			}
			else
			{
				m_Traits = new List<string>();
			}
		}

		private ImportEntry ImportRow(XmlNode row)
		{
			var result = new ImportEntry();
			var cells = row.GetCells().ToList();
			
			result.Key = cells.ReadCellValueAt(0);
			if (result.Key == "[context]")
			{
				return null;
			}

			StringEntry se = null;
			if (result.Key != null && StringManager.StringsByKey.TryGetValue(result.Key, out se))
			{
				se.Reload();
				result.Path = se.StringPath;
				result.CurrentSource = se.Data.GetText(m_SourceLocale);
				result.CurrentTarget = se.Data.GetText(m_TargetLocale);
			}

			result.ImportSource = cells.ReadCellValueAt(3);
			result.ImportTarget = cells.ReadCellValueAt(4) ?? "";
			result.ImportResult = cells.ReadCellValueAt(5);

			if (result.Key == null)
			{
				result.Status = ImportStatus.Error;
				result.AddMessage("Could not find 'key' column.");
			}
			else if (se == null)
			{
				result.Status = ImportStatus.Error;
				result.AddMessage("Key doesn't exist in database.");
			}

			if (result.ImportSource == null)
			{
				result.Status = ImportStatus.Error;
				result.AddMessage("Could not find 'source' column.");
			}

			if (result.ImportResult == null)
			{
				result.Status = ImportStatus.Error;
				result.AddMessage("Could not find 'result' column.");
			}

			bool invalidText =
				string.IsNullOrEmpty(result.ImportResult) ||
				s_InvalidTexts.Any(it => result.ImportResult.Contains(it));
			if (invalidText)
			{
				result.Status = ImportStatus.Error;
				result.AddMessage("Invalid result text.");
			}

			if (se == null || result.Status == ImportStatus.Error)
			{
				result.Status = ImportStatus.Error;
				return result;
			}

			if (result.ImportTarget != result.CurrentTarget)
			{
				result.Status = ImportStatus.Warning;
				result.AddMessage("Target text was changed");
			}

			if (result.ImportSource != result.CurrentSource)
			{
				result.Status = ImportStatus.Warning;
				result.AddMessage("Source text was changed");
			}

			se.Data.UpdateTranslation(m_TargetLocale, result.ImportResult, m_SourceLocale, result.ImportSource);
			foreach (var trait in m_Traits)
			{
				se.Data.AddTrait(m_TargetLocale, trait);
			}

			se.Save();

			return result;
		}

		private static string GetCellValue(XmlNode cell)
		{
			return cell.GetText()?.InnerText ?? "";
		}
	}
}