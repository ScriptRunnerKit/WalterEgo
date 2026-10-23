namespace WalterEgo.Core.Tests;

// Fictitious keys, never used outside tests.
internal static class TestKeys
{
    public static byte[] Sequential { get; } = [.. Enumerable.Range(0, 32).Select(i => (byte)i)];

    public static byte[] Reversed { get; } = [.. Enumerable.Range(0, 32).Select(i => (byte)(31 - i))];
}
