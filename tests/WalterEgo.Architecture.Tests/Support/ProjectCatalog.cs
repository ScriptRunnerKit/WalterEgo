namespace WalterEgo.Architecture.Tests.Support;

internal sealed record ProjectInfo(string Name, string Folder)
{
    public string RelativePath => $"{Folder}/{Name}/{Name}.csproj";

    public string RelativeDirectory => $"{Folder}/{Name}";

    public bool IsSource => Folder == "src";
}

internal static class ProjectCatalog
{
    public static IReadOnlyList<ProjectInfo> All { get; } =
    [
        new("WalterEgo.Core", "src"),
        new("WalterEgo.AspNetCore", "src"),
        new("WalterEgo.AspNetCore.Identity", "src"),
        new("WalterEgo.EntityFrameworkCore", "src"),
        new("WalterEgo.Alerting.Webhook", "src"),
        new("WalterEgo.Generators", "src"),
        new("WalterEgo.Cli", "src"),
        new("WalterEgo.Core.Tests", "tests"),
        new("WalterEgo.AspNetCore.Tests", "tests"),
        new("WalterEgo.AspNetCore.Identity.Tests", "tests"),
        new("WalterEgo.EntityFrameworkCore.Tests", "tests"),
        new("WalterEgo.Alerting.Webhook.Tests", "tests"),
        new("WalterEgo.Generators.Tests", "tests"),
        new("WalterEgo.Cli.Tests", "tests"),
        new("WalterEgo.Architecture.Tests", "tests"),
        new("WalterEgo.Benchmarks", "tests"),
        new("WalterEgo.Samples.MinimalApi", "samples"),
    ];

    public static IReadOnlySet<string> AotCompatible { get; } = new HashSet<string>(
    [
        "WalterEgo.Core",
        "WalterEgo.AspNetCore",
        "WalterEgo.AspNetCore.Identity",
        "WalterEgo.Alerting.Webhook",
    ]);

    public static ProjectInfo Get(string name) => All.Single(project => project.Name == name);

    public static TheoryData<string> AllNames() => [.. All.Select(project => project.Name)];

    public static TheoryData<string> SourceNames() => [.. All.Where(project => project.IsSource).Select(project => project.Name)];

    public static TheoryData<string> NonSourceNames() => [.. All.Where(project => !project.IsSource).Select(project => project.Name)];

    public static TheoryData<string> AotCompatibleNames() => [.. AotCompatible];
}
