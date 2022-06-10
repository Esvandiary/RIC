namespace TinyCart.Eric;

public interface IJSONConnection : IDisposable
{
    string RemoteAddress { get; }
    bool IsOpen { get; }

    Task SendJSONAsync(JObject obj);
    Task ReadWhileOpenAsync();
    Task CloseAsync(string message);
    Task CloseAsync(string message, CancellationToken token);

    public delegate Task JSONReceivedAction(JObject obj);
    JSONReceivedAction? ReceivedJSONHandler { get; set; }

    public delegate bool MessageValidator(JObject obj);
    MessageValidator Validator { get; }
}