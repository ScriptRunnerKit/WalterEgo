namespace WalterEgo.Core.Tests;

public sealed class SecretDigestTests
{
    private static readonly byte[] SequentialBytes = [.. Enumerable.Range(0, SecretDigest.Length).Select(i => (byte)i)];

    private const string SequentialText = "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8";

    public static TheoryData<string> NonCanonicalTexts =>
    [
        "",
        SequentialText[..42],
        SequentialText + "A",
        SequentialText + "=",
        "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=",
        "AAECAwQFBgcICQoL DA0ODxAREhMUFRYXGBkaGxwdHh",
        " AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh",
        "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwd+h8",
        "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwd/h8",
        "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh9",
        "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh*",
    ];

    [Fact]
    public void Round_trips_through_its_base64url_text()
    {
        var digest = SecretDigest.FromBytes(SequentialBytes);

        var text = digest.ToString();

        text.ShouldBe(SequentialText);
        text.Length.ShouldBe(43);
        SecretDigest.Parse(text).ShouldBe(digest);
        SecretDigest.TryParse(text, out var parsed).ShouldBeTrue();
        parsed.ShouldBe(digest);
        parsed.AsSpan().ToArray().ShouldBe(SequentialBytes);
    }

    [Theory]
    [MemberData(nameof(NonCanonicalTexts))]
    public void Rejects_non_canonical_text(string text)
    {
        SecretDigest.TryParse(text, out var parsed).ShouldBeFalse();
        parsed.ShouldBe(default);
        Should.Throw<FormatException>(() => SecretDigest.Parse(text));
    }

    [Fact]
    public void Equality_compares_every_byte()
    {
        var digest = SecretDigest.FromBytes(SequentialBytes);
        var same = SecretDigest.FromBytes(SequentialBytes);
        byte[] lastByteChanged = [.. SequentialBytes];
        lastByteChanged[^1] ^= 1;
        var different = SecretDigest.FromBytes(lastByteChanged);

        (digest == same).ShouldBeTrue();
        digest.Equals((object)same).ShouldBeTrue();
        digest.GetHashCode().ShouldBe(same.GetHashCode());
        (digest != different).ShouldBeTrue();
        digest.Equals(different).ShouldBeFalse();
        digest.Equals((object)SequentialText).ShouldBeFalse();
    }

    [Fact]
    public void Default_digest_is_flagged()
    {
        default(SecretDigest).IsDefault.ShouldBeTrue();
        SecretDigest.FromBytes(SequentialBytes).IsDefault.ShouldBeFalse();
    }

    [Fact]
    public void Requires_exactly_32_bytes()
    {
        Should.Throw<ArgumentException>(() => SecretDigest.FromBytes(new byte[31]));
        Should.Throw<ArgumentException>(() => SecretDigest.FromBytes(new byte[33]));
    }
}
