namespace TinyCart.Eric.Messages.V0;

public class UserIdentity
{
    [JsonProperty("name", Required = Required.Always)]
    public string Name { get; set; } = String.Empty;
    [JsonProperty("type", Required = Required.Always)]
    public string Type { get; set; } = String.Empty;
    [JsonProperty("pubkey", Required = Required.Always)]
    public PublicKey PublicKey { get; set; } = new();
    [JsonProperty("home_server_pubkey", Required = Required.Always)]
    public PublicKey HomeServerPublicKey { get; set; } = new();
    [JsonProperty("home_server_user", Required = Required.Always)]
    public string HomeServerUser { get; set; } = String.Empty;
    [JsonProperty("home_server_user_signature", Required = Required.Always)]
    public string HomeServerUserSignature { get; set; } = String.Empty;
    [JsonProperty("home_server_url")]
    public string? HomeServerURL { get; set; }
}