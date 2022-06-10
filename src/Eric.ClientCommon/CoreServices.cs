namespace TinyCart.Eric.Client;

using TinyCart.Eric.Messages.V0;

public class CoreServices
{
    public CoreServices(AppInfo app, Logging logging)
    {
        ClientInfo = app;
        Logging = logging;
    }

    public Logging Logging { get; }

    public AppInfo ClientInfo { get; init; }
}