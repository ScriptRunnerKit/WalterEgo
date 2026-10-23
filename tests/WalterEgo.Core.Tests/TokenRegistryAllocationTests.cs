using static WalterEgo.Core.Tests.TestRegistry;

namespace WalterEgo.Core.Tests;

public sealed class TokenRegistryAllocationTests
{
    private const string DecoyKey = "fictional-key-0001";

    [Fact]
    public void Lookups_do_not_allocate()
    {
        var registry = Build(
            Registration("key", HoneyTokenKind.ApiKey, Trigger(TriggerDomain.ApiKey, DecoyKey)),
            Registration("row", HoneyTokenKind.Row));
        var id = new HoneyTokenId("row");
        var missingValue = "a-real-key-presented-by-a-legitimate-client-" + new string('x', 64);
        var longestStackValue = new string('y', 1024);
        var multiByteValue = new string('ç', 512);

        var matches = RunLookups(registry, id, missingValue, longestStackValue, multiByteValue);
        var before = GC.GetAllocatedBytesForCurrentThread();
        matches += RunLookups(registry, id, missingValue, longestStackValue, multiByteValue);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        allocated.ShouldBe(0);
        matches.ShouldBe(2 * 1_000 * 2);
    }

    private static int RunLookups(TokenRegistry registry, HoneyTokenId id, string missingValue, string longestStackValue, string multiByteValue)
    {
        var matches = 0;
        for (var i = 0; i < 1_000; i++)
        {
            matches += registry.TryMatch(TriggerDomain.ApiKey, DecoyKey, out _) ? 1 : 0;
            matches += registry.TryMatch(TriggerDomain.ApiKey, missingValue, out _) ? 1 : 0;
            matches += registry.TryMatch(TriggerDomain.ApiKey, longestStackValue, out _) ? 1 : 0;
            matches += registry.TryMatch(TriggerDomain.ApiKey, multiByteValue, out _) ? 1 : 0;
            matches += registry.TryMatch(TriggerDomain.Username, DecoyKey, out _) ? 1 : 0;
            matches += registry.TryGet(id, out _) ? 1 : 0;
        }

        return matches;
    }
}
