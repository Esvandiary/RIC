namespace TinyCart.Eric.Messages.V0.Home;

public class RegisterRequest
{
    [JsonProperty("username", Required = Required.Always)]
    public string Username { get; set; } = String.Empty;
    [JsonProperty("password", Required = Required.Always)]
    public Password Password { get; set; } = new();
    [JsonProperty("join_token")]
    public string? JoinToken { get; set; }
}

public class RegisterFailureResponse
{
    [JsonProperty("username_error_reason")]
    public string? UsernameErrorReason { get; set; }
    [JsonProperty("password_error_reason")]
    public string? PasswordErrorReason { get; set; }
}