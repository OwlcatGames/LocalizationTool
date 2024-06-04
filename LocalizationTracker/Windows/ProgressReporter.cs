using System;
using System.Windows;

namespace LocalizationTracker.Windows
{
	public class ProgressReporter : Progress<ProgressState>, IDisposable
	{
		public ProgressReporter(IProgressWindow win)
		{
			_win = win;
			_startTime = DateTime.Now;
			_win.SetVisibility(Visibility.Visible);
			ProgressChanged += OnEvent;
		}

		IProgressWindow _win;

		DateTime _startTime;

		public string TotalOperationTime => (DateTime.Now - _startTime).ToString("hh\\:mm\\:ss");

		void OnEvent(object context, ProgressState state)
		{
			var percent = Math.Round(state.Progress * 100, 2);
			_win.SetProgressText($"{percent}% complete");
			_win.SetProgressValue(percent);
			_win.SetReportText($"{state.Text}\nПрошло времени: {TotalOperationTime}");
		}

		void IDisposable.Dispose()
		{
			ProgressChanged -= OnEvent;
			_win.SetVisibility(Visibility.Hidden);
		}
	}

	public interface IProgressWindow
	{
		void SetProgressText(string text);
		void SetReportText(string text);
		void SetProgressValue(double value);
		void SetVisibility(Visibility visibility);
	}

	public struct ProgressState
	{
		public ProgressState(string text, float progress)
		{
			Text = text;
			Progress = progress;
		}

		public string Text { get; }
		public float Progress { get; }
	}
}
