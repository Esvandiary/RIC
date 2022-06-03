namespace TinyCart.Eric;

using System.Net;
using System.Net.WebSockets;

using TinyCart.Eric.Extensions;

public abstract class WSConnection : IDisposable
{
    public WSConnection(WebSocket socket, string remoteAddress, Logger logger, bool isClient)
    {
        m_socket = socket;
        RemoteAddress = remoteAddress;
        m_logger = logger;
        m_isClient = isClient;
        m_stoppedReadingSource.Cancel();
    }

    public async Task ReadWhileOpenAsync()
    {
        m_stoppedReadingSource.TryReset();
        await InternalReadWhileOpenAsync();
        m_stoppedReadingSource.Cancel();
    }

    protected abstract Task InternalReadWhileOpenAsync();

    public async Task CloseAsync(string message) => await CloseAsync(message, CancellationToken.None);
    public async Task CloseAsync(string message, CancellationToken token)
    {
        if (!m_disposed && m_socket.State == WebSocketState.Open)
        {
            if (m_isClient)
            {
                await m_socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, message, token);
                try { await m_stoppedReadingSource.Token; } catch (OperationCanceledException) {}
            }
            else
            {
                await m_socket.CloseAsync(WebSocketCloseStatus.NormalClosure, message, token);
            }
        }
    }

    public string RemoteAddress { get; }

#region IDisposable
    // Must ensure double-dispose is harmless since this can get disposed in a few ways
    // and we want to make sure we can afford to catch them all rather than miss one
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && !m_disposed)
        {
            m_socket.Dispose();
            m_disposed = true;
        }
    }
#endregion

    protected WebSocket m_socket;
    protected Logger m_logger;
    protected bool m_isClient;
    protected CancellationTokenSource m_stoppedReadingSource = new();
    protected bool m_disposed { get; private set; } = false;
}