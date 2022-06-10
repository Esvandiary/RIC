namespace TinyCart.Eric;

using System.Net;
using System.Net.WebSockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class WSTextConnection : WSConnection, IDisposable
{
    public delegate Task TextReceivedAction(string message);

    public WSTextConnection(WebSocket socket, string remote, Logger logger, bool isClient)
        : base(socket, remote, logger, isClient)
    {
    }

    protected override async Task InternalReadWhileOpenAsync()
    {
        try
        {
            string? msg = null;
            while (!String.IsNullOrEmpty(msg = await ReceiveTextAsync()))
                await (ReceivedTextHandler?.Invoke(msg!) ?? Task.CompletedTask);
        }
        catch (Exception ex)
        {
            m_logger.Error(ex, "Exception in WebSocket read handler: {0}", ex.Message);
        }
    }

    public async Task<string?> ReceiveTextAsync() => await ReceiveTextAsync(CancellationToken.None);
    public async Task<string?> ReceiveTextAsync(CancellationToken token)
    {
        // only one thread should ever be trying to receive here, so ensure it
        m_buffer.SetLength(0);
        byte[] buf = ArrayPool<byte>.Shared.Rent(BufferSize);
        try
        {
            while (!m_disposed && !m_socket.CloseStatus.HasValue)
            {
                try
                {
                    var result = await m_socket.ReceiveAsync(new Memory<byte>(buf), token);
                    if (token.IsCancellationRequested || m_socket.CloseStatus.HasValue)
                        return null;
                    if (result.MessageType == WebSocketMessageType.Close)
                        return null;

                    m_buffer.Write(buf, 0, result.Count);
                    if (result.EndOfMessage) break;
                }
                catch (ObjectDisposedException)
                {
                    // likely socket disposed
                    return null;
                }
                catch (OperationCanceledException)
                {
                    return null;
                }
                catch (WebSocketException)
                {
                    // socket error - could be transient or indication we've closed
                    continue;
                }
            }

            if (!m_disposed && m_socket.State == WebSocketState.Open)
                return TextUtil.UTF8NoBOM.GetString(m_buffer.ToArray());
            else
                return null;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buf);
        }
    }

    public async Task SendTextAsync(string text) => await SendTextAsync(text, CancellationToken.None);
    public async Task SendTextAsync(string text, CancellationToken token)
    {
        var buf = ArrayPool<byte>.Shared.Rent(TextUtil.UTF8NoBOM.GetByteCount(text));
        int len = TextUtil.UTF8NoBOM.GetBytes(text, buf);
        try
        {
            await m_socket.SendAsync(new ArraySegment<byte>(buf, 0, len), WebSocketMessageType.Text, true, token);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buf);
        }
    }

    public TextReceivedAction? ReceivedTextHandler { get; set; }

    public int BufferSize { get; set; } = DefaultBufferSize;
    private MemoryStream m_buffer = new MemoryStream(DefaultBufferSize);

    public const int DefaultBufferSize = 8192;
}