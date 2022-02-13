namespace TinyCart.Eric.TestClient;

public class CoreServices
{
    public CoreServices(Logging logging)
    {
        Logging = logging;
    }

    public Logging Logging { get; init; }
}