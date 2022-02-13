namespace TinyCart.Eric.Server;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllersWithViews();
        services.AddRazorPages();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime)
    {
        lifetime.ApplicationStopping.Register(OnShutdown);

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseWebAssemblyDebugging();
        }

        app.UseBlazorFrameworkFiles();
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseWebSockets();

        // init
        var coreServices = new CoreServices(
            new Logging(app.ApplicationServices.GetRequiredService<ILoggerFactory>()));

        List<IServerWSEndpoint> wsEndpoints = new();
        wsEndpoints.Add(new HomeServer(coreServices));
        // TODO

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapRazorPages();
            endpoints.MapControllers();
            endpoints.MapFallbackToFile("index.html");

            foreach (var ep in wsEndpoints)
            {
                foreach (var epaddr in ep.EndpointWSAddresses)
                {
                    endpoints.MapGet(epaddr, async context =>
                    {
                        if (context.WebSockets.IsWebSocketRequest)
                        {
                            WebSocket ws = await context.WebSockets.AcceptWebSocketAsync();
                            var conn = await ep.WebSocketConnected(ws, context);
                            await conn.ReadWhileOpenAsync();
                            await ep.WebSocketDisconnected(conn, context);
                            try { await conn.CloseAsync("socket closed"); } catch (Exception) {}
                        }
                        else
                        {
                            context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        }
                    });
                }
            }
        });
    }

    private void OnShutdown()
    {
    }
}