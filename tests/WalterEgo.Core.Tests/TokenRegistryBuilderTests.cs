using static WalterEgo.Core.Tests.TestRegistry;

namespace WalterEgo.Core.Tests;

public sealed class TokenRegistryBuilderTests
{
    public static TheoryData<HoneyTokenKind, TriggerDomain> UniqueByDefault => new()
    {
        { HoneyTokenKind.ApiKey, TriggerDomain.ApiKey },
        { HoneyTokenKind.Config, TriggerDomain.RequestPath },
        { HoneyTokenKind.SessionToken, TriggerDomain.SessionToken },
        { HoneyTokenKind.File, TriggerDomain.Username },
    };

    public static TheoryData<HoneyTokenKind, TriggerDomain?, bool> KindAndDomainMatrix
    {
        get
        {
            var data = new TheoryData<HoneyTokenKind, TriggerDomain?, bool>();
            foreach (var kind in Enum.GetValues<HoneyTokenKind>())
            {
                data.Add(kind, null, kind is HoneyTokenKind.Endpoint or HoneyTokenKind.Row);
                foreach (var domain in Enum.GetValues<TriggerDomain>())
                {
                    var accepted = kind switch
                    {
                        HoneyTokenKind.Endpoint => domain is TriggerDomain.RequestPath,
                        HoneyTokenKind.User => domain is TriggerDomain.Username,
                        HoneyTokenKind.ApiKey => domain is TriggerDomain.ApiKey,
                        HoneyTokenKind.SessionToken => domain is TriggerDomain.SessionToken,
                        HoneyTokenKind.Config or HoneyTokenKind.File => true,
                        _ => false,
                    };
                    data.Add(kind, domain, accepted);
                }
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(UniqueByDefault))]
    public void Applies_the_default_class_by_kind(HoneyTokenKind kind, TriggerDomain domain)
    {
        var registry = Build(Registration("decoy", kind, Trigger(domain, "fictional-value")));

        registry.Tokens.Single().Class.ShouldBe(DecoyClass.Unique);
    }

    [Fact]
    public void Rows_are_unique_by_default()
    {
        Build(Registration("row", HoneyTokenKind.Row)).Tokens.Single().Class.ShouldBe(DecoyClass.Unique);
    }

    [Theory]
    [InlineData(HoneyTokenKind.Endpoint, TriggerDomain.RequestPath)]
    [InlineData(HoneyTokenKind.User, TriggerDomain.Username)]
    public void Requires_an_explicit_class_for_endpoints_and_users(HoneyTokenKind kind, TriggerDomain domain)
    {
        var result = TryBuild(Registration("decoy", kind, Trigger(domain, "fictional-value")));

        result.Registry.ShouldBeNull();
        result.Errors.ShouldHaveSingleItem().TokenId.ShouldBe("decoy");
    }

    [Theory]
    [InlineData(DecoyClass.Generic, Severity.Low)]
    [InlineData(DecoyClass.Unique, Severity.High)]
    public void Applies_the_default_severity_by_class(DecoyClass decoyClass, Severity expected)
    {
        var registry = Build(Registration("env", HoneyTokenKind.Endpoint) with { Class = decoyClass });

        registry.Tokens.Single().Severity.ShouldBe(expected);
    }

    [Fact]
    public void Unique_kinds_default_to_high_severity()
    {
        var registry = Build(Registration("key", HoneyTokenKind.ApiKey, Trigger(TriggerDomain.ApiKey, "fictional-key")));

        registry.Tokens.Single().Severity.ShouldBe(Severity.High);
    }

    [Fact]
    public void Explicit_class_and_severity_win()
    {
        var registry = Build(
            Registration("key", HoneyTokenKind.ApiKey, Trigger(TriggerDomain.ApiKey, "fictional-key")) with { Class = DecoyClass.Generic, Severity = Severity.Critical },
            Registration("user", HoneyTokenKind.User, Trigger(TriggerDomain.Username, "fictional-user")) with { Class = DecoyClass.Generic, Severity = Severity.Medium });

        registry.TryGet(new HoneyTokenId("key"), out var key).ShouldBeTrue();
        key.Class.ShouldBe(DecoyClass.Generic);
        key.Severity.ShouldBe(Severity.Critical);
        registry.TryGet(new HoneyTokenId("user"), out var user).ShouldBeTrue();
        user.Class.ShouldBe(DecoyClass.Generic);
        user.Severity.ShouldBe(Severity.Medium);
    }

    [Fact]
    public void Copies_the_metadata_into_the_token()
    {
        var registry = Build(
            Registration("key", HoneyTokenKind.ApiKey, Trigger(TriggerDomain.ApiKey, "fictional-key")) with
            {
                Name = "Legacy CI key",
                Description = "Key left in an old pipeline variable.",
                Tags = new Dictionary<string, string> { ["env"] = "production" },
            });

        var token = registry.Tokens.Single();
        token.Id.ShouldBe(new HoneyTokenId("key"));
        token.Kind.ShouldBe(HoneyTokenKind.ApiKey);
        token.Placement.ShouldBe("placement of key");
        token.Name.ShouldBe("Legacy CI key");
        token.Description.ShouldBe("Key left in an old pipeline variable.");
        token.Tags["env"].ShouldBe("production");
    }

    [Theory]
    [MemberData(nameof(KindAndDomainMatrix))]
    public void Enforces_the_kind_and_trigger_domain_matrix(HoneyTokenKind kind, TriggerDomain? domain, bool accepted)
    {
        HoneyTrigger[] triggers = domain is { } d ? [Trigger(d, "fictional-value")] : [];

        var result = TryBuild(Registration("decoy", kind, triggers) with { Class = DecoyClass.Unique });

        (result.Registry is not null).ShouldBe(accepted);
        result.Errors.Count.ShouldBe(accepted ? 0 : 1);
    }

    [Fact]
    public void Rejects_duplicate_ids()
    {
        var result = TryBuild(
            Registration("key", HoneyTokenKind.ApiKey, Trigger(TriggerDomain.ApiKey, "fictional-key-1")),
            Registration("key", HoneyTokenKind.ApiKey, Trigger(TriggerDomain.ApiKey, "fictional-key-2")));

        result.Registry.ShouldBeNull();
        result.Errors.ShouldHaveSingleItem().TokenId.ShouldBe("key");
    }

    [Fact]
    public void Rejects_triggers_shared_between_tokens()
    {
        var result = TryBuild(
            Registration("first", HoneyTokenKind.ApiKey, Trigger(TriggerDomain.ApiKey, "fictional-key")),
            Registration("second", HoneyTokenKind.Config, Trigger(TriggerDomain.ApiKey, "fictional-key")));

        result.Registry.ShouldBeNull();
        var error = result.Errors.ShouldHaveSingleItem();
        error.TokenId.ShouldBe("second");
        error.Message.ShouldContain("first");
    }

    [Fact]
    public void Allows_the_same_value_in_different_domains_of_different_tokens()
    {
        var registry = Build(
            Registration("key", HoneyTokenKind.ApiKey, Trigger(TriggerDomain.ApiKey, "fictional-value")),
            Registration("user", HoneyTokenKind.User, Trigger(TriggerDomain.Username, "fictional-value")) with { Class = DecoyClass.Unique });

        registry.Tokens.Count.ShouldBe(2);
    }

    [Fact]
    public void Rejects_repeated_triggers_in_a_token()
    {
        var trigger = Trigger(TriggerDomain.Username, "fictional-user");

        var result = TryBuild(Registration("file", HoneyTokenKind.File, trigger, trigger));

        result.Registry.ShouldBeNull();
        result.Errors.ShouldHaveSingleItem().TokenId.ShouldBe("file");
    }

    [Fact]
    public void Rejects_uninitialized_digests()
    {
        var result = TryBuild(Registration("key", HoneyTokenKind.ApiKey, new HoneyTrigger(TriggerDomain.ApiKey, default)));

        result.Registry.ShouldBeNull();
        result.Errors.ShouldHaveSingleItem().TokenId.ShouldBe("key");
    }

    [Fact]
    public void Rejects_undefined_trigger_domains()
    {
        var result = TryBuild(Registration("file", HoneyTokenKind.File, new HoneyTrigger((TriggerDomain)9, Trigger(TriggerDomain.ApiKey, "fictional-key").Digest)));

        result.Registry.ShouldBeNull();
        result.Errors.ShouldHaveSingleItem().TokenId.ShouldBe("file");
    }

    [Fact]
    public void Allows_tokens_that_share_a_placement()
    {
        var registry = Build(
            Registration("first", HoneyTokenKind.File, Trigger(TriggerDomain.ApiKey, "fictional-key-1")) with { Placement = "backups bucket" },
            Registration("second", HoneyTokenKind.File, Trigger(TriggerDomain.ApiKey, "fictional-key-2")) with { Placement = "backups bucket" });

        registry.Tokens.Count.ShouldBe(2);
    }

    [Fact]
    public void Reports_every_problem_at_once()
    {
        var result = TryBuild(
            Registration("Not A Slug", HoneyTokenKind.ApiKey, Trigger(TriggerDomain.ApiKey, "fictional-key-1")),
            Registration("blank-placement", HoneyTokenKind.ApiKey, Trigger(TriggerDomain.ApiKey, "fictional-key-2")) with { Placement = " " },
            Registration("bad-kind", (HoneyTokenKind)42),
            Registration("no-class", HoneyTokenKind.User, Trigger(TriggerDomain.Username, "fictional-user")),
            Registration("many-problems", HoneyTokenKind.Row, Trigger(TriggerDomain.ApiKey, "fictional-key-3")) with { Name = "", Severity = (Severity)0 },
            Registration("valid", HoneyTokenKind.ApiKey, Trigger(TriggerDomain.ApiKey, "fictional-key-4")));

        result.Registry.ShouldBeNull();
        result.Errors.Count(e => e.TokenId is null).ShouldBe(1);
        result.Errors.Single(e => e.TokenId is null).Message.ShouldContain("position 1");
        result.Errors.Select(e => e.TokenId).Where(id => id is not null).Distinct()
            .ShouldBe(["blank-placement", "bad-kind", "no-class", "many-problems"], ignoreOrder: true);
        result.Errors.Count(e => e.TokenId == "many-problems").ShouldBe(3);
    }

    [Fact]
    public void Error_messages_never_contain_digests_or_values()
    {
        const string DecoyValue = "fictional-key-that-must-not-leak";
        var trigger = Trigger(TriggerDomain.ApiKey, DecoyValue);

        var result = TryBuild(
            Registration("first", HoneyTokenKind.ApiKey, trigger),
            Registration("second", HoneyTokenKind.ApiKey, trigger),
            Registration("third", HoneyTokenKind.Row, trigger, trigger),
            Registration("fourth", HoneyTokenKind.User, trigger));

        result.Errors.ShouldNotBeEmpty();
        foreach (var error in result.Errors)
        {
            error.Message.ShouldNotContain(trigger.Digest.ToString());
            error.Message.ShouldNotContain(DecoyValue);
            error.ToString().ShouldNotContain(trigger.Digest.ToString());
        }
    }

    [Fact]
    public void Indexes_the_triggers_it_validated()
    {
        var validated = Trigger(TriggerDomain.ApiKey, "fictional-key-validated");
        var swapped = Trigger(TriggerDomain.ApiKey, "fictional-key-swapped");

        var registry = Build(Registration("file", HoneyTokenKind.File) with { Triggers = new ChangingList<HoneyTrigger>([validated], [swapped]) });

        registry.TryMatch(TriggerDomain.ApiKey, "fictional-key-validated", out _).ShouldBeTrue();
        registry.TryMatch(TriggerDomain.ApiKey, "fictional-key-swapped", out _).ShouldBeFalse();
    }

    [Fact]
    public void Treats_missing_triggers_as_none()
    {
        var result = TryBuild(
            Registration("row", HoneyTokenKind.Row) with { Triggers = null! },
            Registration("key", HoneyTokenKind.ApiKey) with { Triggers = null! });

        result.Registry.ShouldBeNull();
        result.Errors.ShouldHaveSingleItem().TokenId.ShouldBe("key");
    }

    [Fact]
    public void Builds_an_empty_registry()
    {
        var registry = Build();

        registry.Tokens.ShouldBeEmpty();
        foreach (var domain in Enum.GetValues<TriggerDomain>())
        {
            registry.HasTriggers(domain).ShouldBeFalse();
        }
    }
}
