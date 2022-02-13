namespace TinyCart.Eric.Server;

using System.Reflection;
using TinyCart.Eric.Messages.V0;

public class CoreServices
{
    public CoreServices(Logging logging)
    {
        Logging = logging;
    }

    public Logging Logging { get; }

    public AppInfo ServerInfo { get; } = new AppInfo() {
        Name = "Eric.Server",
        Description = "Eric Server",
        Version = SoftwareVersion.FromCallingAssembly(),
        Capabilities = new(),
        SupportedExtensions = new(),
    };
}