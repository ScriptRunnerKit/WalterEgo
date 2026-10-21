using System.Reflection;
using System.Text.RegularExpressions;

namespace WalterEgo.Alerting.Webhook.Tests;

public sealed partial class PackageIdentityTests
{
    private const string PackageId = "WalterEgo.Alerting.Webhook";

    private static readonly Assembly PackageAssembly = Assembly.Load(PackageId);

    [Fact]
    public void Assembly_name_matches_the_package_id()
    {
        PackageAssembly.GetName().Name.ShouldBe(PackageId);
    }

    [Fact]
    public void Assembly_has_a_semantic_informational_version()
    {
        var version = PackageAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        version.ShouldNotBeNull();
        SemanticVersion().IsMatch(version).ShouldBeTrue(version);
    }

    [GeneratedRegex(@"^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?(\+[0-9A-Za-z.-]+)?$")]
    private static partial Regex SemanticVersion();
}
