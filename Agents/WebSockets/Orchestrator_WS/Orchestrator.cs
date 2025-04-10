namespace Orchestrator_WS;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Common;

using Microsoft.SemanticKernel;

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Messages;
using ModelContextProtocol.Protocol.Transport;

using wsAgent.Core;

internal class Orchestrator(IHttpContextAccessor _contextAccessor, IServiceProvider _services) : Expert(_services)
{
    private readonly static ConcurrentDictionary<string, ClientWebSocket> _experts = new();
    private IMcpClient? _mcpClient;

    protected override bool PerformsIntroduction { get; } = false;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _mcpClient ??= await McpClientFactory.CreateAsync(_services.GetRequiredService<IClientTransport>(), loggerFactory: _services.GetRequiredService<ILoggerFactory>(), cancellationToken: cancellationToken);
        _mcpClient.RegisterNotificationHandler(NotificationMethods.ToolListChangedNotification, async (notification, ct) =>
        {
            _log.LogInformation($"Tool list has changed");//:\n  {string.Join("\n  ", $"{t.Name}({t.Description})")}");
            await AddMcpToolsToSkAsync(cancellationToken);
        });

        await AddMcpToolsToSkAsync(cancellationToken);
    }

    protected override async Task<string> ProcessMessageAsync(WebSocket caller, string message, CancellationToken cancellationToken)
    {
        JsonElement jsonObject = JsonDocument.Parse(message).RootElement;
        var action = jsonObject.GetProperty("action").GetString();

        switch (action)
        {
            case "Introduce":
                await AddAgentAsync(caller, jsonObject.GetProperty("detail"), cancellationToken);
                return JsonSerializer.Serialize(new { message = "Agent introduced" });

            // Add more cases for other actions

            default:
                return await base.ProcessMessageAsync(caller, message, cancellationToken);
        }
    }

    private async Task AddMcpToolsToSkAsync(CancellationToken cancellationToken)
    {
        Debug.Assert(_mcpClient is not null);

        // Remove all the plugins with the `MCP_` prefix
        foreach (var p in _kernel.Plugins.Where(t => t.Name.StartsWith("MCP_")).ToArray())
        {
            _kernel.Plugins.Remove(p);
        }

        await foreach (var tool in _mcpClient.EnumerateToolsAsync(cancellationToken: cancellationToken))
        {
            _kernel.ImportPluginFromFunctions($"MCP_{tool.Name}", [
                _kernel.CreateFunctionFromMethod(
                    async (string prompt) => {
                        var resp = await _mcpClient!.CallToolAsync(tool.Name, new Dictionary<string,object?>() { ["prompt"] = prompt }, cancellationToken: cancellationToken);
                    },
                    tool.Name, tool.Description,
                    parameters: [new("prompt") { IsRequired = true, ParameterType = typeof(string) }],
                    returnParameter: new KernelReturnParameterMetadata() { Description = "Prompt response as a JSON object or array to be inferred upon.", ParameterType = typeof(string) })]
            );
        }
    }

    private async Task AddAgentAsync(WebSocket webSocket, JsonElement request, CancellationToken cancellationToken)
    {
        var name = Throws.IfNullOrWhiteSpace(request.GetProperty("Name").GetString());
        _log.AddingExpertNameToPanel(name);
        _log.LogDebug("{0}", request);

        var agentClient = new ClientWebSocket();
        UriBuilder b = new UriBuilder(request.GetProperty("Secured").GetBoolean() ? "wss" : "ws",
            Throws.IfNullOrWhiteSpace(_contextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()),
            request.GetProperty("CallbackPort").GetInt16(),
            "/ws/agent");
        await agentClient.ConnectAsync(b.Uri, cancellationToken);

        _experts.AddOrUpdate(name, agentClient, (_, _) => agentClient);

        var description = request.GetProperty("Description").GetString();

        _kernel.ImportPluginFromFunctions(name, [_kernel.CreateFunctionFromMethod(async (string prompt) => {
            var response = await SendMessageAndGetResponseAsync(name, new { action = "GetAnswer", prompt }, cancellationToken);
            return response;
        },
            name, description,
            [new ("prompt") { IsRequired = true, ParameterType = typeof(string) }],
            new () { Description = "Prompt response as a JSON object or array to be inferred upon.", ParameterType = typeof(string) })]
        );
    }

    private async Task<string> SendMessageAndGetResponseAsync(string agentName, object message, CancellationToken cancellationToken)
    {
        if (!_experts.TryGetValue(agentName, out ClientWebSocket? webSocket))
        {
            throw new InvalidOperationException($"No WebSocket connection found for agent {agentName}");
        }

        var messageBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        await webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, cancellationToken);

        var buffer = new byte[1024 * 4];
        WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
        var response = Encoding.UTF8.GetString(buffer, 0, result.Count);

        return response;
    }
}
