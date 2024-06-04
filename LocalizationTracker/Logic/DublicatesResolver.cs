using Kingmaker.Localization.Shared;
using LocalizationTracker.Data;
using LocalizationTracker.Utility;
using LocalizationTracker.Windows;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace LocalizationTracker.Logic
{
	public static class DublicatesResolver
	{
		public static List<DublicateInfo> GetCurrentUsing(List<StringEntry> strings)
		{
			var result = new List<DublicateInfo>(strings.Count);
			strings.ForEach(str => result.Add(new DublicateInfo(str, IsUsing(str))));

			return result;
		}

		public static void CheckUsing(List<StringEntry> selected)
		{
			var progressWin = new ProgressWindow(reporter =>
			{
				HashSet<string> checkFail = new HashSet<string>();
				var count = selected.Count;
				for (int i = 0; i < count; i++)
				{
					reporter.Report(new ProgressState($"Check string {i}/{count}", (float)i / count));
					var item = selected[i] as StringEntry;
					try
					{
						var isUsing = IsUsing(item);
						if (!isUsing)
						{
							item.AssetStatus = AssetStatus.NotUsed;
						}
					}
					catch
					{
						checkFail.Add(item.AbsolutePath);
					}
				}

				if (checkFail.Count > 0)
				{
					var path = Path.Combine(Directory.GetCurrentDirectory(), "CheckFails.txt");
					if (File.Exists(path))
						File.Delete(path);
					using(var stream = new StreamWriter(path))
					{
						foreach (var line in checkFail)
							stream.WriteLine(line);
					}
					MessageBox.Show("Некоторы файлы не удалось проверить. Их список в отдельном файле CheckFails.txt");
					WinFormsUtility.OpenFolderAndSelectFile(path);
				}
			});

			progressWin.ShowDialog();
		}

		public static bool IsUsing(StringEntry stringEntry)
		{
			var absPath = stringEntry.AbsolutePath;
			var stringName = Path.GetFileNameWithoutExtension(absPath);
			var lastSpliter = stringName.LastIndexOf('_');
			if (stringName[lastSpliter - 1] == 'm' && stringName[lastSpliter - 2] == '_')
				lastSpliter = lastSpliter - 2;

			stringName = stringName.Substring(0, lastSpliter);
			if (stringName.Contains("Array"))
			{
				var firstDot = stringName.IndexOf('.');
				if (firstDot != -1)
				{
					lastSpliter = stringName.LastIndexOf('_');
					stringName = stringName.Substring(0, lastSpliter);
				}
			}

			var config = AppConfig.Instance;
			var directory = Path.GetDirectoryName(absPath);
			var assetFolder = directory.Replace(config.AbsStringsFolder, config.AbsAssetsFolder);
			var assetPath = Path.Combine(assetFolder, $"{stringName}.asset");

			if (IsAssetHasReference(assetPath, stringEntry.Data.Key))
			{
				return true;
			}

			if (directory.Contains("Mechanics\\Blueprints\\"))
			{
				directory = directory.Replace("Mechanics\\Blueprints\\", string.Empty);
				assetFolder = directory.Replace(config.AbsStringsFolder, config.AbsBlueprintsFolder);
				assetPath = Path.Combine(assetFolder, $"{stringName}.jbp");

				return IsAssetHasReference(assetPath, stringEntry.Data.Key);
			}
			else
			{
				return false;
			}
		}

		public static void Resolve(ResultInfo result, List<DublicateInfo> infos)
		{
			if (result != null)
			{
				var origin = result.Entry;
				foreach (var loc in result.SelectedLocales)
				{
					origin.Data.UpdateText(loc.Key, loc.Value);
				}

				origin.Save();
				origin.UpdateLocaleEntries();
			}

			var dublicates = infos.Select(x => x.Entry).Where(x => x != result?.Entry).ToArray();
			StringManager.DeleteStrings(dublicates);
		}

		private static bool IsAssetHasReference(string assetPath, string guid)
		{
			if (!File.Exists(assetPath))
			{
				return false;
			}

			using var stream = new StreamReader(assetPath);
			while (!stream.EndOfStream)
			{
				var line = stream.ReadLine();
				if (line.Contains(guid))
				{
					return true;
				}
			}

			return false;
		}
	}

	public class ResultInfo
	{
		public StringEntry Entry;
		public Dictionary<Locale, string> SelectedLocales = new Dictionary<Locale, string>();
	}

	public class DublicateInfo
	{
		public DublicateInfo(StringEntry entry, bool isUsing)
		{
			Entry = entry;
			IsUsing = isUsing;

			Locales = new Dictionary<Locale, string>();
			foreach (var locale in Locale.DefaultValues)
			{
				var locEntry = new LocaleEntry(Entry, locale);
				if (!string.IsNullOrEmpty(locEntry.Text))
				{
					Locales.Add(locale, locEntry.Text);
				}
			}
		}
		
		public StringEntry Entry;
		public bool IsUsing;
		public Dictionary<Locale, string> Locales;
	}
}
