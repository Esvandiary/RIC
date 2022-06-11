namespace TinyCart.Eric.Server;

using System.Security.Cryptography;
using TinyCart.Eric.Messages.V0;
using TinyCart.Eric.Messages.V0.Chat;

public class ChatServerClient
{
    public ChatServerClient(ChatServer server, JSONCommunicator comm)
    {
        m_server = server;
        m_comm = comm;
        m_logger = server.Services.Logging.GetLogger($"ChatServerClient[{m_comm.Connection.RemoteAddress}]");

        m_comm.SetRequestHandler("challenge", HandleChallengeRequest);
        m_comm.SetRequestHandler("connect", HandleConnectRequest);
        m_comm.SetRequestHandler("disconnect", HandleDisconnectRequest);
    }


    private Task<JSONCommunicator.Response> HandleChallengeRequest(JSONCommunicator comm, string request, JObject data)
    {
        try
        {
            var rdata = data.ToObject<ChallengeRequest>();
            byte[] challenge = Convert.FromBase64String(rdata!.Challenge);
            byte[] response = m_server.Keys.Sign(challenge);
            ChallengeSuccessResponse result = new() {
                PublicKey = PublicKey.FromRSAKeys(m_server.Keys),
                Response = response.ToBase64() };
            return Task.FromResult<JSONCommunicator.Response>(new() { Status = "success", Data = JObject.FromObject(result) });
        }
        catch (JsonException)
        {
            // bug?
            return Task.FromResult<JSONCommunicator.Response>(new() { Status = "invalid_message", Data = new JObject() });
        }
        catch (Exception)
        {
            // TODO: better exceptions
            return Task.FromResult<JSONCommunicator.Response>(new() { Status = "unknown_error", Data = new JObject() });
        }
    }

    private async Task<JSONCommunicator.Response> HandleConnectRequest(JSONCommunicator comm, string request, JObject data)
    {
        if (m_user != null)
        {
            return new() { Status = "already_connected", Data = new JObject() };
        }

        try
        {
            var rdata = data.ToObject<ConnectRequest>()!;

            var keys = RSAKeys.FromMessage(rdata.User.PublicKey);
            if (!keys.Verify(m_server.Keys.PublicKey, Convert.FromBase64String(rdata.Challenge)))
            {
                return new() { Status = "invalid_challenge", Data = new JObject() };
            }

            var homeKeys = RSAKeys.FromMessage(rdata.User.HomeServerPublicKey);
            // verify home server URL validity if provided
            if (!String.IsNullOrEmpty(rdata.User.HomeServerURL))
            {
                m_logger.Debug("verifying home server {0}", rdata.User.HomeServerURL);
                using (var verifier = await VerifierClient.ConnectAsync(m_server.Services, rdata.User.HomeServerURL, "ric0_home"))
                {
                    await verifier.VerifyServerIdentity();
                    bool keysOK = verifier.Verify(homeKeys);
                    await verifier.CloseAsync("closing normally");
                    if (!keysOK)
                    {
                        m_logger.Error("connected to home server {0} but keys do not match", rdata.User.HomeServerURL);
                        return new() { Status = "invalid_home_server", Data = new JObject() };
                    }
                }
                m_logger.Debug("verified home server {0}", rdata.User.HomeServerURL);
            }
            // verify home server agrees with username
            bool homeUserOK = homeKeys.Verify(
                rdata.User.HomeServerUser.ToUTF8Bytes(),
                Convert.FromBase64String(rdata!.User.HomeServerUserSignature));
            if (!homeUserOK)
            {
                m_logger.Error("home server username signature does not verify");
                return new() { Status = "invalid_home_server", Data = new JObject() };
            }
            m_logger.Debug("verified home server username {0}", rdata.User.HomeServerUser);

            // TODO: m_server.ConnectUser(rdata.User);

            ConnectSuccessResponse response = new() {
                ServerApp = m_server.Services.ServerInfo,
                ServerIdentity = m_server.Identity,
            };

            m_user = rdata.User;

            return new() { Status = "success", Data = JObject.FromObject(response) };
        }
        catch (JsonException)
        {
            // bug?
            return new() { Status = "invalid_message", Data = new JObject() };
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
            return new() { Status = status, Data = new JObject() };
        }
        catch (JoinPolicyException)
        {
            string status = !String.IsNullOrEmpty(data.Value<string>("join_token")) ? "invalid_join_token" : "join_token_required";
            return new() { Status = status, Data = new JObject() };
        }
        catch (Exception)
        {
            return new() { Status = "unknown_error", Data = new JObject() };
        }
    }

    private Task<JSONCommunicator.Response> HandleDisconnectRequest(JSONCommunicator comm, string request, JObject data)
    {
        if (m_user == null)
        {
            return Task.FromResult<JSONCommunicator.Response>(new() { Status = "not_connected", Data = new JObject() });
        }

        try
        {
            var rdata = data.ToObject<DisconnectRequest>();
            // TODO: m_server.DisconnectUser(m_user, rdata.Reason);
            m_user = null;

            return Task.FromResult<JSONCommunicator.Response>(new() { Status = "success", Data = new JObject() });
        }
        catch (JsonException)
        {
            // bug?
            return Task.FromResult<JSONCommunicator.Response>(new() { Status = "invalid_message", Data = new JObject() });
        }
        catch (Exception)
        {
            return Task.FromResult<JSONCommunicator.Response>(new() { Status = "unknown_error", Data = new JObject() });
        }
    }

    private ChatServer m_server;
    private JSONCommunicator m_comm;
    private UserIdentity? m_user;
    private Logger m_logger;
}