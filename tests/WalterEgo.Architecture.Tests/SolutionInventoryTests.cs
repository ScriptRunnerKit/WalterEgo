using System.Xml.Linq;
using WalterEgo.Architecture.Tests.Support;

namespace WalterEgo.Architecture.Tests;

public sealed class SolutionInventoryTests
{
    [Fact]
    public void Solution_contains_exactly_the_expected_projects()
    {
        var solution = XDocument.Load(Repository.PathOf("WalterEgo.slnx"));

        var projectPaths = solution.Descendants("Project")
            .Select(project => ((string?)project.Attribute("Path") ?? string.Empty).Replace('\\', '/'))
            .Order(StringComparer.Ordinal)
            .ToList();

        projectPaths.ShouldBe([.. ProjectCatalog.All.Select(project => project.RelativePath).Order(StringComparer.Ordinal)]);
        ProjectCatalog.All.ShouldAllBe(project => File.Exists(Repository.PathOf(project.RelativePath)));
    }
}
