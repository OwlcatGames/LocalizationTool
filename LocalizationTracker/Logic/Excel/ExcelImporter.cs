using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Spreadsheet;
using JetBrains.Annotations;
using StringsCollector.Data;
using LocalizationTracker.Components;
using LocalizationTracker.Data;
using LocalizationTracker.Logic;
using LocalizationTracker.Utility;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using static StringsCollector.Data.Unreal.UnrealStringData;

namespace LocalizationTracker.Excel
{
    public class ExcelImporter : IImporter
    {
        protected static readonly string[] s_InvalidTexts =
        {
            "[insert your text here]",
            "[insert translation here]"
        };

        WorkbookWrapper _wrapper;

        protected Locale m_SourceLocale;

        protected Locale m_TargetLocale;

        protected List<string> m_Traits;

        protected string m_KeyColumn;

        protected string m_SourceColumn;

        protected string m_OldTargetColumn;

        protected string m_ResultColumn;

        public ImportResult Import([NotNull] string filePath)
        {
            var results = new List<ImportEntry>();
            using (var wrapper = new WorkbookWrapper(filePath, WrapperMod.Import))
            {
                _wrapper = wrapper;
                if (!wrapper.IsReadyToWork)
                    return default;

                var rows = wrapper.ChildElementsOfCurrentSheet.OfType<Row>().ToList();

                ParseHeader(rows.First(r => r.RowIndex == 1));

                foreach (var row in rows)
                {
                    var result = ImportRow(row);
                    if (result != null && !string.IsNullOrEmpty(result.Key))
                    {
                        result.MakeDiffs();
                        results.Add(result);
                    }
                }

                var importResult = new ImportResult(filePath, $"{m_SourceLocale.Code}:{m_TargetLocale.Code}", results);
                return importResult;
            }
        }

        protected void ParseHeader(Row headerRow)
        {
            if (headerRow == null)
            {
                throw new IOException("Could not find header in excel file.");
            }

            var cells = headerRow.ChildElements.OfType<Cell>().ToList();

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

            m_KeyColumn = keyCell.CellReference.Value.Substring(0, 1);
            m_SourceColumn = sourceCell.CellReference.Value.Substring(0, 1);
            m_OldTargetColumn = currentCell.CellReference.Value.Substring(0, 1);
            m_ResultColumn = resultCell.CellReference.Value.Substring(0, 1);

            var m = Regex.Match(GetCellValue(sourceCell), @"source \[(\w*-?\w*)\]");
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

        protected virtual ImportEntry ImportRow(Row row)
        {
            // header row
            if (row.RowIndex <= 1)
            {
                return null;
            }

            var result = new ImportEntry();
            var cells = row.ChildElements.OfType<Cell>().ToList();
            var keyCell = cells.FirstOrDefault(c => c.CellReference.Value.StartsWith(m_KeyColumn));
            StringEntry se = null;
            if (keyCell != null)
            {
                result.Key = GetCellValue(keyCell);
                if (result.Key == "[context]")
                {
                    return null;
                }
                if (StringManager.StringsByKey.TryGetValue(result.Key, out se))
                {
                    se.Reload();
                    result.Path = se.StringPath;
                    result.CurrentSource = se.Data.GetText(m_SourceLocale);
                    result.CurrentTarget = se.Data.GetText(m_TargetLocale);
                }
            }

            var sourceCell = cells.FirstOrDefault(c => c.CellReference.Value.StartsWith(m_SourceColumn));
            if (sourceCell != null)
            {
                result.ImportSource = GetCellValue(sourceCell);
            }

            var currentCell = cells.FirstOrDefault(c => c.CellReference.Value.StartsWith(m_OldTargetColumn));
            if (currentCell != null)
            {
                result.ImportTarget = GetCellValue(currentCell);
            }
            else
            {
                result.ImportTarget = "";
            }

            var resultCell = cells.FirstOrDefault(c => c.CellReference.Value.StartsWith(m_ResultColumn));
            if (resultCell != null)
            {
                result.ImportResult = GetCellValue(resultCell);
            }

            if (keyCell == null)
            {
                result.Status = ImportStatus.Error;
                result.AddMessage("Could not find 'key' column.");
            }
            else if (se == null)
            {
                result.Status = ImportStatus.Error;
                result.AddMessage("Key doesn't exist in database.");
            }

            if (sourceCell == null)
            {
                result.Status = ImportStatus.Error;
                result.AddMessage("Could not find 'source' column.");
            }

            if (resultCell == null)
            {
                result.Status = ImportStatus.Error;
                result.AddMessage("Could not find 'result' column.");
            }

            bool invalidText = s_InvalidTexts.Any(it => result.ImportResult.Contains(it));
            if (string.IsNullOrEmpty(result.ImportResult))
                invalidText = true;

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
                if (result.ImportTarget.Trim() == result.CurrentTarget)
                {
                    result.Status = ImportStatus.Warning;
                    result.AddMessage("The import target has a space at the end of the string.");
                }
                else if (result.ImportTarget == result.CurrentTarget.Trim())
                {
                    result.Status = ImportStatus.Warning;
                    result.AddMessage("The current target has a space at the end of the string.");
                }
                else
                {
                    result.Status = ImportStatus.Warning;
                    result.AddMessage("Target text was changed");
                }
            }

            if (result.ImportSource != result.CurrentSource)
            {
                if (result.ImportSource.Trim() == result.CurrentSource)
                {
                    result.Status = ImportStatus.Warning;
                    result.AddMessage("The import source has a space at the end of the string.");
                }
                else if (result.ImportSource == result.CurrentSource.Trim())
                {
                    result.Status = ImportStatus.Warning;
                    result.AddMessage("The current source has a space at the end of the string.");
                }
                else
                {
                    result.Status = ImportStatus.Warning;
                    result.AddMessage("Source text was changed");
                }
            }

            result = CheckSymbolsCount(se, result);

            se.Data.UpdateTranslation(m_TargetLocale, result.ImportResult, m_SourceLocale, result.ImportSource);
            foreach (var trait in m_Traits)
            {
                se.Data.AddTrait(m_TargetLocale, trait);
            }

            se.Save();

            return result;
        }

        private ImportEntry CheckSymbolsCount(StringEntry se, ImportEntry result)
        {
            int symbolsCount = result.ImportResult.Length;

            if (result.ImportResult.Contains("|"))
            {
                symbolsCount = StringUtils.CountTotalSymbols(result.ImportResult);
            }

            if (se.Data.Kind == StringKind.DialogAnswer)
            {
                if (symbolsCount > AppConfig.Instance.SymbolsBorders.ShortAnswer)
                {
                    result.Status = ImportStatus.Warning;
                    result.AddMessage($"Character limit exceeded: {symbolsCount}/{AppConfig.Instance.SymbolsBorders.ShortAnswer}");
                }

            }
            else if (se.Data.Kind == StringKind.DialogCue)
            {
                if (m_TargetLocale == "en")
                {
                    if (symbolsCount > AppConfig.Instance.SymbolsBorders.En)
                    {
                        result.Status = ImportStatus.Warning;
                        result.AddMessage($"Character limit exceeded: {symbolsCount}/{AppConfig.Instance.SymbolsBorders.En}");

                    }
                }
                else if (symbolsCount > AppConfig.Instance.SymbolsBorders.Common)
                {
                    result.Status = ImportStatus.Warning;
                    result.AddMessage($"Character limit exceeded: {symbolsCount}/{AppConfig.Instance.SymbolsBorders.Common}");
                }
            }

            return result;
        }

        protected string GetCellValue(Cell cell)
        {
            if (cell.DataType == null)
            {
                return cell.InnerText;
            }

            if (!cell.DataType.HasValue)
            {
                return cell.InnerText;
            }

            if (cell.DataType != CellValues.SharedString)
            {
                return cell.InnerText;
            }

            // For shared strings, look up the value in the
            // shared strings table.
            var stringTable = _wrapper.SharedString;

            // If the shared string table is missing, something 
            // is wrong. Return the index that is in
            // the cell. Otherwise, look up the correct text in 
            // the table.
            if (stringTable != null)
            {
                return
                    stringTable.SharedStringTable
                    .ElementAt(int.Parse(cell.InnerText)).InnerText;
            }

            throw new IOException($"Could not find shared string value {cell.InnerText} in excel document for cell {cell.CellReference}");
        }

    }
}