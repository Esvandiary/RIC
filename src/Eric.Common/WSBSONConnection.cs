namespace TinyCart.Eric;

using System.Net;
using System.Net.WebSockets;
using Newtonsoft.Json.Bson;

public class WSBSONConnection : WSBinaryConnection, IJSONConnection, IDisposable
{
    public WSBSONConnection(WebSocket socket, string remote, Logger logger, bool isClient)
        : base(socket, remote, logger, isClient)
    {
        ReceivedBytesHandler = AttemptDispatchAsync;
    }

    public IJSONConnection.JSONReceivedAction? ReceivedJSONHandler { get; set; }

    public IJSONConnection.MessageValidator Validator { get; set; } = (JObject _) => true;

    public async Task SendJSONAsync(JObject obj) => await SendBSONAsync(obj);

    public async Task SendBSONAsync(JObject obj)
    {
        using (var stream = new MemoryStream())
        using (JsonWriter writer = new BsonDataWriter(stream))
        {
            obj.WriteTo(writer);
            await SendBytesAsync(stream.ToArray());
        }
    }

    public async Task<bool> AttemptDispatchAsync(byte[] message)
    {
        try
        {
            using (var stream = new MemoryStream(message))
            using (JsonReader reader = new BsonDataReader(stream))
            {
                var o = JObject.Load(reader);
                if (!Validator(o))
                    return false;

                await (ReceivedJSONHandler?.Invoke(o) ?? Task.CompletedTask);
                return true;
            }
        }
        catch (Exception ex)
        {
            m_logger.Error($"EXCEPTION in AttemptDispatch: {ex.Message}");
            m_logger.Error($"received data of length {message.Length}");
            m_logger.Error($"stack trace: {ex.StackTrace}");
        }
        return false;
    }
}