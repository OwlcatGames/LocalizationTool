using LocalizationTracker.Windows;

namespace LocalizationTracker.Logic.Excel.Wrappers
{
	class ExportSingleFileWrapper : ExportWrapper
	{
		public ExportSingleFileWrapper(IExporter exporter, ExportData data)
			: base(exporter, data) { }

		protected override void ExportInternal()
		{
			var result = _exporter.Export(_data, SendProgress);
			ExportResult.Append(result);
		}

		private void SendProgress(int current, int target)
		{
			var progress = $"Export line: {current}/{target}";
			_progressReporter.Report(new ProgressState(progress, (float)current / target));
		}
	}
}
