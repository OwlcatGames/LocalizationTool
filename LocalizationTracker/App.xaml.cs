using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using LocalizationTracker.Tools.GlossaryTools;
using LocalizationTracker.Utility;
using wpf4gp;

namespace LocalizationTracker
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private void App_OnStartup(object sender, StartupEventArgs e)
		{
            Dispatcher.UnhandledException += HandleThreadException;

            AppDomain.CurrentDomain.UnhandledException += HandleGlobalException;
            
			ShutdownMode = ShutdownMode.OnMainWindowClose;
			
			string configPath;
			if (e.Args.Length > 0)
			{
				configPath = e.Args[0];
			}
			else
			{
				string exePath = Environment.GetCommandLineArgs().FirstOrDefault();
				string exeDir = Path.GetDirectoryName(exePath);
				configPath = Path.Combine(exeDir, "config.json");
			}

			if (!File.Exists(configPath))
			{
				ShowErrorAndShutdown($"Can't find config: {configPath}");
				return;
			}

			try
			{
				var config = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(configPath), JsonSerializerHelpers.JsonSerializerOptions);
				if (!Directory.Exists(config.StringsFolder))
				{
					ShowErrorAndShutdown($"config.StringsFolder does not exist: {config.StringsFolder}");
					return;
				}

                if (!config.ModdersVersion && !string.IsNullOrWhiteSpace(config.AssetsFolder) && !Directory.Exists(config.AssetsFolder))
				{
					MessageBox.Show($"config.AssetsFolder does not exist: {config.AssetsFolder}.{Environment.NewLine}Remove entry in config.json to suppress.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
				}

				if (!config.ModdersVersion && !string.IsNullOrWhiteSpace(config.BlueprintsFolder) && !Directory.Exists(config.BlueprintsFolder))
				{
					MessageBox.Show($"config.BlueprintsFolder does not exist: {config.BlueprintsFolder}.{Environment.NewLine}Remove entry in config.json to suppress.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
				}

				AppConfig.SetupInstance(config);
				Glossary.SetupInstance();
				
			}
			catch (Exception ex)
			{
				ShowErrorAndShutdown(ex.Message + "\n" + ex.StackTrace);
			}
		}

		private void ShowErrorAndShutdown(string error)
		{
			MessageBox.Show(error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			Shutdown();
		}
        private static void HandleThreadException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var ex = e.Exception;
            if (ex == null)
            {
                ex = new NullReferenceException("unhandled exception is null");
            }

            ReportException(ex, false, "thread");
        }

        private static void HandleGlobalException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            if (ex == null)
            {
                ex = new NullReferenceException("unhandled exception is null");
            }

            ReportException(ex, e.IsTerminating, "global");
        }

        private static void ReportException(Exception ex, bool fatal, string context)
        {
            var sb = new StringBuilder();
            var type = fatal ? "fatal" : "unhandled";
            sb.AppendLine()
                .Append($"{type} {context} exception\n")
                .Append(ex)
                .AppendLine();

            File.AppendAllText("error.log", sb.ToString());

            if (fatal)
            {
                ex.ShowMessageBox("Fatal error.", "Unhandled exception");
            }
            else
            {
                ex.ShowMessageBox("Unexpected error.", "Unhandled exception");
            }
        }

    }
}
