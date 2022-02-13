namespace TinyCart.Eric.Server;

using System.Security.Cryptography;

public class HomeServerUser
{
    internal static HomeServerUser CreateNew(string username, string password)
    {
        var pw = StoredPassword.Generate(StoredPassword.HashMode.PBKDF2_HMAC_SHA256_10k, password);
        RSA key = RSA.Create();
        return new HomeServerUser(username, pw, new RSAKeys(key));
    }

    internal HomeServerUser(string username, StoredPassword password, RSAKeys keys)
    {
        Username = username;
        Password = password;
        Keys = keys;
    }

    public string Username { get; init; }
    public StoredPassword Password { get; init; }
    public RSAKeys Keys { get; init; }
}