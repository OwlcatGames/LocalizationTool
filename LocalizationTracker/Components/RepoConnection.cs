using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Serilog;


namespace LocalizationTracker.Components
{
    public class LastUpdate
    {
        [JsonInclude]
        public string Trait { get; set; }
    }

    public class RepoConnection
    {
        Authorization Authorization = new Authorization();
        public async Task<string> ExecuteSVNCommand(string arguments)
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            string logFolderPath = Path.Combine(appDataPath, "LocalizationTracker", "Log", $"log_{DateTime.Today.ToShortDateString()}.txt");

            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(logFolderPath)
                .CreateLogger();

            Authorization.Auth config = new Authorization.Auth();
            config = await Authorization.AuthentificationAsync();

            {
                string? output = null;

                try
                {
                    var processStartInfo = new ProcessStartInfo
                    {
                        FileName = "svn.exe",
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardError = true,
                    };

                    using (var process = new Process { StartInfo = processStartInfo })
                    {
                        process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.CreateNoWindow = true;

                        process.Start();

                        using (StreamReader reader = process.StandardOutput)
                        {
                            output = reader.ReadToEnd();
                        }

                        using (StreamReader errorReader = process.StandardError)
                        {
                            string error = await errorReader.ReadToEndAsync();
                            if (!string.IsNullOrEmpty(error))
                            {
                                Log.Error($"SVN Error: {error}");
                            }
                        }

                        process.WaitForExit();
                    }
                }
                catch (Exception ex)
                {
                }

                return output;
            }

        }

        public async Task<string> ExecuteGitCommand(string arguments)
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            string logFolderPath = Path.Combine(appDataPath, "LocalizationTracker", "Log", $"log_{DateTime.Today.ToShortDateString()}.txt");

            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(logFolderPath)
                .CreateLogger();

            Authorization.Auth config = new Authorization.Auth();
            config = await Authorization.AuthentificationAsync();

            {
                string? output = null;

                try
                {
                    var processStartInfo = new ProcessStartInfo
                    {
                        FileName = "git.exe",
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardError = true,
                        WorkingDirectory = Path.GetFullPath(AppConfig.Instance.DialogsFolder)
                    };

                    using (var process = new Process { StartInfo = processStartInfo })
                    {
                        process.Start();

                        // Чтение результата
                        using (StreamReader reader = process.StandardOutput)
                        {
                            output = await reader.ReadToEndAsync();
                        }

                        // Чтение ошибок
                        using (StreamReader errorReader = process.StandardError)
                        {
                            string error = await errorReader.ReadToEndAsync();
                            if (!string.IsNullOrEmpty(error))
                            {
                                Log.Error($"Git Error: {error}");
                                Console.WriteLine($"Git Error: {error}");
                            }
                        }

                        process.WaitForExit();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Exception: {ex.Message}");
                }

                return output;
            }

        }
    }
}
