using System.Diagnostics.CodeAnalysis;

namespace WalterEgo;

/// <summary>
/// Identifies an alter ego. The identifier is chosen by the user, must stay the same across restarts and is
/// included in every alert about the alter ego.
/// </summary>
/// <remarks>
/// A valid identifier has 1 to 64 characters, starts with a lowercase ASCII letter or digit and contains only
/// lowercase ASCII letters, digits, <c>-</c>, <c>.</c> and <c>_</c>. Identifiers compare ordinally.
/// The default value is not a valid identifier and is rejected wherever an identifier is required.
/// </remarks>
public readonly record struct HoneyTokenId
{
    private readonly string? _value;

    /// <summary>
    /// Creates an identifier.
    /// </summary>
    /// <param name="value">The identifier text.</param>
    /// <exception cref="ArgumentException"><paramref name="value"/> is not a valid identifier.</exception>
    public HoneyTokenId(string value)
    {
        if (!HoneyTokenRules.IsSlug(value, HoneyTokenRules.MaxIdLength))
        {
            throw new ArgumentException(HoneyTokenRules.InvalidIdMessage, nameof(value));
        }

        _value = value;
    }

    /// <summary>
    /// Gets the identifier text, or an empty string for the default value.
    /// </summary>
    public string Value => _value ?? string.Empty;

    internal bool IsDefault => _value is null;

    /// <summary>
    /// Tries to create an identifier.
    /// </summary>
    /// <param name="value">The identifier text.</param>
    /// <param name="id">The identifier, or the default value when <paramref name="value"/> is not valid.</param>
    /// <returns><see langword="true"/> when <paramref name="value"/> is a valid identifier.</returns>
    public static bool TryCreate([NotNullWhen(true)] string? value, out HoneyTokenId id)
    {
        if (!HoneyTokenRules.IsSlug(value, HoneyTokenRules.MaxIdLength))
        {
            id = default;
            return false;
        }

        id = new HoneyTokenId(value);
        return true;
    }

    /// <summary>
    /// Returns the identifier text.
    /// </summary>
    /// <returns>The identifier text, or an empty string for the default value.</returns>
    public override string ToString() => Value;
}
