namespace WalterEgo;

/// <summary>
/// The severity of a trip.
/// </summary>
public enum Severity
{
    /// <summary>Low severity, the default for generic decoys.</summary>
    Low = 1,

    /// <summary>Medium severity.</summary>
    Medium = 2,

    /// <summary>High severity, the default for unique decoys.</summary>
    High = 3,

    /// <summary>Critical severity.</summary>
    Critical = 4,
}
