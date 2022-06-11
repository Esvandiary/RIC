namespace TinyCart.Eric.Client;

using TinyCart.Eric.Messages.V0;
using TinyCart.Eric.Messages.V0.Chat;

public class ChatClient : ClientBase
{
    private ChatClient(AppInfo info, JSONCommunicator comm, Logger logger)
        : base(info, comm, logger)
    {
    }

    public static Task<ChatClient> ConnectAsync(CoreServices services, string uri) => ConnectAsync(services, new Uri(uri));
    public static Task<ChatClient> ConnectAsync(CoreServices services, string uri, CancellationToken token) => ConnectAsync(services, new Uri(uri), token);
    public static Task<ChatClient> ConnectAsync(CoreServices services, Uri uri) => ConnectAsync(services, uri, CancellationToken.None);
    public static async Task<ChatClient> ConnectAsync(CoreServices services, Uri uri, CancellationToken token)
    {
        var logger = services.Logging.GetLogger("ChatClient");
        var chatUri = new Uri(uri, "ric0_chat");
        var comm = await ClientBase.ConnectAsync(chatUri, logger, token);
        return new ChatClient(services.ClientInfo, comm, logger);
    }

    public bool IsChatConnected { get => IsConnected && m_homeClient != null; }
    protected void EnsureChatConnected()
    {
        EnsureConnected();
        if (!IsChatConnected)
            throw new InvalidOperationException("cannot perform this action unless connect process has completed");
    }

    public async Task ConnectChat(HomeClient homeClient)
    {
        EnsureServerIdentityVerified();
        homeClient.EnsureLoggedIn();
        // make request to home server to sign chat server's key in order to verify our identity
        m_logger.Debug("signing chat server key with our privkey");
        byte[] signedServerKey = await homeClient.Sign(m_serverKeys!.PublicKey);
        m_logger.Debug("signed chat server key");

        var connreq = new ConnectRequest
        {
            ClientApp = m_appInfo,
            User = homeClient.UserIdentity,
            Challenge = signedServerKey.ToBase64(),
        };
        var connresult = await m_comm.SendRequestAndWaitAsync("connect", JObject.FromObject(connreq));
        m_logger.Debug("got chat connect response, status: {0}", connresult.Status);
        if (connresult.Status != "success")
        {
            m_logger.Error("connect request failed: {0}", connresult.Status);
            throw new InvalidOperationException("chat connect attempt failed"); // TODO
        }
        var connresp = connresult.Data.ToObject<ConnectSuccessResponse>();
        if (connresp == null)
            throw new InvalidDataException("failed to deserialize connect success response");
        m_logger.Info("connected to chat server {0}", connresp!.ServerIdentity.Name);

        m_homeClient = homeClient;
        m_serverIdentity = connresp!.ServerIdentity;
        m_serverAppInfo = connresp!.ServerApp;
    }

    private HomeClient? m_homeClient;
    private ServerIdentity? m_serverIdentity;
    private AppInfo? m_serverAppInfo;
}