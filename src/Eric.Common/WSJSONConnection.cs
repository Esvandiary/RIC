namespace TinyCart.Eric;

using System.Net.WebSockets;

public class WSJSONConnection : WSTextConnection, IJSONConnection, IDisposable
{
    public WSJSONConnection(WebSocket socket, string remote, Logger logger, bool isClient)
        : base(socket, remote, logger, isClient)
    {
        ReceivedTextHandler = AttemptDispatchAsync;
    }

    public IJSONConnection.JSONReceivedAction? ReceivedJSONHandler { get; set; }

    public IJSONConnection.MessageValidator Validator { get; set; } = (JObject _) => true;

    public async Task SendJSONAsync(JObject obj) => await SendTextAsync(obj.ToString(Formatting.None));

    public async Task<bool> AttemptDispatchAsync(string message)
    {
        try
        {
            var o = JObject.Parse(message);
            if (!Validator(o))
                return false;

            await (ReceivedJSONHandler?.Invoke(o) ?? Task.CompletedTask);
            return true;
        }
        catch (Exception ex)
        {
            m_logger.Error("EXCEPTION in AttemptDispatch: {0}", ex.Message);
            m_logger.Error("received data: {0}", message);
            m_logger.Error("stack trace: {0}", ex.StackTrace ?? "(none)");
        }
        return false;
    }
}