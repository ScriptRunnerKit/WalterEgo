using WalterEgo.Architecture.Tests.Support;

namespace WalterEgo.Architecture.Tests;

public sealed class RepositoryFilesTests
{
    [Fact]
    public void Public_files_exist()
    {
        var license = Repository.ReadText("LICENSE");
        license.ShouldContain("MIT License");
        license.ShouldContain("Copyright (c) 2026 ScriptRunnerKit contributors");

        var security = Repository.ReadText("SECURITY.md");
        security.ShouldContain("Report a vulnerability");
        security.ShouldContain("## Scope");

        Repository.ReadText("CONTRIBUTING.md").ShouldNotBeNullOrWhiteSpace();

        var changelog = Repository.ReadText("CHANGELOG.md");
        changelog.ShouldContain("Keep a Changelog");
        changelog.ShouldContain("## [Unreleased]");

        Repository.ReadText("README.md").ShouldNotBeNullOrWhiteSpace();
        Repository.ReadText("README.pt-br.md").ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Readmes_link_to_each_other()
    {
        Repository.ReadText("README.md").ShouldContain("(README.pt-br.md)");
        Repository.ReadText("README.pt-br.md").ShouldContain("(README.md)");
    }
}
