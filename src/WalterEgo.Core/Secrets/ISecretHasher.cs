namespace WalterEgo;

/// <summary>
/// Computes the keyed digest of a decoy value.
/// </summary>
/// <remarks>
/// A subkey is derived from a secret key for each <see cref="TriggerDomain"/>, so the same value has a different
/// digest in each domain, and nobody without the key can compute a digest or test whether a value is a decoy.
/// The value is hashed as UTF-8 exactly as given, with invalid UTF-16 replaced by U+FFFD. Any normalization, such
/// as the case of a username, must happen before.
/// </remarks>
public interface ISecretHasher
{
    /// <summary>
    /// Computes the digest of a value.
    /// </summary>
    /// <param name="domain">Where the value is looked for.</param>
    /// <param name="value">The value to hash.</param>
    /// <returns>The digest of <paramref name="value"/> in <paramref name="domain"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="value"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="domain"/> is not a defined value.</exception>
    SecretDigest ComputeDigest(TriggerDomain domain, ReadOnlySpan<char> value);
}
