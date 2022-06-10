namespace TinyCart.Eric.Client;

using System.Net.WebSockets;
using System.Security.Cryptography;
using TinyCart.Eric.Messages.V0;
using TinyCart.Eric.Messages.V0.Chat;

public abstract class ClientBase : IDisposable
{
    protected ClientBase(AppInfo info, JSONCommunicator comm, Logger logger)
    {
        m_appInfo = info;
        m_comm = comm;
        m_logger = logger;
    }

    protected static async Task<JSONCommunicator> ConnectAsync(Uri uri, Logger logger, CancellationToken token)
    {
        var ws = new ClientWebSocket();
        foreach (var name in WSProtocol.Names)
            ws.Options.AddSubProtocol(name);
        await ws.ConnectAsync(uri, CancellationToken.None);
        var conn = WSProtocol.CreateConnection(ws, uri.AbsoluteUri, logger, true);
        var comm = new JSONCommunicator((IJSONConnection)conn, logger);
        _ = comm.Connection.ReadWhileOpenAsync();
        return comm;
    }

    public bool IsConnected { get => m_comm.Connection.IsOpen; }
    internal void EnsureConnected()
    {
        if (!IsConnected)
            throw new InvalidOperationException("cannot perform this action while disconnected");
    }

    public async Task CloseAsync(string message) => await m_comm.CloseAsync(message);
    public async Task CloseAsync(string message, CancellationToken token) => await m_comm.CloseAsync(message, token);

    public bool IsServerIdentityVerified { get => m_serverKeys != null; }
    internal void EnsureServerIdentityVerified()
    {
        if (!IsServerIdentityVerified)
            throw new InvalidOperationException("cannot perform this operation before server identity is verified");
    }

    public async Task VerifyServerIdentity()
    {
        if (IsServerIdentityVerified)
            return;

        byte[] randomData = new byte[64];
        Random.Shared.NextBytes(randomData);

        m_logger.Debug("sending challenge to server");
        var creq = new ChallengeRequest { Challenge = Convert.ToBase64String(randomData) };
        var cresult = await m_comm.SendRequestAndWaitAsync("challenge", JObject.FromObject(creq));
        m_logger.Debug("got challenge response, status: {0}", cresult.Status);
        if (cresult.Status != "success")
        {
            m_logger.Error("server challenge request failed (result: {0})", cresult.Status);
            throw new InvalidOperationException("server challenge request failed");
        }
        var cresp = cresult.Data.ToObject<ChallengeSuccessResponse>();

        var serverKey = RSAKeys.FromPublicKey(Convert.FromBase64String(cresp!.PublicKey.KeyData));
        if (!serverKey.Verify(randomData, Convert.FromBase64String(cresp.Response)))
        {
            m_logger.Error("server key verification failed");
            throw new InvalidDataException("server keys failed to verify");
        }
        m_serverKeys = serverKey;
        m_logger.Debug("server key verified");
    }

    private bool m_disposed = false;
    public void Dispose() => Dispose(true);
    protected void Dispose(bool disposing)
    {
        if (!m_disposed)
        {
            m_comm.Dispose();
            m_serverKeys?.Dispose();
            m_disposed = true;
        }
    }

    protected AppInfo m_appInfo;
    protected JSONCommunicator m_comm;
    protected UserIdentity? m_user;
    protected Logger m_logger;

    protected RSAKeys? m_serverKeys;
}