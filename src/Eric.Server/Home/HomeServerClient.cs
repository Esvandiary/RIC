namespace TinyCart.Eric.Server;

using System.Security.Cryptography;
using TinyCart.Eric.Messages.V0;
using TinyCart.Eric.Messages.V0.Home;

public class HomeServerClient
{
    public HomeServerClient(HomeServer server, JSONCommunicator comm)
    {
        m_server = server;
        m_comm = comm;
        m_logger = server.Services.Logging.GetLogger($"HomeServerClient[{m_comm.Connection.RemoteAddress}]");

        m_comm.SetRequestHandler("challenge", HandleChallengeRequest);
        m_comm.SetRequestHandler("register", HandleRegisterRequest);
        m_comm.SetRequestHandler("login", HandleLoginRequest);
        m_comm.SetRequestHandler("logout", HandleLogoutRequest);
        m_comm.SetRequestHandler("decrypt", HandleDecryptRequest);
        m_comm.SetRequestHandler("sign", HandleSignRequest);
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
                Response = Convert.ToBase64String(response) };
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

    private Task<JSONCommunicator.Response> HandleRegisterRequest(JSONCommunicator comm, string request, JObject data)
    {
        try
        {
            var rdata = data.ToObject<RegisterRequest>()!;
            var user = m_server.RegisterUser(rdata.Username, rdata.Password, rdata.JoinToken);

            return Task.FromResult<JSONCommunicator.Response>(new() { Status = "success", Data = new JObject() });
        }
        catch (JsonException)
        {
            // bug?
            return Task.FromResult<JSONCommunicator.Response>(new() { Status = "invalid_message", Data = new JObject() });
        }
        catch (CredentialsException ex)
        {
            string status = ex.InvalidCredentialType switch
            {
                CredentialsException.Credential.Username => "invalid_username",
                CredentialsException.Credential.Password => "invalid_password",
                _ => "unknown_error"
            };
            // TODO: write info struct
            return Task.FromResult<JSONCommunicator.Response>(new() { Status = status, Data = new JObject() });
        }
        catch (JoinPolicyException)
        {
            string status = !String.IsNullOrEmpty(data.Value<string>("join_token")) ? "invalid_join_token" : "join_token_required";
            return Task.FromResult<JSONCommunicator.Response>(new() { Status = status, Data = new JObject() });
        }
        catch (Exception)
        {
            return Task.FromResult<JSONCommunicator.Response>(new() { Status = "unknown_error", Data = new JObject() });
        }
    }

    private Task<JSONCommunicator.Response> HandleLoginRequest(JSONCommunicator comm, string request, JObject data)
    {
        if (m_user != null)
        {
            return Task.FromResult<JSONCommunicator.Response>(new() { Status = "already_logged_in", Data = new JObject() });
        }

        try
        {
            var rdata = data.ToObject<LoginRequest>()!;
            var user = m_server.LoginUser(rdata.Username, rdata.Password, rdata.ClientToken, rdata.MFAToken, rdata.JoinToken);

            LoginSuccessResponse response = new() {
                ServerApp = m_server.Services.ServerInfo,
                ServerIdentity = m_server.Identity,
                UserIdentity = new UserIdentity {
                    Name = user.Username,
                    Type = "user",
                    PublicKey = PublicKey.FromRSAKeys(user.Keys),
                    HomeServerPublicKey = PublicKey.FromRSAKeys(m_server.Keys),
                    HomeServerURL = m_server.PublishedURL },
                ClientToken = null
            };

            m_user = user;

            return Task.FromResult<JSONCommunicator.Response>(new() { Status = "success", Data = JObject.FromObject(response) });
        }
        catch (JsonException)
        {
            // bug?
            return Task.FromResult<JSONCommunicator.Response>(new() { Status = "invalid_message", Data = new JObject() });
        }
        catch (CredentialsException ex)
        {
            string status = ex.InvalidCredentialType switch
            {
                CredentialsException.Credential.Username => "unrecognised_user",
                CredentialsException.Credential.Password => "invalid_password",
                CredentialsException.Credential.MFAToken
                    => !String.IsNullOrEmpty(data.Value<string>("mfa_token")) ? "invalid_mfa_token" : "mfa_token_required",
                _ => "unknown_error"
            };
            // TODO: write info struct
            return Task.FromResult<JSONCommunicator.Response>(new() { Status = status, Data = new JObject() });
        }
        catch (JoinPolicyException)
        {
            string status = !String.IsNullOrEmpty(data.Value<string>("join_token")) ? "invalid_join_token" : "join_token_required";
            return Task.FromResult<JSONCommunicator.Response>(new() { Status = status, Data = new JObject() });
        }
        catch (Exception)
        {
            return Task.FromResult<JSONCommunicator.Response>(new() { Status = "unknown_error", Data = new JObject() });
        }
    }

    private Task<JSONCommunicator.Response> HandleLogoutRequest(JSONCommunicator comm, string request, JObject data)
    {
        if (m_user == null)
        {
            return Task.FromResult<JSONCommunicator.Response>(new() { Status = "not_logged_in", Data = new JObject() });
        }

        m_user = null;

        return Task.FromResult<JSONCommunicator.Response>(new() { Status = "success", Data = new JObject() });
    }

    private Task<JSONCommunicator.Response> HandleDecryptRequest(JSONCommunicator comm, string request, JObject data)
    {
        if (m_user == null)
        {
            return Task.FromResult<JSONCommunicator.Response>(new() { Status = "not_logged_in", Data = new JObject() });
        }

        try
        {
            var rdata = data.ToObject<DecryptRequest>()!;
            var failedMessages = new List<string>();
            var decryptedMessages = new List<string>(rdata.EncryptedMessages.Count);
            foreach (var msg in rdata.EncryptedMessages)
            {
                try
                {
                    var decoded = Convert.FromBase64String(msg);
                    var decrypted = m_user!.Keys.Decrypt(decoded);
                    decryptedMessages.Add(Convert.ToBase64String(decrypted));
                }
                catch (CryptographicException)
                {
                    failedMessages.Add(msg);
                }
            }

            if (failedMessages.Count == 0)
            {
                DecryptSuccessResponse response = new() { DecryptedMessages = decryptedMessages };
                return Task.FromResult<JSONCommunicator.Response>(new() { Status = "success", Data = JObject.FromObject(response) });
            }
            else
            {
                DecryptFailureResponse failResponse = new() { InvalidMessages = failedMessages };
                return Task.FromResult<JSONCommunicator.Response>(new() { Status = "invalid_messages", Data = JObject.FromObject(failResponse) });
            }
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

    private Task<JSONCommunicator.Response> HandleSignRequest(JSONCommunicator comm, string request, JObject data)
    {
        if (m_user == null)
        {
            return Task.FromResult<JSONCommunicator.Response>(new() { Status = "not_logged_in", Data = new JObject() });
        }

        // TODO: check hash format matches

        try
        {
            var rdata = data.ToObject<SignRequest>()!;
            var failedMessages = new List<string>();
            var signedHashes = new List<string>(rdata.Messages.Count);
            foreach (var msg in rdata.Messages)
            {
                try
                {
                    var decoded = Convert.FromBase64String(msg);
                    var signed = m_user!.Keys.Sign(decoded);
                    signedHashes.Add(Convert.ToBase64String(signed));
                }
                catch (CryptographicException)
                {
                    failedMessages.Add(msg);
                }
            }

            if (failedMessages.Count == 0)
            {
                SignSuccessResponse response = new() { SignedHashes = signedHashes };
                return Task.FromResult<JSONCommunicator.Response>(new() { Status = "success", Data = JObject.FromObject(response) });
            }
            else
            {
                SignFailureResponse failResponse = new() { InvalidMessages = failedMessages, SupportedHashes = new() }; // TODO
                return Task.FromResult<JSONCommunicator.Response>(new() { Status = "invalid_messages", Data = JObject.FromObject(failResponse) });
            }
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

    private HomeServer m_server;
    private JSONCommunicator m_comm;
    private HomeServerUser? m_user;
    private Logger m_logger;
}