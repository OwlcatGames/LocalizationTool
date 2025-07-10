using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CommandLine;
using LocalizationTracker.Tools.GlossaryTools;
using LocalizationTracker.Tools.SVGTool;
using LocalizationTracker.Utility;
using StringsCollector.Data;
using LocalizationTracker.Windows;
using wpf4gp;

namespace LocalizationTracker
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>



    public partial class App : Application
    {
        private async void App_OnStartup(object sender, StartupEventArgs e)
        {
            Dispatcher.UnhandledException += HandleThreadException;

            AppDomain.CurrentDomain.UnhandledException += HandleGlobalException;

            _ = OwlcatProtocolListener.StartListeningAsync();

            ShutdownMode = ShutdownMode.OnMainWindowClose;

            try
            {
                string configPath = FindConfig(e);
                var jsonText = File.ReadAllText(configPath);
                if (!CheckJsonIsValid(jsonText))
                {
                    jsonText = jsonText.Replace("\\", "/");
                    if (!CheckJsonIsValid(jsonText))
                    {
                        ShowErrorAndShutdown($"Config.json contents seems to be broken. Check that path values do not contain \\ (forwardslashes), replace them with / (backslashes). After fixing config values try to restart the app.");
                        return;
                    }
                    //Maybe we fixed json values, rewrite fixed json back to config.
                    else
                    {
                        File.WriteAllText(configPath, jsonText);
                    }
                }

                var config = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(configPath), JsonSerializerHelpers.JsonSerializerOptions);

                if (config == null)
                {
                    ShowErrorAndShutdown($"Loading config failed. Check that your config is a valid JSON. Path: {configPath}");
                    return;
                }

                if (config != null && string.IsNullOrEmpty(config.StringsFolder))
                {
                    string messageBoxText = "Path to \"Strings\" folder is missing in config file. Do you want to select the folder?";
                    string caption = "Localization Tool";
                    MessageBoxButton button = MessageBoxButton.YesNo;
                    MessageBoxImage icon = MessageBoxImage.Warning;
                    MessageBoxResult result;

                    result = MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);

                    if (result == MessageBoxResult.Yes)
                    {
                        using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
                        {
                            System.Windows.Forms.DialogResult folderResult = dialog.ShowDialog();
                            if (folderResult == System.Windows.Forms.DialogResult.OK)
                            {
                                config.StringsFolder = dialog.SelectedPath;
                                Console.WriteLine($"Selected path {config.StringsFolder}");
                                jsonText = jsonText.Replace("\"StringsFolder\": \"\"", $"\"StringsFolder\": \"{config.StringsFolder}\"");
                                File.WriteAllText(configPath, jsonText);
                            }
                            else
                            {
                                ShowErrorAndShutdown($"config.StringsFolder is empty. You have to set up StringsFolder before using Localization Tool.");
                                return;
                            }
                        }
                    }
                    else
                    {
                        ShowErrorAndShutdown($"Strings folder path is not set in Config.json. Please specify the path and restart the app");
                        return;
                    }

                }

                if (config.StringsFolder.Contains("\\"))
                {
                    config.StringsFolder = config.StringsFolder.Replace("\\", "/");
                }

                if (!Directory.Exists(config.StringsFolder))
                {
                    ShowErrorAndShutdown($"config.StringsFolder does not exist: {config.StringsFolder}");
                    return;
                }

                if (!string.IsNullOrEmpty(config.DialogsFolder) && !Directory.Exists(config.DialogsFolder))
                {
                    ShowErrorAndShutdown($"config.DialogsFoolder does not exist: {config.DialogsFolder}");
                    return;
                }

                if (!config.ModdersVersion && !string.IsNullOrWhiteSpace(config.AssetsFolder) && !Directory.Exists(config.AssetsFolder))
                {
                    MessageBox.Show(
                        $"config.AssetsFolder does not exist: {config.AssetsFolder}.{Environment.NewLine}Remove entry in Config.json to suppress.",
                        "Warning",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }

                if (!config.ModdersVersion && !string.IsNullOrWhiteSpace(config.BlueprintsFolder) && !Directory.Exists(config.BlueprintsFolder))
                {
                    MessageBox.Show(
                        $"config.BlueprintsFolder does not exist: {config.BlueprintsFolder}.{Environment.NewLine}Remove entry in Config.json to suppress.",
                        "Warning",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }

                AppConfig.SetupInstance(config);

                Glossary.SetupInstance();

                if (e.Args.Contains("--generatesvg"))
                    Parser.Default.ParseArguments<GenerateSvgFromConsole>(e.Args).WithParsed<GenerateSvgFromConsole>(async t => await t.Execute());

            }
            catch (Exception ex)
            {
                ShowErrorAndShutdown(ex.Message + "\n" + ex.StackTrace);
            }
        }

        private static string FindConfig(StartupEventArgs e)
        {
            var configIndex = e.Args.IndexAt(arg => arg == "--config");
            var configFile = configIndex > -1
                ? e.Args.Length > configIndex
                    ? e.Args[configIndex]
                    : ""
                : "";

            string[] candidates =  {
                configFile,
                Path.Combine(Directory.GetCurrentDirectory(), "Config.json"),
                Path.Combine(Path.GetDirectoryName(Environment.GetCommandLineArgs().FirstOrDefault()) ?? string.Empty, "Config.json")
            };

            string? configPath = candidates.FirstOrDefault(File.Exists);

            if (configPath != null)
            {
                return configPath;
            }

            var directories = candidates.Aggregate(
                new StringBuilder(1000),
                (builder, s) => (builder.Length > 0 ? builder.Append(Environment.NewLine) : builder).Append(s));
            throw new InvalidOperationException($"Could not find the config in one of the directories\n{directories}");
        }

        private void MainWindow_ScanCompleted(object sender, EventArgs e)
        {
            Console.WriteLine("Scanning completed!");

            var mainWindow = (MainWindow)sender;
            GenerateSVG.FindAllDialogues();
        }

        /// <summary>
        /// This method is used to check JSON validity. 
        /// In case of invalid JSON we don't get cached exception,
        /// so we can try cache the exception in the way we want it.
        /// If we use JsonSerializer.Deserialize inside try block on invalid json,
		/// JsonSerializer cahces exception internally, so our outer cache don't work and it crashes the app.
		/// 
		/// The most common case is when the path in config contans \ instead of /,
		/// then json treats \ not like path separator, but like a special sequence start. It makes json invalid.
		/// But in that case we don't want to crash the app, we want to try fixing the value by replacing \ with /.
        /// </summary>
        /// <param name="jsonText"></param>
        /// <returns></returns>
        private bool CheckJsonIsValid(string jsonText)
        {
            var result = true;
            if (jsonText == null)
                return false;
            try
            {
                using (JsonDocument doc = JsonDocument.Parse(jsonText))
                {
                    // it's just a test
                }
                result = true;
            }
            catch (JsonException)
            {
                result = false;
            }

            return result;
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

        protected override void OnExit(ExitEventArgs e)
        {
            OwlcatProtocolListener.StopListening();
            base.OnExit(e);
        }

    }

    [Verb("generatesvg", HelpText = "Generate SVG files for this project")]
    public class GenerateSvgFromConsole
    {
        [Option('g', "generatesvg")]
        public string Option { get; set; } = "";
        public async Task Execute()
        {
            if (Application.Current.MainWindow is not MainWindow mainWindow)
            {
                mainWindow = new MainWindow();
                Application.Current.MainWindow = mainWindow;
            }

            await mainWindow.scanCompletionSource.Task;
            await GenerateSVG.FindAllDialogues(Option);
            Application.Current.Shutdown();
        }
    }
}
