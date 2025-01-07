namespace Orchestrator_WS;

using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Common;

using Microsoft.SemanticKernel;

using wsAgent.Core;

internal class OrchestratorExpert(IConfiguration configuration, ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory, Kernel kernel, PromptExecutionSettings promptSettings) : Expert(configuration, loggerFactory, httpClientFactory, kernel, promptSettings)
{
    private readonly static ConcurrentDictionary<string, WebSocket> _experts = new();

    private readonly ILogger _log = loggerFactory.CreateLogger<OrchestratorExpert>();

    protected override bool PerformsIntroduction { get; } = false;

    internal async Task AddAgentAsync(WebSocket webSocket, JsonElement request, CancellationToken cancellationToken)
    {
        var name = Throws.IfNullOrWhiteSpace(request.GetProperty("Name").GetString());
        _log.AddingExpertNameToPanel(name);
        _log.LogDebug("{0}", request);

        _experts.AddOrUpdate(name, webSocket, (_, _) => webSocket);

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
        if (!_experts.TryGetValue(agentName, out var webSocket))
        {
            throw new InvalidOperationException($"No WebSocket connection found for agent {agentName}");
        }

        var messageBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        await webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, cancellationToken);

        var buffer = new byte[1024 * 4];
        var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
        var response = Encoding.UTF8.GetString(buffer, 0, result.Count);

        return response;
    }
}
