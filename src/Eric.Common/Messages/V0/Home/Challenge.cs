namespace TinyCart.Eric.Messages.V0.Home;

public class ChallengeRequest
{
    [JsonProperty("challenge", Required = Required.Always)]
    public string Challenge { get; set; } = String.Empty;
}

public class ChallengeSuccessResponse
{
    [JsonProperty("pubkey", Required = Required.Always)]
    public PublicKey PublicKey { get; set; } = new();
    [JsonProperty("challenge_response", Required = Required.Always)]
    public string Response { get; set; } = String.Empty;
}