using System.Diagnostics.CodeAnalysis;

namespace WalterEgo;

/// <summary>
/// The immutable set of alter egos of the application, built once at startup.
/// </summary>
/// <remarks>
/// The registry stores only keyed digests of decoy values, never the values themselves. Lookups never throw because
/// of their arguments and do not allocate for values of up to 1024 UTF-8 bytes (longer values use pooled buffers),
/// so sensors can call them on every request. Instances are safe for concurrent use.
/// </remarks>
public interface ITokenRegistry
{
    /// <summary>
    /// Gets every alter ego, ordered by identifier (ordinal).
    /// </summary>
    IReadOnlyList<HoneyToken> Tokens { get; }

    /// <summary>
    /// Finds an alter ego by identifier.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="token">The alter ego, or <see langword="null"/> when none has <paramref name="id"/>.</param>
    /// <returns><see langword="true"/> when an alter ego has <paramref name="id"/>.</returns>
    bool TryGet(HoneyTokenId id, [NotNullWhen(true)] out HoneyToken? token);

    /// <summary>
    /// Tells whether any alter ego has a trigger in a domain. When it returns <see langword="false"/>, a sensor can
    /// skip that domain entirely.
    /// </summary>
    /// <param name="domain">The domain.</param>
    /// <returns><see langword="true"/> when at least one alter ego has a trigger in <paramref name="domain"/>.</returns>
    bool HasTriggers(TriggerDomain domain);

    /// <summary>
    /// Finds the alter ego tripped by a value seen in a domain.
    /// </summary>
    /// <param name="domain">Where the value was seen.</param>
    /// <param name="value">
    /// The value as seen by the sensor, after the normalization of its domain. It is hashed and compared with the
    /// stored digests; the value itself is not kept.
    /// </param>
    /// <param name="token">The alter ego, or <see langword="null"/> when the value is not a decoy.</param>
    /// <returns><see langword="true"/> when <paramref name="value"/> is a decoy value in <paramref name="domain"/>.</returns>
    bool TryMatch(TriggerDomain domain, ReadOnlySpan<char> value, [NotNullWhen(true)] out HoneyToken? token);
}
