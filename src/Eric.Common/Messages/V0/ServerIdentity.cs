namespace TinyCart.Eric.Messages.V0;

public class ServerIdentity
{

    [JsonProperty("pubkey", Required = Required.Always)]
    public PublicKey PublicKey { get; set; } = new();
    [JsonProperty("name", Required = Required.Always)]
    public string Name { get; set; } = String.Empty;
    [JsonProperty("description")]
    public string Description { get; set; } = String.Empty;
    [JsonProperty("url", Required = Required.Always)]
    public string URL { get; set; } = String.Empty;
}