using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace csharpClipper;

public static class RegexPatterns
{
    private static List<(Regex regex, string replacement)> _patterns;

    public static void Initialize()
    {
        _patterns = [];

        try
        {
            if (!File.Exists("replacements.json"))
            {
                Logger.Log("replacements.json not found.");
                return;
            }

            var json = File.ReadAllText("replacements.json");
            var replacements = JsonSerializer.Deserialize<List<PatternReplacement>>(json) ?? [];

            foreach (var pr in replacements)
            {
                if (!string.IsNullOrWhiteSpace(pr.Pattern))
                {
                    try
                    {
                        var regex = new Regex(pr.Pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                        _patterns.Add((regex, pr.Replacement));
                    }
                    catch (Exception ex)
                    {
                        Logger.LogException(ex, $"Invalid regex pattern '{pr.Pattern}'");
                    }
                }
            }

            Logger.Log($"Loaded {_patterns.Count} regex patterns.");
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "RegexPatterns.Initialize");
            _patterns = [];
        }
    }

    public static IEnumerable<(Regex regex, string replacement)> GetPatterns() => _patterns;
}

public class PatternReplacement
{
    [JsonPropertyName("pattern")]
    public string Pattern { get; set; }

    [JsonPropertyName("replacement")]
    public string Replacement { get; set; }
}
