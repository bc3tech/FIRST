namespace LoanDrafter_SignalR;

using System.ComponentModel;

using global::Agent.Core;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

internal sealed class Api(IConfiguration appConfig, IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, PromptExecutionSettings promptExecutionSettings)
{
    private readonly Kernel _kernel = SKHelpers.CreateAgenticKernel(appConfig, httpClientFactory, loggerFactory);
    private readonly KernelArguments _kernelArgs = new(promptExecutionSettings);

    [KernelFunction, Description("Given a collection of loan documents, will draft a loan application in markdown format using the provided documents as input.")]
    public async Task<string?> CreateDraftApplicationAsync(
        [Description("The FILLED and approved loan documents to use in drafting a loan application. Should contain the name of each document with all fields having a `RESPONSE - ` prefix (to indicate the user has responded and filled them out) along with the user's response")]
        string loanDocuments)
    {
        var completion = await _kernel.InvokePromptAsync($@"Given the following loan documents, please draft a loan application in markdown format using the provided documents as input.

DOCUMENTS

{loanDocuments}", _kernelArgs).ConfigureAwait(false);

        return completion.ToString();
    }
}
