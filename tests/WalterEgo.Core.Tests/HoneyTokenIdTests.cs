namespace WalterEgo.Core.Tests;

public sealed class HoneyTokenIdTests
{
    public static TheoryData<string> ValidIds =>
    [
        "a",
        "7",
        "honey-endpoint-env",
        "ci.legacy_key-2",
        "0abc",
        new string('a', 64),
    ];

    public static TheoryData<string> InvalidIds =>
    [
        "",
        " ",
        "Honey",
        "honey key",
        "-leading-dash",
        ".leading-dot",
        "_leading-underscore",
        "isca-ção",
        "tab\tinside",
        "slash/inside",
        new string('a', 65),
    ];

    [Theory]
    [MemberData(nameof(ValidIds))]
    public void Accepts_lowercase_slugs_up_to_64_characters(string value)
    {
        var id = new HoneyTokenId(value);

        id.Value.ShouldBe(value);
        id.ToString().ShouldBe(value);
        HoneyTokenId.TryCreate(value, out var created).ShouldBeTrue();
        created.ShouldBe(id);
    }

    [Theory]
    [MemberData(nameof(InvalidIds))]
    public void Rejects_values_outside_the_slug_format(string value)
    {
        Should.Throw<ArgumentException>(() => new HoneyTokenId(value));
        HoneyTokenId.TryCreate(value, out var created).ShouldBeFalse();
        created.ShouldBe(default);
    }

    [Fact]
    public void Rejects_null()
    {
        Should.Throw<ArgumentException>(() => new HoneyTokenId(null!));
        HoneyTokenId.TryCreate(null, out _).ShouldBeFalse();
    }

    [Fact]
    public void Ids_compare_ordinally()
    {
        new HoneyTokenId("abc").ShouldBe(new HoneyTokenId("abc"));
        new HoneyTokenId("abc").ShouldNotBe(new HoneyTokenId("abd"));
    }
}
