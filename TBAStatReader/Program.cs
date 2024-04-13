﻿using Common;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Azure.SignalR;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using TBAStatReader;

internal partial class Program
{
    private static async Task Main(string[] args)
    {
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        cts.Token.Register(() => Console.WriteLine("Cancellation requested. Exiting..."));

        HostApplicationBuilder b = Host.CreateApplicationBuilder(args);
        b.Services.AddHostedService<Worker>()
            .AddTransient<DebugHttpHandler>()
            .AddLogging(lb =>
            {
                lb.AddSimpleConsole(o =>
                {
                    o.SingleLine = true;
                    o.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Enabled;
                    o.IncludeScopes = true;
                });
            })
            .AddHttpLogging(o => o.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestBody | Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.ResponseBody);

        var signalRConnString = b.Configuration["SignalRConnectionString"];
        if (!string.IsNullOrWhiteSpace(signalRConnString))
        {
            var options = new ServiceManagerOptions
            {
                ConnectionString = signalRConnString
            };

            ServiceManager signalRserviceManager = new ServiceManagerBuilder()
                .WithOptions(o =>
                {
                    o.ConnectionString = options.ConnectionString;
                    o.ServiceTransportType = ServiceTransportType.Persistent;
                })
                .WithLoggerFactory(b.Services.BuildServiceProvider().GetService<ILoggerFactory>())
                .BuildServiceManager();
            ServiceHubContext signalRhub = await signalRserviceManager.CreateHubContextAsync(b.Configuration["SignalRHubName"] ?? "TBABot", cts.Token);
            Microsoft.AspNetCore.Http.Connections.NegotiationResponse userNegotiation = await signalRhub.NegotiateAsync(new NegotiationOptions
            {
                UserId = "endUser",
                EnableDetailedErrors = true
            });

            b.Services.AddSingleton<IServiceHubContext>(signalRhub);
            b.Services.AddSingleton<HubConnection>(sp =>
            {
                return new HubConnectionBuilder()
                    .WithUrl(userNegotiation.Url!, options =>
                    {
                        options.AccessTokenProvider = () => Task.FromResult(userNegotiation.AccessToken);
                        options.HttpMessageHandlerFactory = f => new DebugHttpHandler(sp.GetRequiredService<ILoggerFactory>(), f);
                    })
                    .ConfigureLogging(lb =>
                    {
                        lb.SetMinimumLevel(LogLevel.Warning);
                        lb.AddSimpleConsole(o =>
                        {
                            o.SingleLine = true;
                            o.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Enabled;
                            o.IncludeScopes = true;
                        });
                    })
                    .Build();
            });
        }

        b.Services.AddHttpClient("Orchestrator", (sp, c) =>
            {
                IConfiguration config = sp.GetRequiredService<IConfiguration>();
                ILogger? log = sp.GetService<ILoggerFactory>()?.CreateLogger("OrchestratorClientCreator");
                c.BaseAddress = new(sp.GetRequiredService<IConfiguration>()["OrchestratorEndpoint"] ?? throw new ArgumentNullException("Endpoint missing for 'Orchestrator' configuration options"));
                log?.LogTrace("SignalR Connection String: {SignalRConnectionString}", signalRConnString);

                if (!string.IsNullOrWhiteSpace(signalRConnString))
                {
                    c.DefaultRequestHeaders.Add("X-SignalR-Hub-ConnectionString", signalRConnString);
                }
            })
            .AddHttpMessageHandler<DebugHttpHandler>();

        await b.Build().RunAsync(cts.Token);
    }
}