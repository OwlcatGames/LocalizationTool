using DocumentFormat.OpenXml.Spreadsheet;
using LocalizationTracker.Data;
using LocalizationTracker.Excel;
using StringsCollector.Data;
using StringsCollector.Data.Unity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LocalizationTracker.Logic
{
    public class VoiceCommentsImporter : ExcelImporter
    {
        protected override ImportEntry ImportRow(Row row)
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

                if (StringManager.StringsByKey.TryGetValue(result.Key, out se))
                {
                    if (se.SourceLocaleEntry.Text != "Dialog comment")
                    {
                        se.Reload();
                    }
                    result.Path = se.StringPath;

                    result.CurrentSource = se.Data.GetLocale(m_SourceLocale).VoiceComment;
                    result.CurrentTarget = se.Data.EnsureLocale(m_TargetLocale).VoiceComment;
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

            se.Data.UpdateVoiceCommentTranslation(m_TargetLocale, result.ImportResult);

            foreach (var trait in m_Traits)
            {
                se.Data.AddTrait(m_TargetLocale, trait);
            }

            if (se.SourceLocaleEntry.Text != "Dialog comment")
            {
                se.Save();
            }
            else
            {
                se.DialogsDataList.First().Nodes
                            .First(n => n.Kind == "root")
                            .VOComment[m_TargetLocale] = result.ImportResult;

                if (!string.IsNullOrEmpty(se.DialogsDataList.First().FileSource))
                {
                    string jsonString = JsonSerializer.Serialize(se.DialogsDataList.First(), JsonSerializerHelpers.JsonSerializerOptions);
                    File.WriteAllText(se.DialogsDataList.First().FileSource, jsonString);
                }
            }

            return result;
        }

    }
}
