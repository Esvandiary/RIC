namespace TinyCart.Eric.Server;

using System.Security.Cryptography;
using TinyCart.Eric.Messages.V0;
using TinyCart.Eric.Messages.V0.Chat;

public class ChatServerClient
{
    public ChatServerClient(ChatServer server, WSTextConnection conn)
    {
        m_server = server;
        m_conn = conn;
        m_logger = server.Services.Logging.GetLogger($"ChatServerClient[{m_conn.RemoteAddress}]");

        m_conn.SetRequestHandler("challenge", HandleChallengeRequest);
        m_conn.SetRequestHandler("connect", HandleConnectRequest);
        m_conn.SetRequestHandler("disconnect", HandleDisconnectRequest);
    }


    private Task<WSTextConnection.Response> HandleChallengeRequest(WSTextConnection conn, string request, JObject data)
    {
        try
        {
            var rdata = data.ToObject<ChallengeRequest>();
            byte[] challenge = Convert.FromBase64String(rdata!.Challenge);
            byte[] response = m_server.Keys.Sign(challenge);
            ChallengeSuccessResponse result = new() {
                PublicKey = PublicKey.FromRSAKeys(m_server.Keys),
                Response = Convert.ToBase64String(response) };
            return Task.FromResult<WSTextConnection.Response>(new() { Status = "success", Data = JObject.FromObject(result) });
        }
        catch (JsonException)
        {
            // bug?
            return Task.FromResult<WSTextConnection.Response>(new() { Status = "invalid_message", Data = new JObject() });
        }
        catch (Exception)
        {
            // TODO: better exceptions
            return Task.FromResult<WSTextConnection.Response>(new() { Status = "unknown_error", Data = new JObject() });
        }
    }

    private Task<WSTextConnection.Response> HandleConnectRequest(WSTextConnection conn, string request, JObject data)
    {
        if (m_user != null)
        {
            return Task.FromResult<WSTextConnection.Response>(new() { Status = "already_connected", Data = new JObject() });
        }

        try
        {
            var rdata = data.ToObject<ConnectRequest>()!;

            // TODO: check format
            var keys = RSAKeys.FromPublicKey(Convert.FromBase64String(rdata.User.PublicKey.KeyData));
            if (!keys.Verify(m_server.Keys.PublicKey, Convert.FromBase64String(rdata.Challenge)))
            {
                return Task.FromResult<WSTextConnection.Response>(new() { Status = "invalid_challenge", Data = new JObject() });
            }

            // TODO: m_server.ConnectUser(rdata.User);

            ConnectSuccessResponse response = new() {
                ServerApp = m_server.Services.ServerInfo,
                ServerIdentity = m_server.Identity,
            };

            m_user = rdata.User;

            return Task.FromResult<WSTextConnection.Response>(new() { Status = "success", Data = JObject.FromObject(response) });
        }
        catch (JsonException)
        {
            // bug?
            return Task.FromResult<WSTextConnection.Response>(new() { Status = "invalid_message", Data = new JObject() });
        }
        catch (CredentialsException ex)
        {
            string status = ex.InvalidCredentialType switch
            {
                CredentialsException.Credential.Username => "unrecognised_user",
                CredentialsException.Credential.PublicKey => "invalid_pubkey",
                _ => "unknown_error"
            };
            // TODO: write info struct
            return Task.FromResult<WSTextConnection.Response>(new() { Status = status, Data = new JObject() });
        }
        catch (JoinPolicyException)
        {
            string status = !String.IsNullOrEmpty(data.Value<string>("join_token")) ? "invalid_join_token" : "join_token_required";
            return Task.FromResult<WSTextConnection.Response>(new() { Status = status, Data = new JObject() });
        }
        catch (Exception)
        {
            return Task.FromResult<WSTextConnection.Response>(new() { Status = "unknown_error", Data = new JObject() });
        }
    }

    private Task<WSTextConnection.Response> HandleDisconnectRequest(WSTextConnection conn, string request, JObject data)
    {
        if (m_user == null)
        {
            return Task.FromResult<WSTextConnection.Response>(new() { Status = "not_connected", Data = new JObject() });
        }

        try
        {
            var rdata = data.ToObject<DisconnectRequest>();
            // TODO: m_server.DisconnectUser(m_user, rdata.Reason);
            m_user = null;

            return Task.FromResult<WSTextConnection.Response>(new() { Status = "success", Data = new JObject() });
        }
        catch (JsonException)
        {
            // bug?
            return Task.FromResult<WSTextConnection.Response>(new() { Status = "invalid_message", Data = new JObject() });
        }
        catch (Exception)
        {
            return Task.FromResult<WSTextConnection.Response>(new() { Status = "unknown_error", Data = new JObject() });
        }
    }

    private ChatServer m_server;
    private WSTextConnection m_conn;
    private UserIdentity? m_user;
    private Logger m_logger;
}