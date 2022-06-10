namespace TinyCart.Eric;

using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using TinyCart.Eric.Extensions;


public class StoredPassword
{
    public enum HashMode
    {
        PBKDF2_HMAC_SHA256_10k
    }

    public StoredPassword(HashMode mode, byte[] hash, byte[] salt)
    {
        Mode = mode;
        Hash = hash;
        Salt = salt;
    }

    public bool Check(string password)
        => Hash.SequenceEqual(GenerateHash(Mode, password, Salt));


    public override bool Equals(object? obj)
    {
        if (obj is StoredPassword other)
        {
            return
                other.Mode == Mode
                && Hash.SequenceEqual(other.Hash)
                && Salt.SequenceEqual(other.Salt);
        }
        return base.Equals(obj);
    }

    public override int GetHashCode()
        => Mode.GetHashCode() ^ Hash.GetSequenceHashCode() ^ Salt.GetSequenceHashCode();

    public HashMode Mode { get; init; }
    public byte[] Hash { get; init; }
    public byte[] Salt { get; init; }


    private static byte[] GenerateHash(HashMode mode, string password, byte[] salt)
        => mode switch
        {
            HashMode.PBKDF2_HMAC_SHA256_10k
                => KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA256, 10000, HashLengths[mode]),
            _ => throw new ArgumentException("failed to generate stored password: unknown hash mode")
        };

    public static StoredPassword Generate(HashMode mode, string password)
    {
        byte[] salt = new byte[HashLengths[mode]];
        Random.Shared.NextBytes(salt);
        return Generate(mode, password, salt);
    }

    public static StoredPassword Generate(HashMode mode, string password, byte[] salt)
        => new StoredPassword(mode, GenerateHash(mode, password, salt), salt);

    private static ReadOnlyDictionary<HashMode, int> HashLengths = new(new Dictionary<HashMode, int> {
        {HashMode.PBKDF2_HMAC_SHA256_10k, 32}
    });
}