namespace TinyCart.Eric;

public class JSONCommunicator
{
    public struct Response
    {
        public string Status { get; init; }
        public JObject Data { get; init; }
    }

    public delegate bool MessageValidator(JObject message);

    public delegate Task MessageHandler(JSONCommunicator conn, string message, JObject data);
    public delegate Task<Response> RequestHandler(JSONCommunicator conn, string request, JObject data);
    public delegate Task ResponseHandler(JSONCommunicator conn, string request, string status, JObject data);

    private static readonly MessageValidator DefaultValidator = (JObject _) => true;

    public IJSONConnection Connection { get => m_conn; }

    public JSONCommunicator(IJSONConnection conn, Logger logger, MessageValidator? validator = null)
    {
        m_conn = conn;
        m_conn.ReceivedJSONHandler = AttemptDispatchAsync;
        m_logger = logger;
        m_validator = validator ?? DefaultValidator;
    }

    public async Task CloseAsync(string message) => await m_conn.CloseAsync(message);
    public async Task CloseAsync(string message, CancellationToken token) => await m_conn.CloseAsync(message, token);

    public async Task SendMessageAsync(string name, JObject data)
    {
        JObject root = new JObject();
        root.Add("time", DateTimeOffset.UtcNow);
        root.Add("type", "message");
        root.Add("name", name);
        root.Add("data", data);
        await m_conn.SendJSONAsync(root);
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
        await m_conn.SendJSONAsync(root);
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
        await m_conn.SendJSONAsync(root);
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

    public async Task AttemptDispatchAsync(JObject o)
    {
        try
        {
            if (!m_validator(o))
                return;

            string type = o.Value<string>("type")!;
            string name = o.Value<string>("name")!;

            switch (type)
            {
            case "message":
                if (m_messageHandlers.TryGetValue(name, out var mHandler))
                {
                    await mHandler(this, name, o.Value<JObject>("data")!);
                }
                break;
            case "request":
                if (m_requestHandlers.TryGetValue(name, out var rHandler))
                {
                    uint convid = o.Value<uint>("conversation")!;
                    var response = await rHandler(this, name, o.Value<JObject>("data")!);
                    await SendResponseAsync(name, response.Status, response.Data, convid);
                }
                break;
            case "response":
            {
                uint convid = o.Value<uint>("conversation")!;
                if (m_requests.TryRemove(convid, out var source))
                {
                    source.SetResult(new Response { Status = o.Value<string>("status")!, Data = o.Value<JObject>("data")! });
                }
                else
                {
                    m_logger.Error("failed to handle response to convID {0}: unknown request", convid);
                }
                break;
            }
            default:
                m_logger.Error("AttemptDispatch received invalid message type {0}", type);
                break;
            }
        }
        catch (Exception ex)
        {
            m_logger.Error("EXCEPTION in AttemptDispatch: {0}", ex.Message);
            m_logger.Error("stack trace: {0}", ex.StackTrace ?? "(none)");
        }
    }


    public void SetMessageHandler(string message, MessageHandler fn) => m_messageHandlers[message] = fn;
    public void ClearMessageHandler(string message) => m_messageHandlers.TryRemove(message, out var _);

    public void SetRequestHandler(string request, RequestHandler fn) => m_requestHandlers[request] = fn;
    public void ClearRequestHandler(string request) => m_requestHandlers.TryRemove(request, out var _);

    public void Dispose() => Dispose(true);
    ~JSONCommunicator() => Dispose(false);
    protected void Dispose(bool disposing)
    {
        if (!m_disposed)
        {
            foreach (var r in m_requests)
                r.Value.SetCanceled();
        }
        if (disposing && !m_disposed)
        {
            m_messageHandlers.Clear();
            m_requestHandlers.Clear();
            m_requests.Clear();
            m_conn.Dispose();

            GC.SuppressFinalize(this);
        }
        m_disposed = true;
    }

    private IJSONConnection m_conn;
    private Logger m_logger;
    private bool m_disposed = false;

    private uint m_nextConversationID;
    private MessageValidator m_validator;
    private ConcurrentDictionary<uint, TaskCompletionSource<Response>> m_requests = new();
    private ConcurrentDictionary<string, MessageHandler> m_messageHandlers = new();
    private ConcurrentDictionary<string, RequestHandler> m_requestHandlers = new();
}