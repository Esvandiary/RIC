namespace TinyCart.Eric.Messages.V0;

public class AppInfo
{
    [JsonProperty("name", Required = Required.Always)]
    public string Name { get; set; } = String.Empty;
    [JsonProperty("description")]
    public string? Description { get; set; }
    [JsonProperty("version", Required = Required.Always)]
    public SoftwareVersion Version { get; set; } = new();
    [JsonProperty("capabilities", Required = Required.Always)]
    public List<string> Capabilities { get; set; } = new();
    [JsonProperty("extensions", Required = Required.Always)]
    public Dictionary<string, List<int>> SupportedExtensions { get; set; } = new();
}