using WalterEgo.Architecture.Tests.Support;

namespace WalterEgo.Architecture.Tests;

public sealed class AnalyzerWiringTests
{
    private const string BannedSymbolsFile = "eng/BannedSymbols.txt";

    private static readonly string[] RequiredBannedSymbols =
    [
        "P:System.DateTime.Now",
        "P:System.DateTime.UtcNow",
        "P:System.DateTimeOffset.Now",
        "P:System.DateTimeOffset.UtcNow",
        "M:System.Net.Http.HttpClient.#ctor",
        "M:System.Net.Http.HttpClient.#ctor(System.Net.Http.HttpMessageHandler)",
        "M:System.Net.Http.HttpClient.#ctor(System.Net.Http.HttpMessageHandler,System.Boolean)",
        "M:System.Threading.Tasks.Task.Wait",
        "M:System.Threading.Tasks.Task.WaitAll(System.Threading.Tasks.Task[])",
        "P:System.Threading.Tasks.Task`1.Result",
        "T:System.Console",
        "T:System.Random",
        "M:System.GC.Collect",
    ];

    public static TheoryData<string> SourceProjects => ProjectCatalog.SourceNames();

    [Theory]
    [MemberData(nameof(SourceProjects))]
    public async Task Public_api_files_are_wired_into_every_src_project(string projectName)
    {
        var project = ProjectCatalog.Get(projectName);
        var evaluation = await MsBuild.EvaluateAsync(project);
        var additionalFiles = evaluation.Items("AdditionalFiles").Select(item => Path.GetFullPath(item.FullPath)).ToList();

        foreach (var fileName in new[] { "PublicAPI.Shipped.txt", "PublicAPI.Unshipped.txt" })
        {
            var path = Repository.PathOf($"{project.RelativeDirectory}/{fileName}");
            File.Exists(path).ShouldBeTrue($"{project.Name} has no {fileName}");
            File.ReadLines(path).First().ShouldBe("#nullable enable");
            additionalFiles.ShouldContain(path);
        }

        evaluation.Items("PackageReference").ShouldContain(item => item.Identity == "Microsoft.CodeAnalysis.PublicApiAnalyzers");
    }

    [Theory]
    [MemberData(nameof(SourceProjects))]
    public async Task Banned_symbols_are_wired_into_every_src_project(string projectName)
    {
        var evaluation = await MsBuild.EvaluateAsync(ProjectCatalog.Get(projectName));

        evaluation.Items("AdditionalFiles")
            .Select(item => Path.GetFullPath(item.FullPath))
            .ShouldContain(Repository.PathOf(BannedSymbolsFile));
        evaluation.Items("PackageReference").ShouldContain(item => item.Identity == "Microsoft.CodeAnalysis.BannedApiAnalyzers");
    }

    [Fact]
    public void Banned_symbols_include_the_agreed_apis()
    {
        var bannedIds = Repository.ReadLines(BannedSymbolsFile)
            .Select(line => line.Split(';', 2)[0].Trim())
            .ToHashSet(StringComparer.Ordinal);

        RequiredBannedSymbols.Where(symbol => !bannedIds.Contains(symbol)).ShouldBeEmpty();
    }
}
