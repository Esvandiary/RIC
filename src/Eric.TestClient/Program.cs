namespace TinyCart.Eric.TestClient;

using System.Net.WebSockets;
using System.Reflection;

using TinyCart.Eric.Messages.V0;
using TinyCart.Eric.Messages.V0.Chat;
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

        JSONCommunicator? homeCommunicator = null;
        JSONCommunicator? chatCommunicator = null;
        try
        {
            logger.Info($"going to connect to {args[0]} ...");
            var baseUri = new Uri(args[0]);

            var wsHome = new ClientWebSocket();
            foreach (var name in WSProtocol.Names)
                wsHome.Options.AddSubProtocol(name);
            var homeUri = new Uri(baseUri, "ric0_home");
            await wsHome.ConnectAsync(homeUri, CancellationToken.None);
            var homeConn = WSProtocol.CreateConnection(wsHome, homeUri.AbsoluteUri, logger, true);
            homeCommunicator = new JSONCommunicator((IJSONConnection)homeConn, logger);
            _ = homeCommunicator.Connection.ReadWhileOpenAsync();

            byte[] testData = new byte[64];
            Random.Shared.NextBytes(testData);

            logger.Info("sending challenge to home server");
            var creq = new ChallengeRequest { Challenge = Convert.ToBase64String(testData) };
            var cresult = await homeCommunicator.SendRequestAndWaitAsync("challenge", JObject.FromObject(creq));
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
                logger.Info("home server key verification failed");
                Environment.ExitCode = 3;
                return;
            }
            logger.Info("home server key verified");

            logger.Info("sending register request");
            var rreq = new RegisterRequest {
                Username = "test",
                Password = new Messages.V0.Password { Data = "potato", Format = "plaintext" } };
            var rresult = await homeCommunicator.SendRequestAndWaitAsync("register", JObject.FromObject(rreq));
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
            var lresult =  await homeCommunicator.SendRequestAndWaitAsync("login", JObject.FromObject(lreq));
            logger.Info($"got response, status: {lresult.Status}");
            if (lresult.Status != "success")
            {
                logger.Info("login request failed");
                Environment.ExitCode = 5;
                return;
            }
            var lresp = lresult.Data.ToObject<LoginSuccessResponse>();



            var wsChat = new ClientWebSocket();
            foreach (var name in WSProtocol.Names)
                wsChat.Options.AddSubProtocol(name);
            var chatUri = new Uri(baseUri, "ric0_chat");
            await wsChat.ConnectAsync(chatUri, CancellationToken.None);
            var chatConn = WSProtocol.CreateConnection(wsChat, homeUri.AbsoluteUri, logger, true);
            chatCommunicator = new JSONCommunicator((IJSONConnection)chatConn, logger);
            _ = chatCommunicator.Connection.ReadWhileOpenAsync();

            Random.Shared.NextBytes(testData);
            logger.Info("sending challenge to chat server");
            creq = new ChallengeRequest { Challenge = Convert.ToBase64String(testData) };
            cresult = await chatCommunicator.SendRequestAndWaitAsync("challenge", JObject.FromObject(creq));
            logger.Info($"got response, status: {cresult.Status}");
            if (cresult.Status != "success")
            {
                logger.Info("challenge request failed");
                Environment.ExitCode = 2;
                return;
            }
            cresp = cresult.Data.ToObject<ChallengeSuccessResponse>();

            logger.Info("verifying chat server key...");
            var chatServerKey = RSAKeys.FromPublicKey(Convert.FromBase64String(cresp!.PublicKey.KeyData));
            if (!chatServerKey.Verify(testData, Convert.FromBase64String(cresp.Response)))
            {
                logger.Info("chat server key verification failed");
                Environment.ExitCode = 3;
                return;
            }
            logger.Info("chat server key verified");

            logger.Info("signing chat server key with our privkey...");
            var sreq = new SignRequest { Messages = new() { cresp.PublicKey.KeyData } };
            var sresult = await homeCommunicator.SendRequestAndWaitAsync("sign", JObject.FromObject(sreq));
            logger.Info($"got response, status: {sresult.Status}");
            if (sresult.Status != "success")
            {
                logger.Info("sign request failed");
                Environment.ExitCode = 6;
                return;
            }
            var sresp = sresult.Data.ToObject<SignSuccessResponse>();
            logger.Info("signed chat server key, sending connect request to chat server...");

            var connreq = new ConnectRequest
            {
                ClientApp = ClientApp,
                User = lresp!.UserIdentity,
                Challenge = sresp!.SignedHashes[0],
            };
            var connresult = await chatCommunicator.SendRequestAndWaitAsync("connect", JObject.FromObject(connreq));
            logger.Info($"got response, status: {connresult.Status}");
            if (connresult.Status != "success")
            {
                logger.Info("connect request failed");
                Environment.ExitCode = 7;
                return;
            }
            var connresp = connresult.Data.ToObject<ConnectSuccessResponse>();
            logger.Info($"connected to chat server {connresp!.ServerIdentity.Name}");
        }
        finally
        {
            if (chatCommunicator != null)
            {
                await chatCommunicator!.Connection.CloseAsync("application exiting");
                chatCommunicator!.Dispose();
            }
            if (homeCommunicator != null)
            {
                await homeCommunicator!.Connection.CloseAsync("application exiting");
                homeCommunicator!.Dispose();
            }
            sLogger.Dispose();
        }
    }
}