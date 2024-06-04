using LocalizationTracker.Logic.Excel;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace LocalizationTracker.Windows
{
	/// <summary>
	/// Interaction logic for ExportResultsWindow.xaml
	/// </summary>
	public partial class ExportResultsWindow : Window, IProgressWindow
	{
		public ExportResultsWindow(Window owner, ExportWrapper exportWrapper)
		{
			_canClose = false;
			Owner = owner;
			_exportWrapper = exportWrapper;
			InitializeComponent();
			RunExport();
		}

		ExportWrapper _exportWrapper;

		bool _canClose = false;

		async void RunExport()
		{
			ResultsText.Visibility = Visibility.Hidden;
			using (var reporter = new ProgressReporter(this))
			{
				await Task.Run(() => _exportWrapper.Export(reporter));
				ExportDone(reporter.TotalOperationTime);
			}
		}

		void ExportDone(string operationTime)
		{
			_canClose = true;
			ResultsText.Text = $"Экспорт занял: {operationTime}\n{_exportWrapper.ExportResult.GenerateText()}";
			ResultsText.Visibility = Visibility.Visible;
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			if (_canClose)
			{
				e.Cancel = true;
				Hide();
			}
			else
			{
				e.Cancel = false;
			}
		}

		void IProgressWindow.SetProgressText(string text)
			=> ProressText.Text = text;

		void IProgressWindow.SetReportText(string text)
			=> ReportText.Text = text;

		void IProgressWindow.SetProgressValue(double value)
			=> ProgressBar.Value = value;

		void IProgressWindow.SetVisibility(Visibility visibility)
		{
			ProressText.Visibility = visibility;
			ProgressBar.Visibility = visibility;
			ReportText.Visibility = visibility;
		}
	}
}
