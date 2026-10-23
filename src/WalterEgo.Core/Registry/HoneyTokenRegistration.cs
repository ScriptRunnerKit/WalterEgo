namespace WalterEgo;

// Raw input for the registry builder. Every field may be invalid; the builder reports what is wrong instead of
// throwing, so a configuration with several mistakes is fixed in one pass.
internal sealed record HoneyTokenRegistration(string? Id, HoneyTokenKind Kind, string? Placement)
{
    public DecoyClass? Class { get; init; }

    public Severity? Severity { get; init; }

    public string? Name { get; init; }

    public string? Description { get; init; }

    public IReadOnlyDictionary<string, string>? Tags { get; init; }

    public IReadOnlyList<HoneyTrigger> Triggers { get; init; } = [];
}
