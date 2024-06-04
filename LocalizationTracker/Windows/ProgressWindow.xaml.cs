using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace LocalizationTracker.Windows
{
	/// <summary>
	/// Логика взаимодействия для ProgressWindow.xaml
	/// </summary>
	public partial class ProgressWindow : Window, IProgressWindow
	{
		public ProgressWindow(Action<IProgress<ProgressState>> taskAction)
		{
			InitializeComponent();
			TaskRun(taskAction);
		}

		async void TaskRun(Action<IProgress<ProgressState>> taskAction)
		{
			using (var reporter = new ProgressReporter(this))
			{
				await Task.Run(() => taskAction.Invoke(reporter));
				Close();
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
