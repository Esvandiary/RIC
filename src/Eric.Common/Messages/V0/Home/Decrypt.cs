namespace TinyCart.Eric.Messages.V0.Home;

public class DecryptRequest
{
    [JsonProperty("encrypted_messages", Required = Required.Always)]
    public List<string> EncryptedMessages { get; set; } = new();
}

public class DecryptSuccessResponse
{
    [JsonProperty("decrypted_messages", Required = Required.Always)]
    public List<string> DecryptedMessages { get; set; } = new();
}

public class DecryptFailureResponse
{
    [JsonProperty("invalid_messages", Required = Required.Always)]
    public List<string> InvalidMessages { get; set; } = new();
}

