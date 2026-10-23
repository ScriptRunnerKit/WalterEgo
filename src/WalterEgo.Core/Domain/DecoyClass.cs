namespace WalterEgo;

/// <summary>
/// How much a trip of an alter ego can be trusted.
/// </summary>
public enum DecoyClass
{
    /// <summary>
    /// A well-known decoy that any scanner finds without a leak, such as <c>/.env</c> or <c>admin</c>.
    /// Its trips signal reconnaissance and carry low confidence.
    /// </summary>
    Generic = 1,

    /// <summary>
    /// A decoy that can only be found through the place where it was planted.
    /// Its trips signal that this place leaked and carry high confidence.
    /// </summary>
    Unique = 2,
}
