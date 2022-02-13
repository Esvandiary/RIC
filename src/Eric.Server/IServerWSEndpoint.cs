namespace TinyCart.Eric.Server;

public interface IServerEndpoint
{
    string EndpointName { get; }
    string EndpointDescription { get; }
}

public interface IServerWSEndpoint : IServerEndpoint
{
    ReadOnlyCollection<string> EndpointWSAddresses { get; }
    Task<WSConnection> WebSocketConnected(WebSocket client, HttpContext context);
    Task WebSocketDisconnected(WSConnection conn, HttpContext context);
}