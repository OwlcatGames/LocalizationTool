using LocalizationTracker.Windows;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Serilog;
using Serilog.Sinks;
using System.Windows;

namespace LocalizationTracker.Components
{
    public class Authorization
    {
        public struct Auth
        {
            public string Base64Credentials { get; set; }
        }

        bool IsAuthentificated = false;

        public async Task<Auth> AuthentificationAsync()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            string logFolderPath = Path.Combine(appDataPath, "LocalizationTracker", "Log", $"log_{DateTime.Today.ToShortDateString()}.txt");

            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(logFolderPath)
                .CreateLogger();

            Auth config = new Auth();

            while (IsAuthentificated == false)
            {
                config = await GetAuthentificationData();
            }

            return config;
        }

        public async Task<Auth> GetAuthentificationData()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            string authFolderPath = Path.Combine(appDataPath, "SmartLocCommit", "AuthData");

            if (!Directory.Exists(authFolderPath))
            {
                Directory.CreateDirectory(authFolderPath);
            }

            string authPath = Path.Combine(authFolderPath, "auth.json");

            if (!File.Exists(authPath))
            {
                File.WriteAllText(authPath, "{\"Base64Credentials\": \"\" }");
            }

            var config = JsonSerializer.Deserialize<Auth>(File.ReadAllText(authPath));
            bool check = await CheckAuthentification(config);


                if (string.IsNullOrEmpty(config.Base64Credentials) || !check)
                {
                    var authWindow = new AuthorizationWindow();

                    bool? result = authWindow.ShowDialog();

                    if (result == true)
                    {
                        string username = authWindow.Username;
                        string password = authWindow.Password;

                        config.Base64Credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));

                        var authSer = JsonSerializer.Serialize(config);
                        File.WriteAllText(authPath, authSer);
                    }
                }

            return config;
        }

        public async Task<bool> CheckAuthentification(Auth auth)
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://jira.owlcat.local");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Basic", auth.Base64Credentials);

                try
                {
                    HttpResponseMessage response = await client.GetAsync("rest/api/2/search?jql=created%20>=%20startOfDay()%20ORDER%20BY%20created%20ASC&maxResults=1");

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        Log.Information("Authentication completed successfully");
                        MessageBox.Show("Authentication completed successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        IsAuthentificated = true;
                        return true;
                    }
                    else
                    {
                        string errorMessage = $"Error executing request:{response.StatusCode}";
                        Log.Error(errorMessage);
                        MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                    }

                    return false;
                }
                catch (Exception ex)
                {
                    string errorMessage = $"Error executing request:{ex.Message}";
                    Log.Error(errorMessage);
                    MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }

        }

    }
}
