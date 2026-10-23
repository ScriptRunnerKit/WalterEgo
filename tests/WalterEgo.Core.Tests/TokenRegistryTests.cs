using static WalterEgo.Core.Tests.TestRegistry;

namespace WalterEgo.Core.Tests;

public sealed class TokenRegistryTests
{
    private const string DecoyKey = "fictional-key-0001";
    private const string DecoyUser = "fictional.user@example.com";
    private const string DecoyPath = "/k2pV9qLw7zXcR4tY";

    [Fact]
    public void Finds_tokens_by_id()
    {
        var registry = Build(
            Registration("key", HoneyTokenKind.ApiKey, Trigger(TriggerDomain.ApiKey, DecoyKey)),
            Registration("row", HoneyTokenKind.Row));

        registry.TryGet(new HoneyTokenId("key"), out var key).ShouldBeTrue();
        key.Kind.ShouldBe(HoneyTokenKind.ApiKey);
        registry.TryGet(new HoneyTokenId("row"), out var row).ShouldBeTrue();
        row.Kind.ShouldBe(HoneyTokenKind.Row);
        registry.TryGet(new HoneyTokenId("unknown"), out var unknown).ShouldBeFalse();
        unknown.ShouldBeNull();
        registry.TryGet(default, out _).ShouldBeFalse();
    }

    [Fact]
    public void Lists_tokens_ordered_by_id()
    {
        var registry = Build(
            Registration("charlie", HoneyTokenKind.Row),
            Registration("alpha", HoneyTokenKind.Row),
            Registration("bravo-2", HoneyTokenKind.Row),
            Registration("bravo", HoneyTokenKind.Row));

        registry.Tokens.Select(token => token.Id.Value).ShouldBe(["alpha", "bravo", "bravo-2", "charlie"]);
        Should.Throw<NotSupportedException>(() => ((ICollection<HoneyToken>)registry.Tokens).Clear());
    }

    [Fact]
    public void Matches_raw_values_in_their_domain()
    {
        var registry = Build(
            Registration("key", HoneyTokenKind.ApiKey, Trigger(TriggerDomain.ApiKey, DecoyKey)),
            Registration("user", HoneyTokenKind.User, Trigger(TriggerDomain.Username, DecoyUser)) with { Class = DecoyClass.Unique });

        registry.TryMatch(TriggerDomain.ApiKey, DecoyKey, out var key).ShouldBeTrue();
        key.Id.Value.ShouldBe("key");
        registry.TryMatch(TriggerDomain.Username, DecoyUser, out var user).ShouldBeTrue();
        user.Id.Value.ShouldBe("user");
    }

    [Fact]
    public void Does_not_match_values_from_another_domain()
    {
        var registry = Build(
            Registration("key", HoneyTokenKind.ApiKey, Trigger(TriggerDomain.ApiKey, DecoyKey)),
            Registration("user", HoneyTokenKind.User, Trigger(TriggerDomain.Username, DecoyUser)) with { Class = DecoyClass.Unique });

        registry.TryMatch(TriggerDomain.Username, DecoyKey, out var wrongDomain).ShouldBeFalse();
        wrongDomain.ShouldBeNull();
        registry.TryMatch(TriggerDomain.ApiKey, "fictional-key-0002", out _).ShouldBeFalse();
        registry.TryMatch(TriggerDomain.ApiKey, "", out _).ShouldBeFalse();
        registry.TryMatch(TriggerDomain.SessionToken, DecoyKey, out _).ShouldBeFalse();
        registry.TryMatch(0, DecoyKey, out _).ShouldBeFalse();
        registry.TryMatch((TriggerDomain)99, DecoyKey, out _).ShouldBeFalse();
        registry.TryMatch(TriggerDomain.ApiKey, new string('x', 100_000), out _).ShouldBeFalse();
    }

    [Fact]
    public void Reports_which_domains_have_triggers()
    {
        var registry = Build(
            Registration("key", HoneyTokenKind.ApiKey, Trigger(TriggerDomain.ApiKey, DecoyKey)),
            Registration("env", HoneyTokenKind.Endpoint) with { Class = DecoyClass.Generic });

        registry.HasTriggers(TriggerDomain.ApiKey).ShouldBeTrue();
        registry.HasTriggers(TriggerDomain.Username).ShouldBeFalse();
        registry.HasTriggers(TriggerDomain.SessionToken).ShouldBeFalse();
        registry.HasTriggers(TriggerDomain.RequestPath).ShouldBeFalse();
        registry.HasTriggers(0).ShouldBeFalse();
        registry.HasTriggers((TriggerDomain)99).ShouldBeFalse();
    }

    [Fact]
    public void Any_trigger_resolves_its_token()
    {
        var registry = Build(
            Registration(
                "backup-file",
                HoneyTokenKind.File,
                Trigger(TriggerDomain.ApiKey, DecoyKey),
                Trigger(TriggerDomain.Username, DecoyUser),
                Trigger(TriggerDomain.RequestPath, DecoyPath)));

        registry.TryMatch(TriggerDomain.ApiKey, DecoyKey, out var byKey).ShouldBeTrue();
        registry.TryMatch(TriggerDomain.Username, DecoyUser, out var byUser).ShouldBeTrue();
        registry.TryMatch(TriggerDomain.RequestPath, DecoyPath, out var byPath).ShouldBeTrue();
        byKey.ShouldBeSameAs(byUser);
        byUser.ShouldBeSameAs(byPath);
        byPath.Id.Value.ShouldBe("backup-file");
    }

    [Fact]
    public void Concurrent_lookups_are_consistent()
    {
        var registry = Build(
            Registration("key", HoneyTokenKind.ApiKey, Trigger(TriggerDomain.ApiKey, DecoyKey)),
            Registration("user", HoneyTokenKind.User, Trigger(TriggerDomain.Username, DecoyUser)) with { Class = DecoyClass.Unique });
        var mismatches = 0;

        Parallel.For(0, 20_000, i =>
        {
            var consistent = (i % 4) switch
            {
                0 => registry.TryMatch(TriggerDomain.ApiKey, DecoyKey, out var token) && token.Id.Value == "key",
                1 => registry.TryMatch(TriggerDomain.Username, DecoyUser, out var token) && token.Id.Value == "user",
                2 => !registry.TryMatch(TriggerDomain.ApiKey, $"fictional-key-{i}", out _),
                _ => registry.TryGet(new HoneyTokenId("key"), out var token) && token.Kind == HoneyTokenKind.ApiKey,
            };

            if (!consistent)
            {
                Interlocked.Increment(ref mismatches);
            }
        });

        mismatches.ShouldBe(0);
    }

    [Fact]
    public void String_representation_hides_the_digests()
    {
        var trigger = Trigger(TriggerDomain.ApiKey, DecoyKey);
        var registry = Build(Registration("key", HoneyTokenKind.ApiKey, trigger));

        var text = registry.ToString()!;

        text.ShouldNotContain(trigger.Digest.ToString());
        text.ShouldNotContain(DecoyKey);
    }
}
