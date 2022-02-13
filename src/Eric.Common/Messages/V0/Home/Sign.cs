namespace TinyCart.Eric.Messages.V0.Home;

public class SignRequest
{
    [JsonProperty("messages", Required = Required.Always)]
    public List<string> Messages { get; set; } = new();
    [JsonProperty("hash", Required = Required.Always)]
    public string Hash { get; set; } = String.Empty;
}

public class SignSuccessResponse
{
    [JsonProperty("signed_hashes", Required = Required.Always)]
    public List<string> SignedHashes { get; set; } = new();
}

public class SignFailureResponse
{
    [JsonProperty("invalid_messages", Required = Required.Always)]
    public List<string> InvalidMessages { get; set; } = new();
    [JsonProperty("supported_hashes", Required = Required.Always)]
    public List<string> SupportedHashes { get; set; } = new();
}

