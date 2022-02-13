namespace TinyCart.Eric;

public class CredentialsException : Exception
{
    public enum Credential
    {
        Unknown,
        Username,
        Password,
        MFAToken,
        PublicKey,
        PrivateKey,
    }

    public CredentialsException(Credential type, string message) : base(message)
        => InvalidCredentialType = type;
    public CredentialsException(Credential type, string message, Exception inner) : base(message, inner)
        => InvalidCredentialType = type;

    public Credential InvalidCredentialType { get; init; }
}

public class JoinPolicyException : Exception
{
    public JoinPolicyException(string message) : base(message) {}
    public JoinPolicyException(string message, Exception inner) : base(message, inner) {}
}

