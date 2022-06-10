namespace TinyCart.Eric;

using System.Net;
using System.Net.WebSockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class WSBinaryConnection : WSConnection, IDisposable
{
    public delegate Task BytesReceivedAction(byte[] message);

    public WSBinaryConnection(WebSocket socket, string remote, Logger logger, bool isClient)
        : base(socket, remote, logger, isClient)
    {
    }

    protected override async Task InternalReadWhileOpenAsync()
    {
        try
        {
            byte[]? msg = null;
            while ((msg = await ReceiveBytesAsync()) != null)
                await (ReceivedBytesHandler?.Invoke(msg!) ?? Task.CompletedTask);
        }
        catch (Exception ex)
        {
            m_logger.Error(ex, "Exception in WebSocket read handler: {0}", ex.Message);
        }
    }

    public async Task<byte[]?> ReceiveBytesAsync() => await ReceiveBytesAsync(CancellationToken.None);
    public async Task<byte[]?> ReceiveBytesAsync(CancellationToken token)
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

            return m_buffer.ToArray();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buf);
        }
    }

    public async Task SendBytesAsync(byte[] data) => await SendBytesAsync(data, CancellationToken.None);
    public async Task SendBytesAsync(byte[] data, CancellationToken token)
    {
        await m_socket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Binary, true, token);
    }

    public BytesReceivedAction? ReceivedBytesHandler { get; set; }

    public int BufferSize { get; set; } = DefaultBufferSize;
    private MemoryStream m_buffer = new MemoryStream(DefaultBufferSize);

    public const int DefaultBufferSize = 8192;
}