using System.Text.RegularExpressions;

namespace DoTrack.GitProviders.Abstractions;

public static partial class IssueKeyDetector
{
    [GeneratedRegex(@"\b[A-Z][A-Z0-9_]+-\d+\b", RegexOptions.CultureInvariant)]
    private static partial Regex KeyPattern();

    public static IReadOnlyList<string> Extract(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Array.Empty<string>();
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var results = new List<string>();
        foreach (Match match in KeyPattern().Matches(text))
        {
            if (seen.Add(match.Value))
            {
                results.Add(match.Value);
            }
        }
        return results;
    }
}
