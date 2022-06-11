namespace TinyCart.Eric.Client;

using TinyCart.Eric.Messages.V0;
using TinyCart.Eric.Messages.V0.Home;

public class HomeClient : ClientBase
{
    private HomeClient(AppInfo info, JSONCommunicator comm, Logger logger)
        : base(info, comm, logger)
    {
    }

    public static Task<HomeClient> ConnectAsync(CoreServices services, string uri) => ConnectAsync(services, new Uri(uri));
    public static Task<HomeClient> ConnectAsync(CoreServices services, string uri, CancellationToken token) => ConnectAsync(services, new Uri(uri), token);
    public static Task<HomeClient> ConnectAsync(CoreServices services, Uri uri) => ConnectAsync(services, uri, CancellationToken.None);
    public static async Task<HomeClient> ConnectAsync(CoreServices services, Uri uri, CancellationToken token)
    {
        var logger = services.Logging.GetLogger("HomeClient");
        var homeUri = new Uri(uri, "ric0_home");
        var comm = await ClientBase.ConnectAsync(homeUri, logger, token);
        return new HomeClient(services.ClientInfo, comm, logger);
    }

    internal void EnsureLoggedIn()
    {
        EnsureConnected();
        if (m_userIdentity == null)
            throw new InvalidOperationException("cannot perform this action without having completed login");
    }

    public UserIdentity UserIdentity { get { EnsureLoggedIn(); return m_userIdentity!; } }
    public ServerIdentity ServerIdentity { get { EnsureLoggedIn(); return m_serverIdentity!; } }
    public AppInfo ServerAppInfo { get { EnsureLoggedIn(); return m_serverAppInfo!; } }

    public async Task Register(string username, string password)
    {
        EnsureServerIdentityVerified();
        m_logger.Info("sending register request for user {0}", username);
        var rreq = new RegisterRequest {
            Username = username,
            Password = Password.Generate(password, "rsa-base64", m_serverKeys!)};
        var rresult = await m_comm.SendRequestAndWaitAsync("register", JObject.FromObject(rreq));
        m_logger.Debug("got register response, status: {0}", rresult.Status);
        if (rresult.Status != "success")
        {
            m_logger.Error("register request failed: {0}", rresult.Status);
            throw new InvalidOperationException("register request failed"); // TODO
        }
        m_logger.Info("completed registration for user {0}", username);
    }

    public async Task Login(string username, string password, string? mfaToken = null)
    {
        EnsureServerIdentityVerified();
        m_logger.Info("sending login request for user {0}", username);
        var lreq = new LoginRequest {
            Username = username,
            Password = Password.Generate(password, "rsa-base64", m_serverKeys!),
            MFAToken = mfaToken,
            ClientApp = m_appInfo,
        };
        var lresult =  await m_comm.SendRequestAndWaitAsync("login", JObject.FromObject(lreq));
        m_logger.Debug("got login response, status: {0}", lresult.Status);
        if (lresult.Status != "success")
        {
            m_logger.Error("login request failed: {0}", lresult.Status);
            throw new InvalidOperationException("login request failed"); // TODO
        }
        var lresp = lresult.Data.ToObject<LoginSuccessResponse>();
        if (lresp == null)
            throw new InvalidDataException("invalid response from login request: cannot deserialize response");
        m_userIdentity = lresp.UserIdentity;
        m_serverIdentity = lresp.ServerIdentity;
        m_serverAppInfo = lresp.ServerApp;
        m_clientToken = lresp.ClientToken;
        m_logger.Info("completed login for user {0}", username);
    }

    public async Task<byte[]> Sign(byte[] data) => (await Sign(data.Yield())).Single();
    public async Task<List<byte[]>> Sign(IEnumerable<byte[]> data)
    {
        m_logger.Debug("sending sign request for {0} message{1}", data.Count(), data.Count() != 1 ? "s" : "");
        var sreq = new SignRequest { Messages = data.Select(t => t.ToBase64()).ToList() };
        var sresult = await m_comm.SendRequestAndWaitAsync("sign", JObject.FromObject(sreq));
        m_logger.Debug("got sign response, status: {0}", sresult.Status);
        if (sresult.Status != "success")
        {
            m_logger.Error("sign request failed: {0}", sresult.Status);
            throw new InvalidOperationException("sign request failed");
        }
        var sresp = sresult.Data.ToObject<SignSuccessResponse>();
        if (sresp == null)
            throw new InvalidDataException("failed to deserialize sign success response");
        return sresp.SignedHashes.Select(t => Convert.FromBase64String(t)).ToList();
    }

    public async Task<byte[]> Decrypt(byte[] data) => (await Decrypt(data.Yield())).Single();
    public async Task<List<byte[]>> Decrypt(IEnumerable<byte[]> data)
    {
        m_logger.Debug("sending decrypt request for {0} message{1}", data.Count(), data.Count() != 1 ? "s" : "");
        var sreq = new DecryptRequest { EncryptedMessages = data.Select(t => t.ToBase64()).ToList() };
        var sresult = await m_comm.SendRequestAndWaitAsync("decrypt", JObject.FromObject(sreq));
        m_logger.Debug("got decrypt response, status: {0}", sresult.Status);
        if (sresult.Status != "success")
        {
            m_logger.Error("decrypt request failed: {0}", sresult.Status);
            throw new InvalidOperationException("decrypt request failed");
        }
        var sresp = sresult.Data.ToObject<DecryptSuccessResponse>();
        if (sresp == null)
            throw new InvalidDataException("failed to deserialize decrypt success response");
        return sresp.DecryptedMessages.Select(t => Convert.FromBase64String(t)).ToList();
    }

    private UserIdentity? m_userIdentity;
    private ServerIdentity? m_serverIdentity;
    private AppInfo? m_serverAppInfo;
    private string? m_clientToken;
}