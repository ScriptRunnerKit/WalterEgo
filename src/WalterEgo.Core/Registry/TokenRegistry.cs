using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace WalterEgo;

internal sealed class TokenRegistry : ITokenRegistry
{
    private readonly ISecretHasher _hasher;
    private readonly FrozenDictionary<HoneyTokenId, HoneyToken> _byId;

    // Indexed by the numeric value of TriggerDomain; null when no alter ego has a trigger in that domain.
    private readonly FrozenDictionary<SecretDigest, HoneyToken>?[] _byDomain = new FrozenDictionary<SecretDigest, HoneyToken>?[(int)TriggerDomain.RequestPath + 1];

    internal TokenRegistry(ISecretHasher hasher, IReadOnlyList<(HoneyToken Token, IReadOnlyList<HoneyTrigger> Triggers)> entries)
    {
        _hasher = hasher;
        _byId = entries.ToFrozenDictionary(entry => entry.Token.Id, entry => entry.Token);
        Tokens = Array.AsReadOnly([.. entries.Select(entry => entry.Token).OrderBy(token => token.Id.Value, StringComparer.Ordinal)]);

        foreach (var group in entries.SelectMany(entry => entry.Triggers, (entry, trigger) => (trigger, entry.Token)).GroupBy(pair => pair.trigger.Domain))
        {
            _byDomain[(int)group.Key] = group.ToFrozenDictionary(pair => pair.trigger.Digest, pair => pair.Token);
        }
    }

    public IReadOnlyList<HoneyToken> Tokens { get; }

    public bool TryGet(HoneyTokenId id, [NotNullWhen(true)] out HoneyToken? token)
    {
        if (id.IsDefault)
        {
            token = null;
            return false;
        }

        return _byId.TryGetValue(id, out token);
    }

    public bool HasTriggers(TriggerDomain domain) => Index(domain) is not null;

    public bool TryMatch(TriggerDomain domain, ReadOnlySpan<char> value, [NotNullWhen(true)] out HoneyToken? token)
    {
        // Returning before hashing keeps requests cheap when a domain has no decoys.
        if (value.IsEmpty || Index(domain) is not { } index)
        {
            token = null;
            return false;
        }

        return index.TryGetValue(_hasher.ComputeDigest(domain, value), out token);
    }

    private FrozenDictionary<SecretDigest, HoneyToken>? Index(TriggerDomain domain) =>
        HoneyTokenRules.IsDefined(domain) ? _byDomain[(int)domain] : null;
}
