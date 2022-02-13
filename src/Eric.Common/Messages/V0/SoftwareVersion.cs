namespace TinyCart.Eric.Messages.V0;

using System.Reflection;

public class SoftwareVersion
{
    [JsonProperty("major", Required = Required.Always)]
    public int Major { get; set; }
    [JsonProperty("minor", Required = Required.Always)]
    public int Minor { get; set; }
    [JsonProperty("patch")]
    public int Patch { get; set; } = 0;
    [JsonProperty("vcs_id")]
    public string? VCSIdentifier { get; set; }
    [JsonProperty("display")]
    public string? DisplayName { get; set; }


    public static SoftwareVersion FromAssembly(Assembly a)
    {
        var ver = a.GetName().Version ?? new Version(0, 0, 0);
        var info = a.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return new SoftwareVersion() {
            Major = ver.Major,
            Minor = ver.Minor,
            Patch = ver.Build,
            DisplayName = info
        };
    }

    public static SoftwareVersion FromExecutingAssembly() => FromAssembly(Assembly.GetExecutingAssembly());
    public static SoftwareVersion FromCallingAssembly() => FromAssembly(Assembly.GetCallingAssembly());
    public static SoftwareVersion FromEntryAssembly() => FromAssembly(Assembly.GetEntryAssembly()!);
}