namespace TinyCart.Eric.Messages.V0.Home;

public class LoginRequest
{
    [JsonProperty("client_app", Required = Required.Always)]
    public AppInfo ClientApp { get; set; } = new();
    [JsonProperty("username", Required = Required.Always)]
    public string Username { get; set; } = String.Empty;
    [JsonProperty("password", Required = Required.Always)]
    public Password Password { get; set; } = new();
    [JsonProperty("client_token")]
    public string? ClientToken { get; set; }
    [JsonProperty("mfa_token")]
    public string? MFAToken { get; set; }
    [JsonProperty("join_token")]
    public string? JoinToken { get; set; }
}

public class LoginSuccessResponse
{
    [JsonProperty("server_app", Required = Required.Always)]
    public AppInfo ServerApp { get; set; } = new();
    [JsonProperty("server_identity", Required = Required.Always)]
    public ServerIdentity ServerIdentity { get; set; } = new();
    [JsonProperty("user", Required = Required.Always)]
    public UserIdentity UserIdentity { get; set; } = new();
    [JsonProperty("client_token")]
    public string? ClientToken { get; set; }
}

public class LoginFailureResponse
{
    [JsonProperty("disallowed_client_reason")]
    public string? DisallowedClientReason { get; set; }
    [JsonProperty("disallowed_user_reason")]
    public string? DisallowedUserReason { get; set; }
}