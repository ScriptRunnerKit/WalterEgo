namespace WalterEgo;

/// <summary>
/// The kind of asset an alter ego imitates.
/// </summary>
public enum HoneyTokenKind
{
    /// <summary>An HTTP route that no legitimate client calls.</summary>
    Endpoint = 1,

    /// <summary>A username or e-mail address that belongs to nobody.</summary>
    User = 2,

    /// <summary>An API key in the application's own format that never authorizes anything.</summary>
    ApiKey = 3,

    /// <summary>A fictitious database row.</summary>
    Row = 4,

    /// <summary>A configuration entry that carries a beacon or another decoy value.</summary>
    Config = 5,

    /// <summary>A session token, such as a JWT or a cookie, that never validates.</summary>
    SessionToken = 6,

    /// <summary>A file that carries decoy credentials or a beacon.</summary>
    File = 7,
}
