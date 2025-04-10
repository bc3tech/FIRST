using AgentsMcpServer;

using wsAgent.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.AddSemanticKernel();
builder.Services
    .AddHostedService<Server>()
    .AddHttpClient()
    .AddHttpContextAccessor()
    .AddMcpServer();

WebApplication app = builder.Build();

app.MapMcp();
app.UseWebSockets();

app.Map("/ws/register", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        System.Net.WebSockets.WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
        Server orchestratorService = context.RequestServices.GetServices<IHostedService>().OfType<Server>().First();
        await orchestratorService.HandleWebSocketAsync(webSocket, context.RequestAborted);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

await app.RunAsync();
