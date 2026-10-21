using WalterEgo.Architecture.Tests.Support;

namespace WalterEgo.Architecture.Tests;

public sealed class BuildSettingsTests
{
    public static TheoryData<string> AllProjects => ProjectCatalog.AllNames();

    public static TheoryData<string> SourceProjects => ProjectCatalog.SourceNames();

    public static TheoryData<string> NonSourceProjects => ProjectCatalog.NonSourceNames();

    public static TheoryData<string> AotCompatibleProjects => ProjectCatalog.AotCompatibleNames();

    [Theory]
    [MemberData(nameof(AllProjects))]
    public async Task Every_project_inherits_the_common_build_settings(string projectName)
    {
        var evaluation = await MsBuild.EvaluateAsync(ProjectCatalog.Get(projectName));

        evaluation.Property("TargetFramework").ShouldBe("net10.0");
        evaluation.Property("Nullable").ShouldBe("enable");
        evaluation.Property("TreatWarningsAsErrors").ShouldBe("true");
        evaluation.Property("AnalysisLevel").ShouldBe("latest-Recommended");
        evaluation.Property("EnforceCodeStyleInBuild").ShouldBe("true");
        evaluation.Property("RestorePackagesWithLockFile").ShouldBe("true");
    }

    [Theory]
    [MemberData(nameof(SourceProjects))]
    public async Task Src_projects_are_packable_and_documented(string projectName)
    {
        var evaluation = await MsBuild.EvaluateAsync(ProjectCatalog.Get(projectName));

        evaluation.Property("IsPackable").ShouldBe("true");
        evaluation.Property("GenerateDocumentationFile").ShouldBe("true");

        // The SDK turns package validation off for tool packages: they expose no API to keep compatible.
        var expectedValidation = evaluation.Property("PackAsTool") == "true" ? "false" : "true";
        evaluation.Property("EnablePackageValidation").ShouldBe(expectedValidation);
    }

    [Theory]
    [MemberData(nameof(AotCompatibleProjects))]
    public async Task Aot_compatible_projects_are_flagged(string projectName)
    {
        var evaluation = await MsBuild.EvaluateAsync(ProjectCatalog.Get(projectName));

        evaluation.Property("IsAotCompatible").ShouldBe("true");
    }

    [Theory]
    [MemberData(nameof(NonSourceProjects))]
    public async Task Only_src_projects_are_packable(string projectName)
    {
        var evaluation = await MsBuild.EvaluateAsync(ProjectCatalog.Get(projectName));

        evaluation.Property("IsPackable").ShouldNotBe("true");
    }

    [Fact]
    public async Task Cli_is_packed_as_the_walterego_tool()
    {
        var evaluation = await MsBuild.EvaluateAsync(ProjectCatalog.Get("WalterEgo.Cli"));

        evaluation.Property("PackAsTool").ShouldBe("true");
        evaluation.Property("ToolCommandName").ShouldBe("walterego");
    }

    [Theory]
    [MemberData(nameof(AllProjects))]
    public async Task Ci_restores_in_locked_mode(string projectName)
    {
        var project = ProjectCatalog.Get(projectName);
        var evaluation = await MsBuild.EvaluateAsync(project);

        evaluation.Property("RestoreLockedMode").ShouldBe("true");
        File.Exists(Repository.PathOf($"{project.RelativeDirectory}/packages.lock.json"))
            .ShouldBeTrue($"{project.Name} has no packages.lock.json");
    }
}
