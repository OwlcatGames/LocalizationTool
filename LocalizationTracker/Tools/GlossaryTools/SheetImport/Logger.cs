using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace LocalizationTracker.Tools.GoogleSheet;

public class Logger : ILogger
{
    public static readonly Logger Default = new Logger();

    private IExternalScopeProvider scopeProvider = new LoggerExternalScopeProvider();

    private IList<object> scopes = new List<object>();

    public IDisposable BeginScope<TState>(TState state)
    {
        return scopeProvider.Push(state);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        scopes.Clear();
        scopeProvider.ForEachScope((x, scopes) => scopes.Add(x), scopes);

        var message = formatter(state, exception);
        if (scopes.Count > 0)
            message = $"[{string.Join(">", scopes)}] {message}";

        switch (logLevel)
        {
            case LogLevel.Trace:
            case LogLevel.Debug:
            case LogLevel.Information:
                Console.WriteLine(message);
                break;

            case LogLevel.Warning:
                Console.WriteLine(message);
                break;

            case LogLevel.Error:
            case LogLevel.Critical:
                Console.WriteLine(message);
                break;
        }
    }
}