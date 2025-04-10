namespace AgentsMcpServer;

using System.Collections.Concurrent;
using System.Formats.Asn1;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Common;

using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;

using ModelContextProtocol;
using ModelContextProtocol.Protocol.Messages;
using ModelContextProtocol.Server;

using wsAgent.Core;

internal class Server(IHttpContextAccessor _contextAccessor, IMcpServer mcpServer, IServiceProvider sp) : Expert(sp)
{
    private readonly static ConcurrentDictionary<string, ClientWebSocket> _experts = new();

    protected override bool PerformsIntroduction => false;

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

    private async Task AddAgentAsync(WebSocket webSocket, JsonElement request, CancellationToken cancellationToken)
    {
        var name = Throws.IfNullOrWhiteSpace(request.GetProperty("Name").GetString());
        _log.AddingExpertNameToPanel(name);
        _log.LogDebug("{0}", request);

        var agentClient = new ClientWebSocket();
        var b = new UriBuilder(request.GetProperty("Secured").GetBoolean() ? "wss" : "ws",
            Throws.IfNullOrWhiteSpace(_contextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()),
            request.GetProperty("CallbackPort").GetInt16(),
            "/ws/agent");
        await agentClient.ConnectAsync(b.Uri, cancellationToken);

        _experts.AddOrUpdate(name, agentClient, (_, _) => agentClient);

        var description = request.GetProperty("Description").GetString();

        var newAgentPlugin = _kernel.ImportPluginFromFunctions(name, [_kernel.CreateFunctionFromMethod(async (string prompt) => {
            var response = await SendMessageAndGetResponseAsync(name, new { action = "GetAnswer", prompt }, cancellationToken);
            return response;
        },
            name, description,
            [new ("prompt") { IsRequired = true, ParameterType = typeof(string) }],
            new () { Description = "Prompt response as a JSON object or array to be inferred upon.", ParameterType = typeof(string) })]
        );

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var toolListUpdated = false;
        var mcpTool = McpServerTool.Create(AIFunctionFactory.Create(async (string prompt) =>
        {
            var response = await SendMessageAndGetResponseAsync(name, new { action = "GetAnswer", prompt }, cancellationToken);
            return response;
        }, name, description));
        if (((mcpServer.ServerOptions.Capabilities ??= new()).Tools ??= new()).ToolCollection?.Count is null or 0)
        {
            mcpServer.ServerOptions.Capabilities.Tools!.ToolCollection = [mcpTool];
            toolListUpdated = true;
        }
        else
        {
            var successfulAdd = mcpServer.ServerOptions.Capabilities.Tools!.ToolCollection!.TryAdd(mcpTool);
            if (!successfulAdd)
            {
                _log.LogWarning("'{McpToolName}' already exists in the tool collection.", mcpTool.ProtocolTool.Name);
            }
            else
            {
                toolListUpdated = true;
            }
        }
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        if (toolListUpdated)
        {
            await mcpServer.SendNotificationAsync(NotificationMethods.ToolListChangedNotification, cancellationToken);
        }
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
