using System;

namespace Orchestrator.App;

internal static class Logger
{
    public static void WriteLine(string message)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");
        Console.WriteLine($"[{timestamp}] {message}");
    }
}
