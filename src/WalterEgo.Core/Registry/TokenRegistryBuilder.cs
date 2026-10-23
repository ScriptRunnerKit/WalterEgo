using System.Collections.Frozen;

namespace WalterEgo;

internal static class TokenRegistryBuilder
{
    public static TokenRegistryBuildResult Build(ISecretHasher hasher, IEnumerable<HoneyTokenRegistration> registrations)
    {
        List<RegistryError> errors = [];
        List<(HoneyToken Token, IReadOnlyList<HoneyTrigger> Triggers)> entries = [];
        HashSet<HoneyTokenId> ids = [];
        Dictionary<HoneyTrigger, HoneyTokenId> triggerOwners = [];
        var position = 0;

        foreach (var registration in registrations)
        {
            position++;
            if (!HoneyTokenId.TryCreate(registration.Id, out var id))
            {
                errors.Add(new(null, $"The alter ego at position {position} has an invalid identifier. {HoneyTokenRules.InvalidIdMessage}"));
                continue;
            }

            if (!ids.Add(id))
            {
                errors.Add(new(id.Value, "The identifier is used by more than one alter ego."));
                continue;
            }

            // Inputs are read once, so what is validated is exactly what gets indexed.
            HoneyTrigger[] triggers = [.. registration.Triggers ?? []];
            var tags = registration.Tags?.ToFrozenDictionary(StringComparer.Ordinal);

            var errorCount = errors.Count;
            CheckSharedTriggers(id, triggers, triggerOwners, errors);
            if (Validate(id, registration, triggers, tags, errors) is { } token && errors.Count == errorCount)
            {
                entries.Add((token, triggers));
            }
        }

        return errors.Count > 0
            ? new TokenRegistryBuildResult(null, errors)
            : new TokenRegistryBuildResult(new TokenRegistry(hasher, entries), []);
    }

    private static HoneyToken? Validate(
        HoneyTokenId id,
        HoneyTokenRegistration registration,
        HoneyTrigger[] triggers,
        IReadOnlyDictionary<string, string>? tags,
        List<RegistryError> errors)
    {
        var errorCount = errors.Count;
        void Fail(string message) => errors.Add(new(id.Value, message));

        var kind = registration.Kind;
        var kindIsDefined = HoneyTokenRules.IsDefined(kind);
        if (!kindIsDefined)
        {
            Fail("The kind is not defined.");
        }

        DecoyClass? decoyClass = registration.Class;
        if (decoyClass is { } givenClass && !HoneyTokenRules.IsDefined(givenClass))
        {
            Fail("The class is not defined.");
        }
        else if (decoyClass is null && kindIsDefined)
        {
            if (kind is HoneyTokenKind.Endpoint or HoneyTokenKind.User)
            {
                Fail($"A {kind} alter ego needs an explicit class, because it can be either a generic or a unique decoy.");
            }
            else
            {
                decoyClass = DecoyClass.Unique;
            }
        }

        var severity = registration.Severity;
        if (severity is { } givenSeverity && !HoneyTokenRules.IsDefined(givenSeverity))
        {
            Fail("The severity is not defined.");
        }

        AddIfInvalid(HoneyTokenRules.CheckPlacement(registration.Placement));
        AddIfInvalid(HoneyTokenRules.CheckName(registration.Name));
        AddIfInvalid(HoneyTokenRules.CheckDescription(registration.Description));
        AddIfInvalid(HoneyTokenRules.CheckTags(tags));

        HashSet<HoneyTrigger> seen = [];
        foreach (var trigger in triggers)
        {
            if (!HoneyTokenRules.IsDefined(trigger.Domain))
            {
                Fail("A trigger has an undefined domain.");
            }
            else if (kindIsDefined && !AcceptsDomain(kind, trigger.Domain))
            {
                Fail($"A {kind} alter ego does not accept {trigger.Domain} triggers.");
            }

            if (trigger.Digest.IsDefault)
            {
                Fail("A trigger has an uninitialized digest.");
            }

            if (!seen.Add(trigger))
            {
                Fail("A trigger is repeated.");
            }
        }

        if (kindIsDefined && triggers.Length < MinimumTriggers(kind))
        {
            Fail($"A {kind} alter ego needs at least one trigger.");
        }

        if (errors.Count > errorCount || decoyClass is not { } resolvedClass)
        {
            return null;
        }

        return new HoneyToken(
            id,
            kind,
            resolvedClass,
            registration.Placement!,
            severity ?? (resolvedClass is DecoyClass.Generic ? Severity.Low : Severity.High),
            registration.Name,
            registration.Description,
            tags);

        void AddIfInvalid(string? error)
        {
            if (error is not null)
            {
                Fail(error);
            }
        }
    }

    private static void CheckSharedTriggers(
        HoneyTokenId id,
        HoneyTrigger[] triggers,
        Dictionary<HoneyTrigger, HoneyTokenId> triggerOwners,
        List<RegistryError> errors)
    {
        foreach (var trigger in triggers)
        {
            if (!HoneyTokenRules.IsDefined(trigger.Domain) || trigger.Digest.IsDefault)
            {
                continue;
            }

            if (!triggerOwners.TryAdd(trigger, id) && triggerOwners[trigger] != id)
            {
                errors.Add(new(id.Value, $"A {trigger.Domain} trigger is shared with the alter ego '{triggerOwners[trigger]}'."));
            }
        }
    }

    private static bool AcceptsDomain(HoneyTokenKind kind, TriggerDomain domain) => kind switch
    {
        HoneyTokenKind.Endpoint => domain is TriggerDomain.RequestPath,
        HoneyTokenKind.User => domain is TriggerDomain.Username,
        HoneyTokenKind.ApiKey => domain is TriggerDomain.ApiKey,
        HoneyTokenKind.SessionToken => domain is TriggerDomain.SessionToken,
        HoneyTokenKind.Config or HoneyTokenKind.File => true,
        _ => false,
    };

    private static int MinimumTriggers(HoneyTokenKind kind) => kind is HoneyTokenKind.Endpoint or HoneyTokenKind.Row ? 0 : 1;
}
