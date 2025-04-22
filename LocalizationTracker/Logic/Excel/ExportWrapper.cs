using LocalizationTracker.Data;
using LocalizationTracker.Windows;
using System;
using System.Linq;

namespace LocalizationTracker.Logic.Excel
{
	public abstract class ExportWrapper 
	{
		public ExportWrapper(IExporter exporter, ExportData data)
		{
			ExportResult = new ExportRequestResult(data.ExportParams.Source, string.Join(",", data.ExportParams.Target));
			_exporter = exporter;
			_data = data;
		}

		public ExportData _data;

		protected IExporter _exporter;

		protected IProgress<ProgressState> _progressReporter;

		public ExportRequestResult ExportResult { get; private set; }

		public void Export(IProgress<ProgressState> progress)
		{
			_progressReporter = progress;
			ExportInternal();
		}

		protected abstract void ExportInternal();
	}
}
