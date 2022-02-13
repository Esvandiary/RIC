namespace TinyCart.Eric.Server;

using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

using TinyCart.Eric.Messages.V0;
using TinyCart.Eric.Messages.V0.Home;

public class HomeServer : IServerWSEndpoint
{
    public enum JoinPolicy
    {
        Disabled,
        JoinTokenOnly,
        Enabled
    }


    public HomeServer(CoreServices cs)
    {
        Services = cs;
        m_logger = Services.Logging.GetLogger<HomeServer>();

        Keys = new RSAKeys(RSA.Create());
        Identity = new ServerIdentity()
        {
            PublicKey = PublicKey.FromRSAKeys(Keys),
            Name = "Test Server",
            Description = "A server for testing",
            URL = "wss://tinycart.local"
        };
        PublishedURL = Identity.URL;
    }

#region IServerWSEndpoint
    public ReadOnlyCollection<string> EndpointWSAddresses { get; } = new(new[] { "/ric0_home" });
    public string EndpointName { get; } = "Home Server";
    public string EndpointDescription { get; } = "Home server functionality";

    public Task<WSConnection> WebSocketConnected(WebSocket socket, HttpContext context)
    {
        var ep = new IPEndPoint(context.Connection.RemoteIpAddress!, context.Connection.RemotePort);
        m_logger.Info($"WebSocket connected from {ep.ToString()}");
        var conn = new WSTextConnection(socket, ep.ToString(), Services.Logging.GetLogger<WSTextConnection>(), false);
        var client = new HomeServerClient(this, conn);
        m_clients[conn] = client;
        return Task.FromResult<WSConnection>(conn);
    }

    public Task WebSocketDisconnected(WSConnection conn, HttpContext context)
    {
        m_logger.Info($"WebSocket disconnected from {conn.RemoteAddress}");
        if (m_clients.TryRemove(conn, out var client))
        {
            // TODO: client.Disconnect() ?
        }
        return Task.CompletedTask;
    }
#endregion


    public HomeServerUser RegisterUser(string username, Password password, string? joinToken)
    {
        if (RegistrationPolicy == JoinPolicy.Disabled)
            throw new JoinPolicyException("attempted to register with registrations disabled");

        lock (m_registerLock)
        {
            if (RegistrationPolicy == JoinPolicy.JoinTokenOnly)
            {
                if (joinToken == null)
                    throw new JoinPolicyException("attempted to register with join token required and no token provided");
                if (!m_registerTokens.Contains(joinToken!))
                    throw new JoinPolicyException("attempted to register with join token required and unrecognised token provided");
            }

            if (m_users.ContainsKey(username))
                throw new CredentialsException(CredentialsException.Credential.Username, "username in use");
            
            if (password.Format != "plaintext")
                throw new CredentialsException(CredentialsException.Credential.Password, "password must be in plaintext");

            var user = HomeServerUser.CreateNew(username, password.Data);
            m_users[username] = user;

            if (RegistrationPolicy == JoinPolicy.JoinTokenOnly)
            {
                // TODO: better system
                m_registerTokens.Remove(joinToken!);
            }

            return user;
        }
    }

    public HomeServerUser LoginUser(string username, Password password, string? clientToken, string? mfaToken, string? joinToken)
    {
        if (LoginPolicy == JoinPolicy.Disabled)
            throw new JoinPolicyException("attempted to log in with logins disabled");

        if (password.Format != "plaintext")
            throw new CredentialsException(CredentialsException.Credential.Password, "password must be in plaintext");

        if (!m_users.TryGetValue(username, out var user))
            throw new CredentialsException(CredentialsException.Credential.Username, "unknown username");

        if (!user.Password.Check(password.Data))
            throw new CredentialsException(CredentialsException.Credential.Password, "incorrect password");

        // TODO: MFA

        lock (m_loginLock)
        {
            if (LoginPolicy == JoinPolicy.JoinTokenOnly)
            {
                if (joinToken == null)
                    throw new JoinPolicyException("attempted login with join token required and no token provided");
                if (!m_loginTokens.Contains(joinToken!))
                    throw new JoinPolicyException("attempted login with join token required and unrecognised token provided");

                if (LoginPolicy == JoinPolicy.JoinTokenOnly)
                {
                    // TODO: better system
                    m_loginTokens.Remove(joinToken!);
                }
            }

            return user;
        }
    }

    // TODO: persist these...
    public RSAKeys Keys { get; init; }

    public JoinPolicy RegistrationPolicy { get; set; } = JoinPolicy.Enabled;
    public JoinPolicy LoginPolicy { get; set; } = JoinPolicy.Enabled;

    public ServerIdentity Identity { get; init; }
    public string? PublishedURL { get; set; }

    public CoreServices Services { get; init; }
    private Logger m_logger;
    private ConcurrentDictionary<WSConnection, HomeServerClient> m_clients = new();
    private ConcurrentDictionary<string, HomeServerUser> m_users = new();

    private List<string> m_registerTokens = new();
    private List<string> m_loginTokens = new();
    private object m_registerLock = new();
    private object m_loginLock = new();
}