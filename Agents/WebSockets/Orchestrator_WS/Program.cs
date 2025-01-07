using Orchestrator_WS;

using wsAgent.Core.Extensions;

IHostApplicationBuilder builder = WebApplication.CreateBuilder(args)
    .AddExpert<OrchestratorExpert>()
    .AddSemanticKernel();
builder.Services.AddSingleton<OrchestratorWebSocketService>();

WebApplication app = ((WebApplicationBuilder)builder).Build();

// Configure the HTTP request pipeline.
app.UseWebSockets();

// Map WebSocket endpoints
app.Map("/ws/orchestrator", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var orchestratorService = context.RequestServices.GetRequiredService<OrchestratorWebSocketService>();
        await orchestratorService.HandleWebSocketAsync(webSocket, context);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

app.Run();
