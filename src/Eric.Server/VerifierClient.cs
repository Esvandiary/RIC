namespace TinyCart.Eric.Server;

using TinyCart.Eric.Messages.V0;

public class VerifierClient : ClientBase
{
    private VerifierClient(AppInfo info, JSONCommunicator comm, Logger logger)
        : base(info, comm, logger)
    {
    }

    public static Task<VerifierClient> ConnectAsync(CoreServices services, string uri, string path) => ConnectAsync(services, new Uri(uri), path);
    public static Task<VerifierClient> ConnectAsync(CoreServices services, string uri, string path, CancellationToken token) => ConnectAsync(services, new Uri(uri), path, token);
    public static Task<VerifierClient> ConnectAsync(CoreServices services, Uri uri, string path) => ConnectAsync(services, uri, path, CancellationToken.None);
    public static async Task<VerifierClient> ConnectAsync(CoreServices services, Uri uri, string path, CancellationToken token)
    {
        var info = new AppInfo {
            Name = "Eric Server Verifier Client",
            Description = "Client used to verify server identities",
            Version = services.ServerInfo.Version,
        };
        var logger = services.Logging.GetLogger("VerifierClient");
        var comm = await ClientBase.ConnectAsync(new Uri(uri, path), logger, token);
        return new VerifierClient(info, comm, logger);
    }

    public bool Verify(RSAKeys key)
    {
        EnsureServerIdentityVerified();
        return m_serverKeys!.PublicKey.SequenceEqual(key.PublicKey);
    }
}