using System.Net.WebSockets;

namespace TinyCart.Eric;

public static class WSProtocol
{
    public const string JSON = "json";
    public const string BSON = "bson";
    // ordered by preference
    public static readonly string[] Names = {BSON, JSON};

    public static WSConnection CreateConnection(WebSocket socket, string endpoint, Logger logger, bool isClient)
        => socket.SubProtocol switch
        {
            BSON => new WSBSONConnection(socket, endpoint, logger, isClient),
            JSON => new WSJSONConnection(socket, endpoint, logger, isClient),
            _ => throw new ArgumentException($"invalid protocol {socket.SubProtocol} provided")
        };
}