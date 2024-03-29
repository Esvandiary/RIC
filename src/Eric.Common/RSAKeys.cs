namespace TinyCart.Eric;

using System.Security.Cryptography;

public class RSAKeys : IDisposable
{
    public static RSAKeys FromPublicKey(byte[] pubkey)
    {
        var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(pubkey, out var _);
        return new RSAKeys(rsa);
    }

    public static RSAKeys FromMessage(Messages.V0.PublicKey message)
    {
        if (message.KeyFormat != RSAConstants.FormatName)
            throw new CredentialsException(CredentialsException.Credential.PublicKey, $"invalid public key format {message.KeyFormat}");
        return FromPublicKey(Convert.FromBase64String(message.KeyData));
    }

    public RSAKeys(RSA impl)
        => m_rsa = impl;

    public byte[] Decrypt(byte[] data) => m_rsa.Decrypt(data, RSAConstants.EncryptionPadding);
    public byte[] Encrypt(byte[] data) => m_rsa.Encrypt(data, RSAConstants.EncryptionPadding);
    public byte[] Sign(byte[] data)
        => m_rsa.SignData(data, RSAConstants.HashAlgorithm, RSAConstants.SignaturePadding);
    public bool Verify(byte[] original, byte[] signature)
        => m_rsa.VerifyData(original, signature, RSAConstants.HashAlgorithm, RSAConstants.SignaturePadding);

    public byte[] PublicKey { get => m_rsa.ExportRSAPublicKey(); }

    public string FormatName { get => RSAConstants.FormatName; }

    public void Dispose() => Dispose(true);
    public void Dispose(bool disposing)
    {
        if (disposing)
        {
            m_rsa.Dispose();
        }
    }

    private RSA m_rsa;
}

public static class RSAConstants
{
    public static HashAlgorithmName HashAlgorithm { get; } = HashAlgorithmName.SHA256;
    public static RSAEncryptionPadding EncryptionPadding { get; } = RSAEncryptionPadding.OaepSHA256;
    public static RSASignaturePadding SignaturePadding { get; } = RSASignaturePadding.Pkcs1;

    public static string FormatName
    {
        get => $"rsa-{HashAlgorithm.Name!.ToLower()}-{EncryptionPadding.ToString().ToLower()}-{SignaturePadding.ToString().ToLower()}";
    }
}