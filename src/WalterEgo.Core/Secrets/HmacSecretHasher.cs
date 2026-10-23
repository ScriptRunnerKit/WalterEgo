using System.Buffers;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace WalterEgo;

internal sealed class HmacSecretHasher : ISecretHasher
{
    internal const int MinimumKeyLength = 32;

    private const int SubkeyLength = 32;
    private const int StackBufferLength = 1024;

    // Indexed by the numeric value of TriggerDomain; index 0 stays empty.
    private readonly byte[][] _subkeys = new byte[(int)TriggerDomain.RequestPath + 1][];

    public HmacSecretHasher(ReadOnlySpan<byte> key)
    {
        if (key.Length < MinimumKeyLength)
        {
            throw new ArgumentException("The HMAC key must have at least 32 bytes.", nameof(key));
        }

        // Only the derived subkeys are kept, so the key itself never stays in memory owned by the hasher.
        ReadOnlySpan<TriggerDomain> domains = [TriggerDomain.ApiKey, TriggerDomain.Username, TriggerDomain.SessionToken, TriggerDomain.RequestPath];
        foreach (var domain in domains)
        {
            var subkey = new byte[SubkeyLength];
            HKDF.DeriveKey(HashAlgorithmName.SHA256, key, subkey, salt: [], info: Info(domain));
            _subkeys[(int)domain] = subkey;
        }
    }

    public SecretDigest ComputeDigest(TriggerDomain domain, ReadOnlySpan<char> value)
    {
        if (!HoneyTokenRules.IsDefined(domain))
        {
            throw new ArgumentOutOfRangeException(nameof(domain), domain, "The trigger domain is not defined.");
        }

        if (value.IsEmpty)
        {
            throw new ArgumentException("The value must not be empty.", nameof(value));
        }

        var byteCount = Encoding.UTF8.GetByteCount(value);
        byte[]? rented = null;
        Span<byte> utf8 = byteCount <= StackBufferLength
            ? stackalloc byte[StackBufferLength]
            : rented = ArrayPool<byte>.Shared.Rent(byteCount);
        utf8 = utf8[..byteCount];

        try
        {
            Encoding.UTF8.GetBytes(value, utf8);
            Span<byte> digest = stackalloc byte[SecretDigest.Length];
            HMACSHA256.HashData(_subkeys[(int)domain], utf8, digest);
            return SecretDigest.FromBytes(digest);
        }
        finally
        {
            // Sensors hash every value they see, so the buffer may hold the real secret of a legitimate user.
            CryptographicOperations.ZeroMemory(utf8);
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    // These labels are part of every digest: changing one invalidates all digests already planted in its domain.
    private static ReadOnlySpan<byte> Info(TriggerDomain domain) => domain switch
    {
        TriggerDomain.ApiKey => "walterego/v1/api-key"u8,
        TriggerDomain.Username => "walterego/v1/username"u8,
        TriggerDomain.SessionToken => "walterego/v1/session-token"u8,
        TriggerDomain.RequestPath => "walterego/v1/request-path"u8,
        _ => throw new UnreachableException(),
    };
}
