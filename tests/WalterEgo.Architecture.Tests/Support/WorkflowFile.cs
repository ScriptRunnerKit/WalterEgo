using System.Text.RegularExpressions;

namespace WalterEgo.Architecture.Tests.Support;

/// <summary>
/// Line-based reader for the repository's own GitHub Actions workflows (two-space indentation, jobs at level one).
/// </summary>
internal sealed partial class WorkflowFile(string relativePath, string[] lines)
{
    public string RelativePath { get; } = relativePath;

    public string Text { get; } = string.Join('\n', lines);

    public static IReadOnlyList<WorkflowFile> All() =>
        [.. Directory.EnumerateFiles(Repository.PathOf(".github/workflows"))
            .Where(path => path.EndsWith(".yml", StringComparison.Ordinal) || path.EndsWith(".yaml", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .Select(Load)];

    public static WorkflowFile Load(string path) =>
        new(Path.GetRelativePath(Repository.Root, path).Replace('\\', '/'), File.ReadAllLines(path));

    public IEnumerable<string> ActionReferences() =>
        lines.Select(line => UsesPattern().Match(line))
            .Where(match => match.Success)
            .Select(match => match.Groups["ref"].Value);

    public IReadOnlyList<string> TopLevelBlock(string key)
    {
        var start = Array.FindIndex(lines, line => line.StartsWith($"{key}:", StringComparison.Ordinal));
        if (start < 0)
        {
            return [];
        }

        var inline = lines[start][(key.Length + 1)..].Trim();
        return inline.Length > 0 ? [inline] : [.. lines.Skip(start + 1).TakeWhile(IsIndentedOrBlank)];
    }

    public IReadOnlyDictionary<string, IReadOnlyList<string>> Jobs()
    {
        var jobs = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        string? current = null;
        List<string> body = [];

        foreach (var line in TopLevelBlock("jobs"))
        {
            var header = JobHeaderPattern().Match(line);
            if (header.Success)
            {
                if (current is not null)
                {
                    jobs[current] = body;
                }

                current = header.Groups["name"].Value;
                body = [];
            }
            else
            {
                body.Add(line);
            }
        }

        if (current is not null)
        {
            jobs[current] = body;
        }

        return jobs;
    }

    private static bool IsIndentedOrBlank(string line) => line.Length == 0 || char.IsWhiteSpace(line[0]) || line.StartsWith('#');

    [GeneratedRegex("""^\s*(?:-\s+)?uses:\s*['"]?(?<ref>[^\s'"#]+)""")]
    private static partial Regex UsesPattern();

    [GeneratedRegex(@"^  (?<name>[A-Za-z0-9_-]+):\s*$")]
    private static partial Regex JobHeaderPattern();
}
