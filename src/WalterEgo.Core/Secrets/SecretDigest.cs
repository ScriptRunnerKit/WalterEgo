using System.Buffers;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace WalterEgo;

/// <summary>
/// A keyed digest (HMAC-SHA256) of a decoy value. The token registry stores digests instead of decoy values, so
/// reading the configuration does not reveal which values are decoys.
/// </summary>
/// <remarks>
/// The canonical text form is the 32 bytes in base64url without padding: exactly 43 characters. Comparing two
/// digests takes the same time whatever their bytes are.
/// </remarks>
public readonly struct SecretDigest : IEquatable<SecretDigest>
{
    /// <summary>The number of bytes in a digest.</summary>
    public const int Length = 32;

    private const int TextLength = 43;

    private static readonly SearchValues<char> Base64UrlAlphabet =
        SearchValues.Create("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_");

    private readonly DigestBytes _bytes;

    private SecretDigest(ReadOnlySpan<byte> bytes) => bytes.CopyTo(_bytes);

    internal bool IsDefault => AsSpan().IndexOfAnyExcept((byte)0) < 0;

    /// <summary>
    /// Reads a digest from its canonical text form.
    /// </summary>
    /// <param name="text">43 base64url characters, without padding or white space.</param>
    /// <returns>The digest.</returns>
    /// <exception cref="FormatException"><paramref name="text"/> is not the canonical text form of a digest.</exception>
    public static SecretDigest Parse(ReadOnlySpan<char> text) =>
        TryParse(text, out var digest)
            ? digest
            : throw new FormatException("A digest must be written as 43 base64url characters, without padding or white space.");

    /// <summary>
    /// Tries to read a digest from its canonical text form.
    /// </summary>
    /// <param name="text">43 base64url characters, without padding or white space.</param>
    /// <param name="digest">The digest, or the default value when <paramref name="text"/> is not canonical.</param>
    /// <returns><see langword="true"/> when <paramref name="text"/> is the canonical text form of a digest.</returns>
    public static bool TryParse(ReadOnlySpan<char> text, out SecretDigest digest)
    {
        digest = default;
        if (text.Length != TextLength || text.ContainsAnyExcept(Base64UrlAlphabet))
        {
            return false;
        }

        // The OperationStatus overload is used because TryDecodeFromChars throws on some malformed inputs.
        Span<byte> bytes = stackalloc byte[Length];
        if (Base64Url.DecodeFromChars(text, bytes, out _, out var bytesWritten) != OperationStatus.Done || bytesWritten != Length)
        {
            return false;
        }

        // Re-encoding rejects texts whose unused trailing bits are not zero, so each digest has a single text form.
        Span<char> canonical = stackalloc char[TextLength];
        if (!Base64Url.TryEncodeToChars(bytes, canonical, out var charsWritten) || !canonical[..charsWritten].SequenceEqual(text))
        {
            return false;
        }

        digest = new SecretDigest(bytes);
        return true;
    }

    internal static SecretDigest FromBytes(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != Length)
        {
            throw new ArgumentException("A digest has exactly 32 bytes.", nameof(bytes));
        }

        return new SecretDigest(bytes);
    }

    [UnscopedRef]
    internal ReadOnlySpan<byte> AsSpan() => _bytes;

    /// <summary>
    /// Compares two digests in constant time.
    /// </summary>
    /// <param name="other">The digest to compare with.</param>
    /// <returns><see langword="true"/> when both digests have the same bytes.</returns>
    public bool Equals(SecretDigest other) => CryptographicOperations.FixedTimeEquals(AsSpan(), other.AsSpan());

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is SecretDigest other && Equals(other);

    // HMAC output is uniform and an attacker cannot choose it without the key, so the first bytes are a good hash.
    /// <inheritdoc/>
    public override int GetHashCode() => BinaryPrimitives.ReadInt32LittleEndian(AsSpan());

    /// <summary>
    /// Returns the canonical text form of the digest.
    /// </summary>
    /// <returns>43 base64url characters, without padding.</returns>
    public override string ToString() => Base64Url.EncodeToString(AsSpan());

    /// <summary>Compares two digests in constant time.</summary>
    /// <param name="left">The first digest.</param>
    /// <param name="right">The second digest.</param>
    /// <returns><see langword="true"/> when both digests have the same bytes.</returns>
    public static bool operator ==(SecretDigest left, SecretDigest right) => left.Equals(right);

    /// <summary>Compares two digests in constant time.</summary>
    /// <param name="left">The first digest.</param>
    /// <param name="right">The second digest.</param>
    /// <returns><see langword="true"/> when the digests have different bytes.</returns>
    public static bool operator !=(SecretDigest left, SecretDigest right) => !left.Equals(right);

    [InlineArray(Length)]
    private struct DigestBytes
    {
        private byte _element;
    }
}
