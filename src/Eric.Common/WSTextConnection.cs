namespace TinyCart.Eric;

using System.Net;
using System.Net.WebSockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class WSTextConnection : WSConnection, IDisposable
{
    public struct Response
    {
        public string Status { get; init; }
        public JObject Data { get; init; }
    }

    public delegate bool MessageValidator(JObject message);

    public delegate Task MessageHandler(WSTextConnection conn, string message, JObject data);
    public delegate Task<Response> RequestHandler(WSTextConnection conn, string request, JObject data);
    public delegate Task ResponseHandler(WSTextConnection conn, string request, string status, JObject data);


    public WSTextConnection(WebSocket socket, string remote, Logger logger, bool isClient, MessageValidator? validator = null)
        : base(socket, remote, logger, isClient)
    {
        m_nextConversationID = (isClient) ? 0U : 1U;
        m_validator = validator ?? (MessageValidator)((JObject _) => true);
    }

    protected override async Task InternalReadWhileOpenAsync()
    {
        try
        {
            string? msg = null;
            while (!String.IsNullOrEmpty(msg = await ReceiveTextAsync()))
                await AttemptDispatchAsync(msg!);
        }
        catch (Exception ex)
        {
            m_logger.Error(ex, $"Exception in WebSocket read handler: {ex.Message}");
        }
    }

    public async Task SendMessageAsync(string name, JObject data)
    {
        JObject root = new JObject();
        root.Add("time", DateTimeOffset.UtcNow);
        root.Add("type", "message");
        root.Add("name", name);
        root.Add("data", data);
        await SendTextAsync(root.ToString(Formatting.None));
    }

    public async Task<uint> SendRequestAsync(string name, JObject data)
    {
        uint convid = unchecked(Interlocked.Add(ref m_nextConversationID, 2) - 2);
        // TODO: throw more descriptively if this ever happens somehow?
        if (!m_requests.TryAdd(convid, new TaskCompletionSource<Response>()))
            throw new ApplicationException("failed to add request convID to dictionary: out of convIDs?!");

        JObject root = new JObject();
        root.Add("time", DateTimeOffset.UtcNow);
        root.Add("type", "request");
        root.Add("conversation", convid);
        root.Add("name", name);
        root.Add("data", data);
        await SendTextAsync(root.ToString(Formatting.None));
        return convid;
    }

    private async Task SendResponseAsync(string name, string status, JObject data, uint convid)
    {
        JObject root = new JObject();
        root.Add("time", DateTimeOffset.UtcNow);
        root.Add("type", "response");
        root.Add("conversation", convid);
        root.Add("status", status);
        root.Add("name", name);
        root.Add("data", data);
        await SendTextAsync(root.ToString(Formatting.None));
    }

    public async Task<Response> WaitForResponseAsync(uint convid)
        => await WaitForResponseAsync(convid, CancellationToken.None);

    public async Task<Response> WaitForResponseAsync(uint convid, CancellationToken token)
    {
        if (m_requests.TryGetValue(convid, out var source))
            return await source!.Task.WaitAsync(token);
        else
            throw new ArgumentException($"no conversation with ID {convid} found", nameof(convid));
    }

    public async Task<Response> SendRequestAndWaitAsync(string name, JObject data)
        => await SendRequestAndWaitAsync(name, data, CancellationToken.None);

    public async Task<Response> SendRequestAndWaitAsync(string name, JObject data, CancellationToken token)
        => await WaitForResponseAsync(await SendRequestAsync(name, data), token);

    public async Task<bool> AttemptDispatchAsync(string message)
    {
        try
        {
            var o = JObject.Parse(message);
            if (!m_validator(o))
                return false;

            string type = o.Value<string>("type")!;
            string name = o.Value<string>("name")!;

            switch (type)
            {
            case "message":
                if (m_messageHandlers.TryGetValue(name, out var mHandler))
                {
                    await mHandler(this, name, o.Value<JObject>("data")!);
                    return true;
                }
                break;
            case "request":
                if (m_requestHandlers.TryGetValue(name, out var rHandler))
                {
                    uint convid = o.Value<uint>("conversation")!;
                    var response = await rHandler(this, name, o.Value<JObject>("data")!);
                    await SendResponseAsync(name, response.Status, response.Data, convid);
                    return true;
                }
                break;
            case "response":
            {
                uint convid = o.Value<uint>("conversation")!;
                if (m_requests.TryRemove(convid, out var source))
                {
                    source.SetResult(new Response { Status = o.Value<string>("status")!, Data = o.Value<JObject>("data")! });
                    return true;
                }
                else
                {
                    m_logger.Error($"failed to handle response to convID {convid}: unknown request");
                }
                break;
            }
            default:
                m_logger.Error($"AttemptDispatch received invalid message type {type}");
                break;
            }
        }
        catch (Exception ex)
        {
            m_logger.Error($"EXCEPTION in AttemptDispatch: {ex.Message}");
            m_logger.Error($"received data: {message}");
            m_logger.Error($"stack trace: {ex.StackTrace}");
        }
        return false;
    }


    public void SetMessageHandler(string message, MessageHandler fn) => m_messageHandlers[message] = fn;
    public void ClearMessageHandler(string message) => m_messageHandlers.TryRemove(message, out var _);

    public void SetRequestHandler(string request, RequestHandler fn) => m_requestHandlers[request] = fn;
    public void ClearRequestHandler(string request) => m_requestHandlers.TryRemove(request, out var _);

#region Low-level comms
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
#endregion

    protected override void Dispose(bool disposing)
    {
        if (disposing && !m_disposed)
        {
            m_messageHandlers.Clear();
            m_requestHandlers.Clear();

            foreach (var r in m_requests)
                r.Value.SetCanceled();
            m_requests.Clear();
        }
        base.Dispose(disposing);
    }

    private uint m_nextConversationID;
    private MessageValidator m_validator;
    private ConcurrentDictionary<uint, TaskCompletionSource<Response>> m_requests = new();
    private ConcurrentDictionary<string, MessageHandler> m_messageHandlers = new();
    private ConcurrentDictionary<string, RequestHandler> m_requestHandlers = new();
    public int BufferSize { get; set; } = DefaultBufferSize;
    private MemoryStream m_buffer = new MemoryStream(DefaultBufferSize);

    public const int DefaultBufferSize = 8192;
}