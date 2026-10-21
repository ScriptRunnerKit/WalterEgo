using System.Collections.Concurrent;
using System.Text.Json;

namespace WalterEgo.Architecture.Tests.Support;

internal sealed record EvaluatedItem(string Identity, string FullPath);

internal sealed class ProjectEvaluation(
    IReadOnlyDictionary<string, string> properties,
    IReadOnlyDictionary<string, IReadOnlyList<EvaluatedItem>> items)
{
    public string Property(string name) => properties.GetValueOrDefault(name, string.Empty);

    public IReadOnlyList<EvaluatedItem> Items(string itemType) => items.GetValueOrDefault(itemType, []);
}

/// <summary>
/// Reads evaluated MSBuild properties and items, as they would be on the CI (<c>GITHUB_ACTIONS=true</c>).
/// Each project is evaluated once per test run.
/// </summary>
internal static class MsBuild
{
    private static readonly string[] PropertyNames =
    [
        "TargetFramework",
        "Nullable",
        "TreatWarningsAsErrors",
        "AnalysisLevel",
        "EnforceCodeStyleInBuild",
        "GenerateDocumentationFile",
        "RestorePackagesWithLockFile",
        "RestoreLockedMode",
        "IsPackable",
        "IsAotCompatible",
        "EnablePackageValidation",
        "PackAsTool",
        "ToolCommandName",
    ];

    private static readonly string[] ItemTypes = ["AdditionalFiles", "PackageReference", "FrameworkReference"];

    private static readonly ConcurrentDictionary<string, Lazy<Task<ProjectEvaluation>>> Cache = new();

    public static Task<ProjectEvaluation> EvaluateAsync(ProjectInfo project) =>
        Cache.GetOrAdd(project.RelativePath, path => new Lazy<Task<ProjectEvaluation>>(() => RunEvaluationAsync(path))).Value;

    private static async Task<ProjectEvaluation> RunEvaluationAsync(string relativePath)
    {
        string[] arguments =
        [
            "msbuild",
            Repository.PathOf(relativePath),
            "-nologo",
            "-p:GITHUB_ACTIONS=true",
            .. PropertyNames.Select(name => $"-getProperty:{name}"),
            .. ItemTypes.Select(type => $"-getItem:{type}"),
        ];

        var result = await ProcessRunner.RunAsync("dotnet", arguments, CancellationToken.None);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Evaluation of {relativePath} failed:{Environment.NewLine}{result.StandardOutput}{result.StandardError}");
        }

        using var document = JsonDocument.Parse(result.StandardOutput);
        var root = document.RootElement;

        var properties = root.GetProperty("Properties")
            .EnumerateObject()
            .ToDictionary(property => property.Name, property => property.Value.GetString() ?? string.Empty);

        var items = root.GetProperty("Items")
            .EnumerateObject()
            .ToDictionary(
                itemType => itemType.Name,
                itemType => (IReadOnlyList<EvaluatedItem>)[.. itemType.Value.EnumerateArray().Select(ToItem)]);

        return new ProjectEvaluation(properties, items);
    }

    private static EvaluatedItem ToItem(JsonElement item) =>
        new(item.GetProperty("Identity").GetString() ?? string.Empty,
            item.TryGetProperty("FullPath", out var fullPath) ? fullPath.GetString() ?? string.Empty : string.Empty);
}
