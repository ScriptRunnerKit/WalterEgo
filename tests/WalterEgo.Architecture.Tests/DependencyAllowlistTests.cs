using System.Text.Json;
using WalterEgo.Architecture.Tests.Support;

namespace WalterEgo.Architecture.Tests;

public sealed class DependencyAllowlistTests
{
    private const string AllowlistFile = "eng/dependency-allowlist.json";

    public static TheoryData<string> SourceProjects => ProjectCatalog.SourceNames();

    [Theory]
    [MemberData(nameof(SourceProjects))]
    public async Task Src_projects_only_depend_on_allowed_packages(string projectName)
    {
        var project = ProjectCatalog.Get(projectName);
        var allowlist = DependencyAllowlist.Load();
        var allowed = allowlist.AllowedFor(projectName);

        var lockedDependencies = ReadLockFile(project);
        var unexpectedPackages = lockedDependencies.Where(name => !allowed.Contains(name)).ToList();
        unexpectedPackages.ShouldBeEmpty($"{projectName} depends on packages or projects outside {AllowlistFile}");

        var evaluation = await MsBuild.EvaluateAsync(project);
        var unexpectedFrameworks = evaluation.Items("FrameworkReference")
            .Select(item => item.Identity)
            .Where(name => !allowed.Contains(name))
            .ToList();
        unexpectedFrameworks.ShouldBeEmpty($"{projectName} has framework references outside {AllowlistFile}");
    }

    [Fact]
    public void Allowlist_covers_exactly_the_src_projects()
    {
        DependencyAllowlist.Load().ProjectNames
            .Order(StringComparer.Ordinal)
            .ShouldBe([.. ProjectCatalog.All.Where(project => project.IsSource).Select(project => project.Name).Order(StringComparer.Ordinal)]);
    }

    private static List<string> ReadLockFile(ProjectInfo project)
    {
        using var document = JsonDocument.Parse(Repository.ReadText($"{project.RelativeDirectory}/packages.lock.json"));
        return [.. document.RootElement.GetProperty("dependencies")
            .EnumerateObject()
            .SelectMany(framework => framework.Value.EnumerateObject())
            .Select(dependency => dependency.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)];
    }

    private sealed class DependencyAllowlist
    {
        private readonly HashSet<string> _common;
        private readonly Dictionary<string, HashSet<string>> _perProject;

        private DependencyAllowlist(HashSet<string> common, Dictionary<string, HashSet<string>> perProject)
        {
            _common = common;
            _perProject = perProject;
        }

        public IEnumerable<string> ProjectNames => _perProject.Keys;

        public static DependencyAllowlist Load()
        {
            using var document = JsonDocument.Parse(Repository.ReadText(AllowlistFile));
            var root = document.RootElement;

            HashSet<string> common = new(StringComparer.OrdinalIgnoreCase);
            common.UnionWith(Strings(root.GetProperty("buildOnly")));
            common.UnionWith(Strings(root.GetProperty("baseFrameworks")));

            var perProject = root.GetProperty("projects")
                .EnumerateObject()
                .ToDictionary(
                    project => project.Name,
                    project => new HashSet<string>(
                        [.. Strings(project.Value.GetProperty("packages")),
                         .. Strings(project.Value.GetProperty("projects")),
                         .. Strings(project.Value.GetProperty("frameworks"))],
                        StringComparer.OrdinalIgnoreCase));

            return new DependencyAllowlist(common, perProject);
        }

        public HashSet<string> AllowedFor(string projectName) =>
            _perProject.TryGetValue(projectName, out var specific)
                ? new HashSet<string>([.. _common, .. specific], StringComparer.OrdinalIgnoreCase)
                : throw new InvalidOperationException($"{projectName} is missing from {AllowlistFile}.");

        private static IEnumerable<string> Strings(JsonElement array) =>
            array.EnumerateArray().Select(element => element.GetString() ?? string.Empty);
    }
}
