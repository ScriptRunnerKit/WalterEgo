using WalterEgo.Architecture.Tests.Support;

namespace WalterEgo.Architecture.Tests;

public sealed class RepositoryHygieneTests
{
    private const string LocalOnlyFolder = "docs";

    [Fact]
    public async Task Docs_folder_is_ignored_and_untracked()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        Repository.ReadLines(".gitignore").Select(line => line.Trim()).ShouldContain($"/{LocalOnlyFolder}/");
        (await Git.TrackedFilesAsync(LocalOnlyFolder, cancellationToken)).ShouldBeEmpty();
        (await Git.IsIgnoredAsync($"{LocalOnlyFolder}/any-file.md", cancellationToken)).ShouldBeTrue();
    }
}
