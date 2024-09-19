using LocalizationTracker.Logic;
using LocalizationTracker.Windows;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace LocalizationTracker
{
    public class OwlcatProtocolListener
    {
        static Guid _guid = Guid.NewGuid();
        static HttpListener _listener = new HttpListener();


        public static async Task StartListeningAsync(int port = 55555)
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
                using (var jsonReader = new JsonTextReader(reader))
                {
                    if (context.Request.HttpMethod == "GET" && context.Request.Url.LocalPath == "/Status")
                    {
                        Status(response);
                    }

                    if (context.Request.HttpMethod == "GET" && context.Request.Url.LocalPath == "/owlcat://open/")
                    {
                        string path = context.Request.QueryString["path"];
                        if (!string.IsNullOrEmpty(path))
                        {
                            Console.WriteLine("Команда OpenString получена");
                            Console.WriteLine($"Path: {path}");

                            await OpenString(response, path);
                            response.OutputStream.Close();

                        }
                        else
                        {
                            response.StatusCode = (int)HttpStatusCode.BadRequest;
                            byte[] buffer = System.Text.Encoding.UTF8.GetBytes("Missing 'path' parameter");
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
                SupportedCommands = new List<string> { "Status", "FindLocale" },
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

            StringManager.Filter.Name = path;
            StringManager.Filter.ForceUpdateFilter();

            string result = $"Processed path: {path}";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(result);
            resp.ContentLength64 = buffer.Length;
            await resp.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        }


        public static void StopListening()
        {
            if (_listener != null)
            {
                _listener.Stop();
                _listener.Close();
            }
        }
    }
}
