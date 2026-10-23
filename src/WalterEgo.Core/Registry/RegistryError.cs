namespace WalterEgo;

// TokenId is null when the identifier itself is invalid; the message then points to the position of the entry.
internal sealed record RegistryError(string? TokenId, string Message);

internal sealed record TokenRegistryBuildResult(TokenRegistry? Registry, IReadOnlyList<RegistryError> Errors);
