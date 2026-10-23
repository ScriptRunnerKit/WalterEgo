namespace WalterEgo.Core.Tests;

public sealed class HmacSecretHasherTests
{
    private const string DecoyValue = "fictional-decoy-0001";

    // Computed outside .NET with a hand-written RFC 5869 HKDF over Python's hmac and hashlib modules:
    // subkey = HKDF-SHA256(ikm = bytes 0..31, salt = empty, info = "walterego/v1/<label>", 32 bytes),
    // digest = HMAC-SHA256(subkey, UTF-8 of the value), written in base64url without padding.
    public static TheoryData<TriggerDomain, string, string> KnownAnswers => new()
    {
        { TriggerDomain.ApiKey, DecoyValue, "j6fz20sq7UH8J6nWbAokioefCSdWXmGbKXZzFxZPlMM" },
        { TriggerDomain.Username, DecoyValue, "LaEmDk6L4MTQn5gfGSTpp7C_H0nG2CjY6bzwD9aXFJw" },
        { TriggerDomain.SessionToken, DecoyValue, "j_wdZfRB9ob6azJRrCB2SPiE5X_GTbNiId2r0M1GxUQ" },
        { TriggerDomain.RequestPath, DecoyValue, "_y9SJ1mKg7jScFWykDZubA4daCjXWPvApIXCfsk8AOY" },
        { TriggerDomain.ApiKey, "isca-ção-\U0001F989", "6FwGhcCwJc3_xsnip0QOxVeZ46AU71Pb0stPYyibJfM" },
        { TriggerDomain.ApiKey, "a\uD800b", "RHPMn3bH8027nk3FCbUejZlHyf6TDbPV2ve7Tlj9Xxc" },
    };

    [Theory]
    [InlineData(0)]
    [InlineData(16)]
    [InlineData(31)]
    public void Rejects_keys_shorter_than_32_bytes(int length)
    {
        Should.Throw<ArgumentException>(() => new HmacSecretHasher(new byte[length]));
    }

    [Theory]
    [InlineData(32)]
    [InlineData(64)]
    public void Accepts_keys_of_32_bytes_or_more(int length)
    {
        var hasher = new HmacSecretHasher(new byte[length]);

        hasher.ComputeDigest(TriggerDomain.ApiKey, DecoyValue).IsDefault.ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(KnownAnswers))]
    public void Digest_matches_the_known_answer_vector_for_each_domain(TriggerDomain domain, string value, string expected)
    {
        var hasher = new HmacSecretHasher(TestKeys.Sequential);

        hasher.ComputeDigest(domain, value).ToString().ShouldBe(expected);
    }

    [Fact]
    public void Separates_digests_by_domain()
    {
        var hasher = new HmacSecretHasher(TestKeys.Sequential);

        var digests = Enum.GetValues<TriggerDomain>().Select(domain => hasher.ComputeDigest(domain, DecoyValue)).ToList();

        digests.Distinct().Count().ShouldBe(digests.Count);
    }

    [Fact]
    public void Same_input_produces_the_same_digest()
    {
        var first = new HmacSecretHasher(TestKeys.Sequential).ComputeDigest(TriggerDomain.Username, DecoyValue);
        var second = new HmacSecretHasher(TestKeys.Sequential).ComputeDigest(TriggerDomain.Username, DecoyValue);

        second.ShouldBe(first);
    }

    [Fact]
    public void Different_keys_produce_different_digests()
    {
        var first = new HmacSecretHasher(TestKeys.Sequential).ComputeDigest(TriggerDomain.ApiKey, DecoyValue);
        var second = new HmacSecretHasher(TestKeys.Reversed).ComputeDigest(TriggerDomain.ApiKey, DecoyValue);

        second.ShouldNotBe(first);
    }

    [Theory]
    [InlineData("admin", "Admin")]
    [InlineData("admin", " admin")]
    [InlineData("admin", "admin ")]
    [InlineData("/.env", "/.ENV")]
    public void Hashes_the_value_exactly_as_given(string value, string variant)
    {
        var hasher = new HmacSecretHasher(TestKeys.Sequential);

        hasher.ComputeDigest(TriggerDomain.Username, variant).ShouldNotBe(hasher.ComputeDigest(TriggerDomain.Username, value));
    }

    [Fact]
    public void Rejects_empty_values_and_undefined_domains()
    {
        var hasher = new HmacSecretHasher(TestKeys.Sequential);

        Should.Throw<ArgumentException>(() => hasher.ComputeDigest(TriggerDomain.ApiKey, ""));
        Should.Throw<ArgumentOutOfRangeException>(() => hasher.ComputeDigest(0, DecoyValue));
        Should.Throw<ArgumentOutOfRangeException>(() => hasher.ComputeDigest((TriggerDomain)5, DecoyValue));
    }

    [Fact]
    public void Hashes_values_longer_than_the_stack_buffer()
    {
        var hasher = new HmacSecretHasher(TestKeys.Sequential);
        var longValue = new string('x', 5000);

        var digest = hasher.ComputeDigest(TriggerDomain.SessionToken, longValue);

        digest.ShouldBe(hasher.ComputeDigest(TriggerDomain.SessionToken, longValue));
        digest.ShouldNotBe(hasher.ComputeDigest(TriggerDomain.SessionToken, longValue.AsSpan(0, 4999)));
    }

    [Fact]
    public void String_representation_hides_the_key()
    {
        var text = new HmacSecretHasher(TestKeys.Sequential).ToString()!;

        text.ShouldNotContain(Convert.ToBase64String(TestKeys.Sequential));
        text.ShouldNotContain(Convert.ToHexString(TestKeys.Sequential), Case.Insensitive);
        text.ShouldNotContain(Convert.ToBase64String(TestKeys.Sequential).TrimEnd('=').Replace('+', '-').Replace('/', '_'));
    }
}
