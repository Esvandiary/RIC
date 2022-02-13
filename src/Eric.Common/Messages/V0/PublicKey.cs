namespace TinyCart.Eric.Messages.V0;

public class PublicKey
{
    [JsonProperty("key", Required = Required.Always)]
    public string KeyData { get; set; } = String.Empty;
    [JsonProperty("format", Required = Required.Always)]
    public string KeyFormat { get; set; } = String.Empty;

    public static PublicKey FromRSAKeys(RSAKeys keys)
        => new() { KeyData = Convert.ToBase64String(keys.PublicKey), KeyFormat = keys.FormatName };
}