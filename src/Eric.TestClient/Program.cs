namespace TinyCart.Eric.TestClient;

using System.Net.WebSockets;
using System.Reflection;

using TinyCart.Eric.Messages.V0;
using TinyCart.Eric.Messages.V0.Home;

using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Extensions.Logging;
using Newtonsoft.Json;

public static class Program
{
    private static AppInfo ClientApp = new AppInfo
    {
        Name = "Eric.TestClient",
        Description = "Eric Test Client",
        Version = SoftwareVersion.FromCallingAssembly(),
        Capabilities = new(),
        SupportedExtensions = new(),
    };

    public static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine($"Usage: {Assembly.GetExecutingAssembly().GetName().Name} <url>");
            Environment.ExitCode = 1;
            return;
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var sLogger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        var cs = new CoreServices(
            new Logging(new SerilogLoggerFactory(sLogger)));
        var logger = cs.Logging.GetLogger("client");

        WSTextConnection? conn = null;
        try
        {
            logger.Info($"going to connect to {args[0]} ...");
            var ws = new ClientWebSocket();
            var uri = new Uri(args[0]);
            await ws.ConnectAsync(uri, CancellationToken.None);
            conn = new WSTextConnection(ws, uri.AbsoluteUri, logger, true);
            _ = conn.ReadWhileOpenAsync();

            byte[] testData = new byte[64];
            Random.Shared.NextBytes(testData);

            logger.Info("sending challenge");
            var creq = new ChallengeRequest { Challenge = Convert.ToBase64String(testData) };
            var cresult = await conn.SendRequestAndWaitAsync("challenge", JObject.FromObject(creq));
            logger.Info($"got response, status: {cresult.Status}");
            if (cresult.Status != "success")
            {
                logger.Info("challenge request failed");
                Environment.ExitCode = 2;
                return;
            }
            var cresp = cresult.Data.ToObject<ChallengeSuccessResponse>();

            var serverKey = RSAKeys.FromPublicKey(Convert.FromBase64String(cresp!.PublicKey.KeyData));
            if (!serverKey.Verify(testData, Convert.FromBase64String(cresp.Response)))
            {
                logger.Info("server key verification failed");
                Environment.ExitCode = 3;
                return;
            }
            logger.Info("server key verified");

            logger.Info("sending register request");
            var rreq = new RegisterRequest {
                Username = "test",
                Password = new Messages.V0.Password { Data = "potato", Format = "plaintext" } };
            var rresult = await conn.SendRequestAndWaitAsync("register", JObject.FromObject(rreq));
            logger.Info($"got response, status: {rresult.Status}");
            if (rresult.Status != "success")
            {
                logger.Info("register request failed");
                Environment.ExitCode = 4;
                return;
            }

            logger.Info("sending login request");
            var lreq = new LoginRequest {

                Username = "test",
                Password = new Messages.V0.Password { Data = "potato", Format = "plaintext" },
                ClientApp = ClientApp,
            };
            var lresult =  await conn.SendRequestAndWaitAsync("login", JObject.FromObject(lreq));
            logger.Info($"got response, status: {lresult.Status}");
            if (lresult.Status != "success")
            {
                logger.Info("login request failed");
                Environment.ExitCode = 5;
                return;
            }
            var lresp = lresult.Data.ToObject<LoginSuccessResponse>();


            await conn.CloseAsync("application exiting");
        }
        finally
        {
            if (conn != null)
                await conn!.CloseAsync("application exiting");
            sLogger.Dispose();
        }
    }
}