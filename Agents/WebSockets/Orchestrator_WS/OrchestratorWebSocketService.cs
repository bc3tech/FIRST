namespace Orchestrator_WS;

using Microsoft.AspNetCore.Http;

using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

internal class OrchestratorWebSocketService(OrchestratorExpert _orchestrator)
{
    public async Task HandleWebSocketAsync(WebSocket webSocket, HttpContext context)
    {
        var buffer = new byte[1024 * 4];
        WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        while (!result.CloseStatus.HasValue)
        {
            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            var response = await ProcessMessageAsync(webSocket, message, context.RequestAborted).ConfigureAwait(false);

            var responseBytes = Encoding.UTF8.GetBytes(response);
            await webSocket.SendAsync(new ArraySegment<byte>(responseBytes), result.MessageType, result.EndOfMessage, CancellationToken.None);

            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        }

        await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
    }

    private async Task<string> ProcessMessageAsync(WebSocket webSocket, string message, CancellationToken cancellationToken)
    {
        var jsonObject = JsonDocument.Parse(message).RootElement;
        var action = jsonObject.GetProperty("action").GetString();

        switch (action)
        {
            case "GetAnswer":
                var prompt = jsonObject.GetProperty("prompt").GetString();
                var completion = await _orchestrator.GetAnswerAsync(prompt, cancellationToken);
                return JsonSerializer.Serialize(new { completion });

            case "GetAnswerStream":
                // Implement streaming logic here
                return JsonSerializer.Serialize(new { message = "Streaming not implemented" });

            // Add more cases for other actions

            case "Introduce":
                await _orchestrator.AddAgentAsync(webSocket, jsonObject.GetProperty("detail"), cancellationToken);
                return JsonSerializer.Serialize(new { message = "Agent introduced" });

            default:
                return JsonSerializer.Serialize(new { error = "Unknown action" });
        }
    }
}
