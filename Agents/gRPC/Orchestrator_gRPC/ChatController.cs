namespace Orchestrator_gRPC;

using System.Text.Json.Nodes;

using Common;

using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class ChatController(OrchestratorExpert orchestrator) : ControllerBase
{
    [HttpPost("GetCompletion")]
    public async Task<IActionResult> GetCompletionAsync(CancellationToken cancellationToken)
    {
        HttpRequest req = this.HttpContext.Request;
        JsonObject? body = await req.ReadFromJsonAsync<JsonObject>();
        var prompt = Throws.IfNullOrWhiteSpace(body?["prompt"]?.ToString());
        Expert_gRPC.AnswerResponse r = await orchestrator.GetAnswer(prompt, cancellationToken);
        return Ok(r.Completion);
    }
}
