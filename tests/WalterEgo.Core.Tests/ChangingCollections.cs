using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace WalterEgo.Core.Tests;

// Collections that return different content after the first enumeration, to prove that inputs are read once.
internal sealed class ChangingList<T>(IReadOnlyList<T> first, IReadOnlyList<T> later) : IReadOnlyList<T>
{
    private int _reads;

    private IReadOnlyList<T> Current => _reads == 0 ? first : later;

    public int Count => Current.Count;

    public T this[int index] => Current[index];

    public IEnumerator<T> GetEnumerator() => (_reads++ == 0 ? first : later).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

internal sealed class ChangingDictionary(IReadOnlyDictionary<string, string> first, IReadOnlyDictionary<string, string> later)
    : IReadOnlyDictionary<string, string>
{
    private int _reads;

    private IReadOnlyDictionary<string, string> Current => _reads == 0 ? first : later;

    public int Count => Current.Count;

    public IEnumerable<string> Keys => Current.Keys;

    public IEnumerable<string> Values => Current.Values;

    public string this[string key] => Current[key];

    public bool ContainsKey(string key) => Current.ContainsKey(key);

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out string value) => Current.TryGetValue(key, out value);

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => (_reads++ == 0 ? first : later).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
