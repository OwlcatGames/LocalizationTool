using LocalizationTracker.Logic;
using LocalizationTracker.Windows;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Text;

namespace LocalizationTracker
{
    public class OwlcatProtocolListener
    {
        static Guid _guid = Guid.NewGuid();
        static HttpListener _listener = new HttpListener();
        private static bool _isStopped = false;

        public static async Task StartListeningAsync(int port = 35556)
        {
            string[] args = new[] { $"http://+:{port}/" };

            if (args == null || args.Length == 0)
                throw new ArgumentException("args");

            _listener = new HttpListener();

            foreach (string s in args)
            {
                _listener.Prefixes.Add(s);
            }

            _listener.Start();

            while (true)
            {
                Console.WriteLine("Listening...");
                HttpListenerContext context = await _listener.GetContextAsync();
                HttpListenerResponse response = context.Response;

                using (var reader = new StreamReader(context.Request.InputStream))
                {
                    // Проверяем, если запрос на получение статуса
                    if (context.Request.HttpMethod == "GET" && context.Request.Url.LocalPath == "/Status")
                    {
                        Status(response);
                    }
                    else
                    {
                        // Читаем тело запроса
                        string requestBody = await reader.ReadToEndAsync();
                        var commandInfo = JsonSerializer.Deserialize<ExecuteCommandInfo>(requestBody);

                        // Проверяем, если это POST запрос с командой
                        if (context.Request.HttpMethod == "POST" && context.Request.Url.LocalPath == "/Command")
                        {
                            if (commandInfo.Args != null && !string.IsNullOrEmpty(commandInfo.Args.FirstOrDefault()))
                            {
                                string path = commandInfo.Args.FirstOrDefault();
                                Console.WriteLine("Команда OpenString получена");
                                Console.WriteLine($"Path: {path}");

                                await OpenString(response, path); // Передаем путь для дальнейшей обработки
                                response.OutputStream.Close();
                            }
                            else
                            {
                                // Если отсутствует параметр 'path'
                                response.StatusCode = (int)HttpStatusCode.BadRequest;
                                byte[] buffer = Encoding.UTF8.GetBytes("Missing 'path' parameter");
                                response.ContentLength64 = buffer.Length;
                                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                            }
                        }
                        else
                        {
                            // Неверный метод или путь запроса
                            response.StatusCode = (int)HttpStatusCode.NotFound;
                            byte[] buffer = Encoding.UTF8.GetBytes("Unsupported request");
                            response.ContentLength64 = buffer.Length;
                            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                        }
                    }
                }

            }
        }

        public struct ProcessStatus
        {
            public Guid Guid { get; set; }
            public bool IsPlaying { get; set; }
            public int ProcessID { get; set; }
            public string ServiceType { get; set; }
            public IList<string> SupportedCommands { get; set; }
        }

        private struct StringURL
        {
            public string Url { get; set; }
        }

        private static ProcessStatus GetStatus()
        {
            return new ProcessStatus()
            {
                Guid = _guid,
                IsPlaying = true,
                ServiceType = "loctool",
                ProcessID = Process.GetCurrentProcess().Id
            };

        }

        private static void Status(HttpListenerResponse resp)
        {
            resp.ContentType = "application/json";
            resp.StatusCode = (int)HttpStatusCode.OK;

            var results = GetStatus();
            var json = System.Text.Json.JsonSerializer.Serialize(results);

            using (var stream = new StreamWriter(resp.OutputStream))
            {
                stream.Write(json);
            }

            resp.Close();
        }

        private static async Task OpenString(HttpListenerResponse resp, string path)
        {
            resp.ContentType = "application/json";
            resp.StatusCode = (int)HttpStatusCode.OK;

            if (AppConfig.Instance.Project == "Expance")
            {
                path = path.Replace(".json", "");
            }

            StringsFilter.Filter.Name = path.Substring(path.IndexOf("=") + 1);
            StringsFilter.Filter.ForceUpdateFilter();

            string result = $"Processed path: {path}";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(result);
            resp.ContentLength64 = buffer.Length;
            await resp.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        }


        public static void StopListening()
        {
            if (_listener != null)
            {
                try
                {
                    _listener.Stop();
                    _listener.Close();
                }
                catch (ObjectDisposedException ex)
                {
                    Console.WriteLine($"[StopListening] ObjectDisposedException: {ex.Message}");
                }
                finally
                {
                    _isStopped = true;
                }
            }
        }

        public struct ExecuteCommandInfo
        {
            public string CommandName { get; set; }
            public string[] Args { get; set; }
        }
    }
}
