using System.Collections.Concurrent;

namespace csharpClipper;

public static class Logger
{
    private static readonly BlockingCollection<string> _logQueue = [];
    private static readonly CancellationTokenSource _cts = new();
    private static readonly string logFilePath = "app.log";
    private static readonly Task _loggingTask;

    static Logger()
    {
        _loggingTask = Task.Factory.StartNew(ProcessLogQueue, TaskCreationOptions.LongRunning);
    }

    public static void Log(string message)
    {
        string logEntry = $"{DateTime.Now:HH:mm:ss.fff} - {message}";
        _logQueue.Add(logEntry);
        Console.WriteLine(logEntry);
    }

    public static void LogException(Exception ex, string context = "")
    {
        Log($"Exception in {context}: {ex}");
    }

    private static void ProcessLogQueue()
    {
        try
        {
            using var writer = new StreamWriter(logFilePath, append: true);
            foreach (var logEntry in _logQueue.GetConsumingEnumerable(_cts.Token))
            {
                writer.WriteLine(logEntry);
                writer.Flush();
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Console.WriteLine($"Logger encountered an exception: {ex}");
        }
    }

    public static void Shutdown()
    {
        _logQueue.CompleteAdding();
        _cts.Cancel();
        try
        {
            _loggingTask.Wait();
        }
        catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is OperationCanceledException)) { }
    }
}
