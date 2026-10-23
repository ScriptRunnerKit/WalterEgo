using BenchmarkDotNet.Attributes;

namespace WalterEgo.Benchmarks;

[MemoryDiagnoser]
public class TokenRegistryBenchmarks
{
    private const string DecoyKey = "fictional-key-0050";
    private const string LegitimateKey = "a-real-key-presented-by-a-legitimate-client-0123456789";

    private TokenRegistry _registry = null!;
    private HoneyTokenId _id;

    [GlobalSetup]
    public void Setup()
    {
        // Fictitious key, only used to build the benchmark registry.
        byte[] key = [.. Enumerable.Range(0, 32).Select(i => (byte)i)];
        var hasher = new HmacSecretHasher(key);
        var registrations = Enumerable.Range(0, 100).Select(i =>
            new HoneyTokenRegistration($"key-{i:D3}", HoneyTokenKind.ApiKey, $"fictional placement {i}")
            {
                Triggers = [new HoneyTrigger(TriggerDomain.ApiKey, hasher.ComputeDigest(TriggerDomain.ApiKey, $"fictional-key-{i:D4}"))],
            });

        _registry = TokenRegistryBuilder.Build(hasher, registrations).Registry
            ?? throw new InvalidOperationException("The benchmark registry is invalid.");
        _id = new HoneyTokenId("key-050");
    }

    [Benchmark(Baseline = true)]
    public bool TryMatchWithoutMatch() => _registry.TryMatch(TriggerDomain.ApiKey, LegitimateKey, out _);

    [Benchmark]
    public bool TryMatchWithMatch() => _registry.TryMatch(TriggerDomain.ApiKey, DecoyKey, out _);

    [Benchmark]
    public bool TryMatchInEmptyDomain() => _registry.TryMatch(TriggerDomain.RequestPath, "/orders/42", out _);

    [Benchmark]
    public bool TryGetById() => _registry.TryGet(_id, out _);
}
