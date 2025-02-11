namespace LoanDrafter_SignalR;

using System.ComponentModel;
using System.Text.Json;

using global::Agent.Core;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

using static DocCollector_SignalR.Api;

internal sealed class Api(IConfiguration appConfig, IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, PromptExecutionSettings promptExecutionSettings)
{
    private readonly Kernel _kernel = SKHelpers.CreateAgenticKernel(appConfig, httpClientFactory, loggerFactory);
    private readonly KernelArguments _kernelArgs = new(promptExecutionSettings);

    [KernelFunction, Description("Given a collection of loan documents, will draft a loan application in markdown format using the provided documents as input.")]
    public async Task<string?> CreateDraftApplicationAsync(
        [Description("The loan documents to draft the application from.")]
        LoanDocument[] documents)
    {
        var completion = await _kernel.InvokePromptAsync<string>($@"Given the following loan documents, please draft a loan application in markdown format using the provided documents as input.

DOCUMENTS

{JsonSerializer.Serialize(documents)}", _kernelArgs).ConfigureAwait(false);

        return completion;
    }
}
