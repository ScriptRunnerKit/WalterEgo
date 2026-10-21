using System.Xml.Linq;
using WalterEgo.Architecture.Tests.Support;

namespace WalterEgo.Architecture.Tests;

public sealed class NuGetConfigTests
{
    private const string NuGetOrg = "https://api.nuget.org/v3/index.json";

    [Fact]
    public void Only_nuget_org_is_configured_with_source_mapping()
    {
        var configuration = XDocument.Load(Repository.PathOf("nuget.config")).Root.ShouldNotBeNull();

        var sources = configuration.Element("packageSources").ShouldNotBeNull();
        sources.Elements().First().Name.LocalName.ShouldBe("clear");
        var source = sources.Elements("add").ShouldHaveSingleItem();
        ((string?)source.Attribute("value")).ShouldBe(NuGetOrg);

        var mapping = configuration.Element("packageSourceMapping").ShouldNotBeNull()
            .Elements("packageSource").ShouldHaveSingleItem();
        ((string?)mapping.Attribute("key")).ShouldBe((string?)source.Attribute("key"));
        mapping.Elements("package").Select(package => (string?)package.Attribute("pattern")).ShouldBe(["*"]);
    }
}
