using WalterEgo.Architecture.Tests.Support;

namespace WalterEgo.Architecture.Tests;

public sealed class InternalReferenceTests
{
    [Fact]
    public async Task Tracked_files_do_not_reference_internal_material()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var scanner = InternalReferenceScanner.FromRepository();
        List<InternalReferenceFinding> findings = [];

        foreach (var file in await Git.VersionedFilesAsync(cancellationToken))
        {
            var path = Repository.PathOf(file);
            if (!File.Exists(path) || await IsBinaryAsync(path, cancellationToken))
            {
                continue;
            }

            findings.AddRange(scanner.Scan(file, await File.ReadAllTextAsync(path, cancellationToken)));
        }

        findings.ShouldBeEmpty(string.Join(Environment.NewLine, findings.Select(finding => $"{finding.Path}:{finding.Line}: {finding.Match}")));
    }

    [Fact]
    public void Scanner_reports_the_file_and_line_of_an_internal_reference()
    {
        var scanner = new InternalReferenceScanner(RepositoryPatterns(), []);

        var findings = scanner.Scan("src/Example.cs", "// first line\n// implements spec 007\n").ToList();

        findings.ShouldHaveSingleItem().ShouldBe(new InternalReferenceFinding("src/Example.cs", 2, "spec 007"));
    }

    [Fact]
    public void Scanner_accepts_allowlisted_occurrences()
    {
        var allowlist = InternalReferenceScanner.ParseAllowlist(["src/Example.cs | spec 007 | example of an accepted occurrence"]);
        var scanner = new InternalReferenceScanner(RepositoryPatterns(), allowlist);

        scanner.Scan("src/Example.cs", "// implements spec 007\n").ShouldBeEmpty();
        scanner.Scan("src/Other.cs", "// implements spec 007\n").ShouldNotBeEmpty();
    }

    private static IReadOnlyList<System.Text.RegularExpressions.Regex> RepositoryPatterns() =>
        InternalReferenceScanner.ParsePatterns(Repository.ReadLines(InternalReferenceScanner.PatternsFile));

    private static async Task<bool> IsBinaryAsync(string path, CancellationToken cancellationToken)
    {
        var buffer = new byte[8000];
        await using var stream = File.OpenRead(path);
        var read = await stream.ReadAsync(buffer, cancellationToken);
        return Array.IndexOf(buffer, (byte)0, 0, read) >= 0;
    }
}
