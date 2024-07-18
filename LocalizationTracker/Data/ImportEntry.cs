using LocalizationTracker.Components;
using LocalizationTracker.Tools;

namespace LocalizationTracker.Data
{
	public class ImportEntry
	{
		public ImportStatus Status { get; set; }

		public string Messages { get; set; }

		public string Key { get; set; }

		public string Path { get; set; }

		public string CurrentSource { get; set; }

		public string ImportSource { get; set; }

		public InlinesWrapper SourceDiffs { get; set; }

		public string CurrentTarget { get; set; }

		public string ImportTarget { get; set; }

		public InlinesWrapper TargetDiffs { get; set; }

		public string ImportResult { get; set; }

		public InlinesWrapper ResultDiffs { get; set; }

		public ImportEntry()
		{
			Status = ImportStatus.Ok;
			Messages = "";
			Key = "";
			Path = "";
			CurrentSource = "";
			ImportSource = "";
			SourceDiffs = new InlinesWrapper();
			CurrentTarget = "";
			ImportTarget = "";
			TargetDiffs = new InlinesWrapper();
			ImportResult = "";
			ResultDiffs = new InlinesWrapper();
		}

		public void AddMessage(string message)
		{
			if (Messages == "")
			{
				Messages = message;
			}
			else
			{
				Messages += "\n" + message;
			}
		}

		public void MakeDiffs()
		{
			SourceDiffs = Diff.MakeInlines(ImportSource, CurrentSource);
			TargetDiffs = Diff.MakeInlines(ImportTarget, CurrentTarget);
			ResultDiffs = Diff.MakeInlines(CurrentTarget, ImportResult);
		}
	}

	public enum ImportStatus
	{
		Ok,
		Warning,
		Error
	}
}