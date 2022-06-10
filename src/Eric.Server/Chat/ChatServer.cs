namespace TinyCart.Eric.Server;

using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

using TinyCart.Eric.Messages.V0;
using TinyCart.Eric.Messages.V0.Home;

public class ChatServer : IServerWSEndpoint
{
    public enum JoinPolicy
    {
        Disabled,
        JoinTokenOnly,
        Enabled
    }


    public ChatServer(CoreServices cs)
    {
        Services = cs;
        m_logger = Services.Logging.GetLogger<ChatServer>();

        // TODO: persist keys and identity
        Keys = new RSAKeys(RSA.Create());
        Identity = new ServerIdentity()
        {
            PublicKey = PublicKey.FromRSAKeys(Keys),
            Name = "Test Server",
            Description = "A server for testing",
            URL = "wss://tinycart.local"
        };
    }

#region IServerWSEndpoint
    public ReadOnlyCollection<string> EndpointWSAddresses { get; } = new(new[] { "/ric0_chat" });
    public string EndpointName { get; } = "Chat Server";
    public string EndpointDescription { get; } = "Chat server functionality";

    public Task<WSConnection> WebSocketConnected(WebSocket socket, HttpContext context)
    {
        var ep = new IPEndPoint(context.Connection.RemoteIpAddress!, context.Connection.RemotePort);
        m_logger.Info("WebSocket connected from {0} using protocol {1}", ep.ToString(), socket.SubProtocol ?? "(none)");
        var conn = WSProtocol.CreateConnection(socket, ep.ToString(), Services.Logging.GetLogger<WSConnection>(), false);
        var comm = new JSONCommunicator((IJSONConnection)conn, Services.Logging.GetLogger<JSONCommunicator>());
        var client = new ChatServerClient(this, comm);
        m_clients[conn] = client;
        return Task.FromResult<WSConnection>(conn);
    }

    public Task WebSocketDisconnected(WSConnection conn, HttpContext context)
    {
        m_logger.Info("WebSocket disconnected from {0}", conn.RemoteAddress);
        if (m_clients.TryRemove(conn, out var client))
        {
            // TODO: client.Disconnect() ?
        }
        return Task.CompletedTask;
    }
#endregion


    // TODO: persist these...
    // TODO: dispose
    public RSAKeys Keys { get; init; }

    public JoinPolicy ConnectPolicy { get; set; } = JoinPolicy.Enabled;

    public ServerIdentity Identity { get; init; }

    public CoreServices Services { get; init; }
    private Logger m_logger;
    private ConcurrentDictionary<WSConnection, ChatServerClient> m_clients = new();

    private List<string> m_connectTokens = new();
    private object m_connectLock = new();
}