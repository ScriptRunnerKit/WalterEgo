namespace WalterEgo.Core.Tests;

public sealed class HoneyTokenTests
{
    private static readonly HoneyTokenId Id = new("ci-legacy-key");

    private const string Placement = "LEGACY_KEY variable in the CI of the orders repository";

    public static TheoryData<string> InvalidArguments =>
    [
        "default id",
        "undefined kind",
        "undefined class",
        "undefined severity",
        "null placement",
        "empty placement",
        "blank placement",
        "long placement",
        "placement with a line break",
        "placement with a line separator",
        "blank name",
        "long name",
        "name with a control character",
        "long description",
        "description with a tab",
        "too many tags",
        "tag key not a slug",
        "long tag key",
        "empty tag value",
        "long tag value",
        "tag value with a line break",
    ];

    [Theory]
    [MemberData(nameof(InvalidArguments))]
    public void Rejects_invalid_arguments(string scenario)
    {
        Func<HoneyToken> create = scenario switch
        {
            "default id" => () => new HoneyToken(default, HoneyTokenKind.ApiKey, DecoyClass.Unique, Placement, Severity.High),
            "undefined kind" => () => Create(kind: (HoneyTokenKind)99),
            "undefined class" => () => Create(decoyClass: 0),
            "undefined severity" => () => Create(severity: (Severity)5),
            "null placement" => () => Create(placement: null!),
            "empty placement" => () => Create(placement: ""),
            "blank placement" => () => Create(placement: "   "),
            "long placement" => () => Create(placement: new string('p', 513)),
            "placement with a line break" => () => Create(placement: "line one\nline two"),
            "placement with a line separator" => () => Create(placement: "line one\u2028line two"),
            "blank name" => () => Create(name: " "),
            "long name" => () => Create(name: new string('n', 129)),
            "name with a control character" => () => Create(name: "bell\u0007"),
            "long description" => () => Create(description: new string('d', 1025)),
            "description with a tab" => () => Create(description: "a\tb"),
            "too many tags" => () => Create(tags: Enumerable.Range(0, 17).ToDictionary(i => $"k{i}", i => "v")),
            "tag key not a slug" => () => Create(tags: new Dictionary<string, string> { ["Env"] = "production" }),
            "long tag key" => () => Create(tags: new Dictionary<string, string> { [new string('k', 33)] = "v" }),
            "empty tag value" => () => Create(tags: new Dictionary<string, string> { ["env"] = "" }),
            "long tag value" => () => Create(tags: new Dictionary<string, string> { ["env"] = new string('v', 129) }),
            "tag value with a line break" => () => Create(tags: new Dictionary<string, string> { ["env"] = "a\r\nb" }),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario)),
        };

        Should.Throw<ArgumentException>(create);
    }

    [Fact]
    public void Accepts_values_at_the_limits()
    {
        var token = Create(
            placement: new string('p', 512),
            name: new string('n', 128),
            description: new string('d', 1024),
            tags: Enumerable.Range(0, 16).ToDictionary(i => $"{i}{new string('k', 30)}", _ => new string('v', 128)));

        token.Placement.Length.ShouldBe(512);
        token.Name.Length.ShouldBe(128);
        token.Description!.Length.ShouldBe(1024);
        token.Tags.Count.ShouldBe(16);
    }

    [Fact]
    public void Keeps_the_given_values()
    {
        var token = new HoneyToken(
            Id,
            HoneyTokenKind.ApiKey,
            DecoyClass.Unique,
            Placement,
            Severity.Critical,
            name: "Legacy CI key",
            description: "Key left in an old pipeline variable.",
            tags: new Dictionary<string, string> { ["env"] = "production" });

        token.Id.ShouldBe(Id);
        token.Kind.ShouldBe(HoneyTokenKind.ApiKey);
        token.Class.ShouldBe(DecoyClass.Unique);
        token.Placement.ShouldBe(Placement);
        token.Severity.ShouldBe(Severity.Critical);
        token.Name.ShouldBe("Legacy CI key");
        token.Description.ShouldBe("Key left in an old pipeline variable.");
        token.Tags.ShouldBe(new Dictionary<string, string> { ["env"] = "production" });
    }

    [Fact]
    public void Name_defaults_to_the_id()
    {
        Create(name: null).Name.ShouldBe(Id.Value);
    }

    [Fact]
    public void Tags_are_a_frozen_copy()
    {
        var source = new Dictionary<string, string> { ["env"] = "production" };

        var token = Create(tags: source);
        source["env"] = "staging";
        source["team"] = "orders";

        token.Tags.Count.ShouldBe(1);
        token.Tags["env"].ShouldBe("production");
    }

    [Fact]
    public void Tags_are_validated_after_being_copied()
    {
        var tags = new ChangingDictionary(
            new Dictionary<string, string> { ["env"] = "production" },
            new Dictionary<string, string> { ["Not A Slug"] = "line\nbreak" });

        var token = Create(tags: tags);

        token.Tags.ShouldBe(new Dictionary<string, string> { ["env"] = "production" });
    }

    [Fact]
    public void Tags_default_to_empty()
    {
        Create(tags: null).Tags.ShouldBeEmpty();
    }

    [Fact]
    public void Equality_compares_tags_by_content()
    {
        var first = Create(tags: new Dictionary<string, string> { ["env"] = "production", ["team"] = "orders" });
        var second = Create(tags: new Dictionary<string, string> { ["team"] = "orders", ["env"] = "production" });
        var different = Create(tags: new Dictionary<string, string> { ["env"] = "staging", ["team"] = "orders" });

        first.ShouldBe(second);
        first.GetHashCode().ShouldBe(second.GetHashCode());
        first.ShouldNotBe(different);
        first.ShouldNotBe(Create(severity: Severity.Low, tags: new Dictionary<string, string> { ["env"] = "production", ["team"] = "orders" }));
    }

    private static HoneyToken Create(
        HoneyTokenId? id = null,
        HoneyTokenKind kind = HoneyTokenKind.ApiKey,
        DecoyClass decoyClass = DecoyClass.Unique,
        string placement = Placement,
        Severity severity = Severity.High,
        string? name = null,
        string? description = null,
        IReadOnlyDictionary<string, string>? tags = null) =>
        new(id ?? Id, kind, decoyClass, placement, severity, name, description, tags);
}
