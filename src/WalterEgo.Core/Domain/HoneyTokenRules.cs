using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace WalterEgo;

internal static class HoneyTokenRules
{
    internal const int MaxIdLength = 64;
    internal const int MaxNameLength = 128;
    internal const int MaxPlacementLength = 512;
    internal const int MaxDescriptionLength = 1024;
    internal const int MaxTagCount = 16;
    internal const int MaxTagKeyLength = 32;
    internal const int MaxTagValueLength = 128;

    internal const string InvalidIdMessage =
        "The identifier must have 1 to 64 characters, start with a lowercase ASCII letter or digit and contain only lowercase ASCII letters, digits, '-', '.' and '_'.";

    internal static bool IsSlug([NotNullWhen(true)] string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length > maxLength || !IsLowercaseAlphanumeric(value[0]))
        {
            return false;
        }

        foreach (var c in value.AsSpan(1))
        {
            if (!IsLowercaseAlphanumeric(c) && c is not ('-' or '.' or '_'))
            {
                return false;
            }
        }

        return true;
    }

    internal static bool IsDefined(HoneyTokenKind kind) => kind is >= HoneyTokenKind.Endpoint and <= HoneyTokenKind.File;

    internal static bool IsDefined(DecoyClass decoyClass) => decoyClass is DecoyClass.Generic or DecoyClass.Unique;

    internal static bool IsDefined(Severity severity) => severity is >= Severity.Low and <= Severity.Critical;

    internal static bool IsDefined(TriggerDomain domain) => domain is >= TriggerDomain.ApiKey and <= TriggerDomain.RequestPath;

    internal static string? CheckPlacement(string? placement) =>
        CheckText(placement, "The placement", MaxPlacementLength, allowBlank: false);

    internal static string? CheckName(string? name) =>
        name is null ? null : CheckText(name, "The name", MaxNameLength, allowBlank: false);

    internal static string? CheckDescription(string? description) =>
        description is null ? null : CheckText(description, "The description", MaxDescriptionLength, allowBlank: true);

    internal static string? CheckTags(IReadOnlyDictionary<string, string>? tags)
    {
        if (tags is null)
        {
            return null;
        }

        if (tags.Count > MaxTagCount)
        {
            return "An alter ego can have at most 16 tags.";
        }

        foreach (var (key, value) in tags)
        {
            if (!IsSlug(key, MaxTagKeyLength))
            {
                return "Tag keys must have 1 to 32 characters, start with a lowercase ASCII letter or digit and contain only lowercase ASCII letters, digits, '-', '.' and '_'.";
            }

            if (CheckText(value, "Each tag value", MaxTagValueLength, allowBlank: false) is { } error)
            {
                return error;
            }
        }

        return null;
    }

    private static string? CheckText(string? text, string subject, int maxLength, bool allowBlank)
    {
        if (text is null || (!allowBlank && string.IsNullOrWhiteSpace(text)))
        {
            return $"{subject} must not be empty or white space.";
        }

        if (text.Length > maxLength)
        {
            return $"{subject} must have at most {maxLength} characters.";
        }

        // Line and paragraph separators are rejected with the control characters because they also break
        // log lines and message layouts in alert channels.
        foreach (var c in text)
        {
            if (char.GetUnicodeCategory(c) is UnicodeCategory.Control or UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator)
            {
                return $"{subject} must not contain control characters or line separators.";
            }
        }

        return null;
    }

    private static bool IsLowercaseAlphanumeric(char c) => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c);
}
