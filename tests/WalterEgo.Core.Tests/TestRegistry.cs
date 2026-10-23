namespace WalterEgo.Core.Tests;

internal static class TestRegistry
{
    public static HmacSecretHasher Hasher { get; } = new(TestKeys.Sequential);

    public static HoneyTrigger Trigger(TriggerDomain domain, string value) => new(domain, Hasher.ComputeDigest(domain, value));

    public static HoneyTokenRegistration Registration(string id, HoneyTokenKind kind, params HoneyTrigger[] triggers) =>
        new(id, kind, $"placement of {id}") { Triggers = triggers };

    public static TokenRegistryBuildResult TryBuild(params HoneyTokenRegistration[] registrations) =>
        TokenRegistryBuilder.Build(Hasher, registrations);

    public static TokenRegistry Build(params HoneyTokenRegistration[] registrations)
    {
        var result = TryBuild(registrations);

        result.Errors.ShouldBeEmpty();
        return result.Registry.ShouldNotBeNull();
    }
}
