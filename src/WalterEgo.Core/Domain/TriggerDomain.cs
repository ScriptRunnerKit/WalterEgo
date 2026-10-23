namespace WalterEgo;

/// <summary>
/// Where a sensor looks for a decoy value. The same value produces a different digest in each domain.
/// </summary>
public enum TriggerDomain
{
    /// <summary>The value of an API key presented to the application.</summary>
    ApiKey = 1,

    /// <summary>A username or e-mail address used in a sign-in attempt.</summary>
    Username = 2,

    /// <summary>A session token, such as a bearer token or a session cookie.</summary>
    SessionToken = 3,

    /// <summary>The path of an HTTP request, such as a beacon or a unique honey endpoint.</summary>
    RequestPath = 4,
}
