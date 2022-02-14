namespace TinyCart.Eric.Messages.V0.Chat;

public class ConnectRequest
{
    [JsonProperty("client_app", Required = Required.Always)]
    public AppInfo ClientApp { get; set; } = new();
    [JsonProperty("user", Required = Required.Always)]
    public UserIdentity User { get; set; } = new();
    [JsonProperty("challenge", Required = Required.Always)]
    public string Challenge { get; set; } = String.Empty;
    [JsonProperty("join_token")]
    public string? JoinToken { get; set; }
}

public class ConnectSuccessResponse
{
    [JsonProperty("server_app", Required = Required.Always)]
    public AppInfo ServerApp { get; set; } = new();
    [JsonProperty("server_identity", Required = Required.Always)]
    public ServerIdentity ServerIdentity { get; set; } = new();
}

public class ConnectFailureResponse
{
    [JsonProperty("disallowed_client_reason")]
    public string? DisallowedClientReason { get; set; }
    [JsonProperty("disallowed_user_reason")]
    public string? DisallowedUserReason { get; set; }
}


public class DisconnectRequest
{
    [JsonProperty("reason")]
    public string? Reason { get; set; }
}