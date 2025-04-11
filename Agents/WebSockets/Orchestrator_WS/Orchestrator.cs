namespace Orchestrator_WS;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using Common;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Messages;
using ModelContextProtocol.Protocol.Transport;

using wsAgent.Core;

internal class Orchestrator(IHttpContextAccessor _contextAccessor, IServiceProvider _services) : Expert(_services)
{
    private readonly static ConcurrentDictionary<IPAddress, ChatHistory> _threads = new();
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
                    (string prompt) => SendMessageAndGetResponseAsync(tool.Name, ( "GetAnswer", prompt ), cancellationToken),
                    tool.Name, tool.Description,
                    parameters: [new("prompt") { IsRequired = true, ParameterType = typeof(string) }],
                    returnParameter: new KernelReturnParameterMetadata() { Description = "Prompt response as a JSON object or array to be inferred upon.", ParameterType = typeof(string) })]
            );
        }
    }

    private async Task<string> SendMessageAndGetResponseAsync(string agentName, (string action, string data) message, CancellationToken cancellationToken)
    {
        var originalThread = _threads.GetOrAdd(_contextAccessor.HttpContext!.Connection.RemoteIpAddress!, _ => []);
        var updatedThread = new ChatHistory(originalThread);
        updatedThread.AddUserMessage(message.data);

        var toolResponse = await _mcpClient!.CallToolAsync(agentName, new Dictionary<string, object?> { ["action"] = message.action, ["prompt"] = updatedThread });

        var b64string = JsonSerializer.Deserialize<JsonElement>(toolResponse.Content.Single().Text!).GetProperty("uri").GetString();
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(b64string[(b64string.IndexOf(',') + 1)..]));
        var toolCompletion = JsonSerializer.Deserialize<ToolCompletion>(json)!;
        updatedThread = toolCompletion.Completion;
        var updated = _threads.TryUpdate(_contextAccessor.HttpContext.Connection.RemoteIpAddress!, updatedThread, originalThread);
        Debug.Assert(updated);

        return updatedThread.Last().ToString();
    }

    record ToolCompletion([property: JsonPropertyName("completion")] ChatHistory Completion);
}
