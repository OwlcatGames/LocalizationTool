using LocalizationTracker.Data;
using LocalizationTracker.Excel;
using LocalizationTracker.Windows;
using System.Collections.Generic;
using System.IO;

namespace LocalizationTracker.Logic.Excel
{
	class ExportHierarchyWrapper : ExportWrapper
	{
		public ExportHierarchyWrapper(IExporter exporter, ExportData data)
			: base(exporter, data) { }

		int _currentFile;
		int _targetFiles;
		float _percentPerFile;

		protected override void ExportInternal()
		{
			var exportEntries = SplitIntoFolders(_data.Items);
			_currentFile = 0;
			_targetFiles = exportEntries.Length;
			_percentPerFile = 1f / _targetFiles;

			var baseRoot = _data.DistFolder;
			foreach (var entry in exportEntries)
			{
				_currentFile++;
				var path = GetExportPath(baseRoot, entry.EntryName, _data.ExportParams.Source.Code, _data.ExportParams.Target[0].Code);
				_data.DistFolder = path;
				_data.Items = entry.Strings;
				var result = _exporter.Export(_data, SendProgress);
				ExportResult.Append(result);
			}
		}

		private void SendProgress(int current, int target)
		{
			var doneFilesProgress = (_currentFile - 1) * _percentPerFile;
			var currentProgress = doneFilesProgress + _percentPerFile * current / target;
			var progressText = $"Export file: {_currentFile}/{_targetFiles}\nExport line: {current}/{target}";
			_progressReporter.Report(new ProgressState(progressText, currentProgress));
		}

		string GetExportPath(string rootPath, string entryFolder, string fromLang, string toLang, bool removeOldFile = true)
		{
			var entryName = Path.GetFileName(entryFolder);
			var folderPath = Path.Combine(rootPath, Path.GetDirectoryName(entryFolder));
			if (!Directory.Exists(folderPath))
				Directory.CreateDirectory(folderPath);

			var fileName = $"{entryName}_{fromLang}_{toLang}.xlsx";
			var filePath = Path.Combine(folderPath, fileName);
			if (removeOldFile && File.Exists(filePath))
				File.Delete(filePath);

			return filePath;
		}

		ExportEntry[] SplitIntoFolders(StringEntry[] selectedStrings)
		{
			var sortedEntries = GetSortedStrings(selectedStrings);
			List<ExportEntry> exportEntries = new List<ExportEntry>();
			foreach (var pair in sortedEntries)
			{
				if (pair.Value.Count > 0)
				{
					var entry = new ExportEntry(pair.Key, pair.Value.ToArray());
					exportEntries.Add(entry);
				}
			}

			return exportEntries.ToArray();
		}

		static Dictionary<string, List<StringEntry>> GetSortedStrings(StringEntry[] selectedStrings)
		{
			Dictionary<string, List<StringEntry>> _folderSortedEntries = new Dictionary<string, List<StringEntry>>();
			foreach (var entry in selectedStrings)
			{
				var strPath = entry.PathRelativeToStringsFolder;
				var folder = Path.GetDirectoryName(strPath);
				if (!_folderSortedEntries.TryGetValue(folder, out var list))
				{
					list = new List<StringEntry>();
					_folderSortedEntries.Add(folder, list);
				}

				list.Add(entry);
			}


			return _folderSortedEntries;
		}
	}

    public class ExportEntry
    {
        public ExportEntry(string name, StringEntry[] strings)
        {
            EntryName = name;
            Strings = strings;
        }

        public string EntryName { get; }
        public StringEntry[] Strings { get; }
    }

}