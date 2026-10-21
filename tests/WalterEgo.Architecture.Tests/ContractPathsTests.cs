using WalterEgo.Architecture.Tests.Support;

namespace WalterEgo.Architecture.Tests;

public sealed class ContractPathsTests
{
    [Fact]
    public void Contract_paths_cover_public_api_and_schemas()
    {
        var paths = Repository.ReadLines("eng/contract-paths.txt").Select(line => line.Trim()).ToList();

        paths.ShouldContain("src/**/PublicAPI.*.txt");
        paths.ShouldContain("schemas/**");
    }
}
