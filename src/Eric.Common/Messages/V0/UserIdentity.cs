namespace TinyCart.Eric.Messages.V0;

public class UserIdentity
{
    [JsonProperty("name", Required = Required.Always)]
    public string Name { get; set; } = String.Empty;
    [JsonProperty("pubkey", Required = Required.Always)]
    public PublicKey PublicKey { get; set; } = new();
    [JsonProperty("home_server", Required = Required.Always)]
    public PublicKey HomeServerPublicKey { get; set; } = new();
    [JsonProperty("home_server_url")]
    public string? HomeServerURL { get; set; }
}