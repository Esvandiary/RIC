namespace TinyCart.Eric.Messages.V0;

public class SupportedExtension
{
    [JsonProperty("name", Required = Required.Always)]
    public string Name { get; set; } = String.Empty;
    [JsonProperty("versions", Required = Required.Always)]
    public List<int> Versions { get; set; } = new();
}