namespace TinyCart.Eric;

using System.Net.WebSockets;
using Newtonsoft.Json.Bson;

public class WSBSONConnection : WSBinaryConnection, IJSONConnection, IDisposable
{
    public WSBSONConnection(WebSocket socket, string remote, Logger logger, bool isClient)
        : base(socket, remote, logger, isClient)
    {
        ReceivedBytesHandler = OnBytesReceived;
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

    public async Task<bool> OnBytesReceived(byte[] message)
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
        catch (JsonReaderException ex)
        {
            m_logger.Warning("Error deserializing BSON message: {0}", ex.Message);
        }
        catch (Exception ex)
        {
            m_logger.Error("EXCEPTION in OnBytesReceived: {0}", ex.Message);
            m_logger.Error("received data of length {0}", message.Length);
            m_logger.Error("stack trace: {0}", ex.StackTrace ?? "(none)");
        }
        return false;
    }
}