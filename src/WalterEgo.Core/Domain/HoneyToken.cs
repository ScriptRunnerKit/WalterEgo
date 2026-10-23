using System.Collections.Frozen;

namespace WalterEgo;

/// <summary>
/// An alter ego: a fake asset that no legitimate user has a reason to touch. A trip of an alter ego tells where
/// it was planted (<see cref="Placement"/>) and how much the trip can be trusted (<see cref="Class"/>).
/// </summary>
/// <remarks>
/// An instance only carries metadata. The decoy values that trip the alter ego are never part of it, so it can be
/// copied into events, logs and alert channels safely. Instances are immutable and always valid.
/// </remarks>
public sealed record HoneyToken
{
    /// <summary>
    /// Creates an alter ego.
    /// </summary>
    /// <param name="id">The identifier of the alter ego.</param>
    /// <param name="kind">The kind of asset the alter ego imitates.</param>
    /// <param name="decoyClass">Whether the alter ego is a generic decoy or a unique one.</param>
    /// <param name="placement">
    /// Where the alter ego was planted, such as "LEGACY_KEY variable in the CI of the orders repository". Up to 512
    /// characters, not blank, without control characters or line separators.
    /// </param>
    /// <param name="severity">The severity of a trip of the alter ego.</param>
    /// <param name="name">
    /// A readable name. Up to 128 characters, not blank, without control characters or line separators. When
    /// <see langword="null"/>, the identifier is used.
    /// </param>
    /// <param name="description">
    /// An optional description. Up to 1024 characters, without control characters or line separators.
    /// </param>
    /// <param name="tags">
    /// Optional tags. Up to 16 entries; keys are lowercase slugs of up to 32 characters; values are not blank, have up
    /// to 128 characters and no control characters or line separators. The entries are copied.
    /// </param>
    /// <exception cref="ArgumentException">Any argument breaks the rules above, <paramref name="id"/> is the default
    /// value, or an enumeration argument is not a defined value.</exception>
    public HoneyToken(
        HoneyTokenId id,
        HoneyTokenKind kind,
        DecoyClass decoyClass,
        string placement,
        Severity severity,
        string? name = null,
        string? description = null,
        IReadOnlyDictionary<string, string>? tags = null)
    {
        if (id.IsDefault)
        {
            throw new ArgumentException("The identifier is required.", nameof(id));
        }

        ThrowIfUndefined(HoneyTokenRules.IsDefined(kind), nameof(kind));
        ThrowIfUndefined(HoneyTokenRules.IsDefined(decoyClass), nameof(decoyClass));
        ThrowIfUndefined(HoneyTokenRules.IsDefined(severity), nameof(severity));
        ThrowIfInvalid(HoneyTokenRules.CheckPlacement(placement), nameof(placement));
        ThrowIfInvalid(HoneyTokenRules.CheckName(name), nameof(name));
        ThrowIfInvalid(HoneyTokenRules.CheckDescription(description), nameof(description));

        // The copy is validated, not the argument, so a dictionary that changes after the check cannot slip invalid
        // tags in.
        var frozenTags = tags?.ToFrozenDictionary(StringComparer.Ordinal);
        ThrowIfInvalid(HoneyTokenRules.CheckTags(frozenTags), nameof(tags));

        Id = id;
        Kind = kind;
        Class = decoyClass;
        Placement = placement;
        Severity = severity;
        Name = name ?? id.Value;
        Description = description;
        Tags = frozenTags ?? FrozenDictionary<string, string>.Empty;
    }

    /// <summary>Gets the identifier of the alter ego.</summary>
    public HoneyTokenId Id { get; }

    /// <summary>Gets the kind of asset the alter ego imitates.</summary>
    public HoneyTokenKind Kind { get; }

    /// <summary>Gets whether the alter ego is a generic decoy or a unique one.</summary>
    public DecoyClass Class { get; }

    /// <summary>Gets the readable name, which is the identifier when no name was given.</summary>
    public string Name { get; }

    /// <summary>Gets where the alter ego was planted.</summary>
    public string Placement { get; }

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; }

    /// <summary>Gets the severity of a trip of the alter ego.</summary>
    public Severity Severity { get; }

    /// <summary>Gets the tags, compared by ordinal keys. Empty when no tags were given.</summary>
    public IReadOnlyDictionary<string, string> Tags { get; }

    /// <summary>
    /// Compares two alter egos by all their values, including the content of <see cref="Tags"/>.
    /// </summary>
    /// <param name="other">The alter ego to compare with.</param>
    /// <returns><see langword="true"/> when both alter egos have the same values.</returns>
    public bool Equals(HoneyToken? other) =>
        other is not null
        && Id == other.Id
        && Kind == other.Kind
        && Class == other.Class
        && Severity == other.Severity
        && string.Equals(Name, other.Name, StringComparison.Ordinal)
        && string.Equals(Placement, other.Placement, StringComparison.Ordinal)
        && string.Equals(Description, other.Description, StringComparison.Ordinal)
        && TagsEqual(Tags, other.Tags);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        HashCode.Combine(Id, Kind, Class, Severity, Name, Placement, Description, Tags.Count);

    private static bool TagsEqual(IReadOnlyDictionary<string, string> left, IReadOnlyDictionary<string, string> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        foreach (var (key, value) in left)
        {
            if (!right.TryGetValue(key, out var otherValue) || !string.Equals(value, otherValue, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static void ThrowIfUndefined(bool isDefined, string parameterName)
    {
        if (!isDefined)
        {
            throw new ArgumentException("The value is not defined in its enumeration.", parameterName);
        }
    }

    private static void ThrowIfInvalid(string? error, string parameterName)
    {
        if (error is not null)
        {
            throw new ArgumentException(error, parameterName);
        }
    }
}
