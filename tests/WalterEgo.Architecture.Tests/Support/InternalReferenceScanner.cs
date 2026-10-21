using System.Text.RegularExpressions;

namespace WalterEgo.Architecture.Tests.Support;

internal sealed record InternalReferenceFinding(string Path, int Line, string Match);

internal sealed record AllowlistEntry(string Path, string Excerpt);

internal sealed class InternalReferenceScanner(IReadOnlyList<Regex> patterns, IReadOnlyList<AllowlistEntry> allowlist)
{
    public const string PatternsFile = "eng/internal-references.patterns";
    public const string AllowlistFile = "eng/internal-references.allowlist";

    private const string WholeFile = "*";

    public static InternalReferenceScanner FromRepository() =>
        new(ParsePatterns(Repository.ReadLines(PatternsFile)), ParseAllowlist(Repository.ReadLines(AllowlistFile)));

    public static IReadOnlyList<Regex> ParsePatterns(IEnumerable<string> lines) =>
        [.. MeaningfulLines(lines).Select(line => new Regex(line, RegexOptions.CultureInvariant))];

    public static IReadOnlyList<AllowlistEntry> ParseAllowlist(IEnumerable<string> lines) =>
        [.. MeaningfulLines(lines).Select(ParseAllowlistEntry)];

    public IEnumerable<InternalReferenceFinding> Scan(string path, string content)
    {
        var lines = content.Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            foreach (var pattern in patterns)
            {
                var match = pattern.Match(line);
                if (match.Success && !IsAllowed(path, line))
                {
                    yield return new InternalReferenceFinding(path, index + 1, match.Value);
                }
            }
        }
    }

    private bool IsAllowed(string path, string line) =>
        allowlist.Any(entry => entry.Path == path
            && (entry.Excerpt == WholeFile || line.Contains(entry.Excerpt, StringComparison.Ordinal)));

    private static IEnumerable<string> MeaningfulLines(IEnumerable<string> lines) =>
        lines.Select(line => line.Trim()).Where(line => line.Length > 0 && !line.StartsWith('#'));

    private static AllowlistEntry ParseAllowlistEntry(string line)
    {
        var parts = line.Split('|', 3, StringSplitOptions.TrimEntries);
        if (parts.Length != 3 || parts.Any(part => part.Length == 0))
        {
            throw new FormatException($"Allowlist entries must be 'path | excerpt | reason': {line}");
        }

        return new AllowlistEntry(parts[0], parts[1]);
    }
}
