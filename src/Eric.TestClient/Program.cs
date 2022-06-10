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
using TinyCart.Eric.Client;

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
            ClientApp,
            new Logging(new SerilogLoggerFactory(sLogger)));

        var logger = cs.Logging.GetLogger("client");

        HomeClient? homeClient = null;
        ChatClient? chatClient = null;
        try
        {
            logger.Info($"going to connect to {args[0]} ...");
            var baseUri = new Uri(args[0]);

            homeClient = await HomeClient.ConnectAsync(cs, baseUri);
            await homeClient.VerifyServerIdentity();
            await homeClient.Register("test", "potato");
            await homeClient.Login("test", "potato");

            chatClient = await ChatClient.ConnectAsync(cs, baseUri);
            await chatClient.VerifyServerIdentity();
            await chatClient.ConnectChat(homeClient);
        }
        finally
        {
            if (chatClient != null)
            {
                await chatClient!.CloseAsync("application exiting");
                chatClient!.Dispose();
            }
            if (homeClient != null)
            {
                await homeClient!.CloseAsync("application exiting");
                homeClient!.Dispose();
            }
            sLogger.Dispose();
        }
    }
}