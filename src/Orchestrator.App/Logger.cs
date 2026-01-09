using System;

namespace Orchestrator.App;

internal static class Logger
{
    private static bool _debugEnabled = Environment.GetEnvironmentVariable("DEBUG")?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

    public static void WriteLine(string message)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");
        Console.WriteLine($"[{timestamp}] {message}");
    }

    public static void Debug(string message)
    {
        if (_debugEnabled)
        {
            WriteLine($"[DEBUG] {message}");
        }
    }

    public static void Info(string message)
    {
        WriteLine($"[INFO] {message}");
    }

    public static void Warning(string message)
    {
        WriteLine($"[WARN] {message}");
    }

    public static void Error(string message)
    {
        WriteLine($"[ERROR] {message}");
    }
}
