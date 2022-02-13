namespace TinyCart.Eric.Messages.V0;

public class Password
{
    [JsonProperty("data", Required = Required.Always)]
    public string Data { get; set; } = String.Empty;
    [JsonProperty("format", Required = Required.Always)]
    public string Format { get; set; } = String.Empty;
}