namespace DocReviewer_SignalR;

using System.ComponentModel;
using System.Text.Json;

using global::Agent.Core;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using static DocCollector_SignalR.Api;

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
internal sealed class Api
{
    private readonly Kernel _kernel;
    private readonly KernelArguments _kernelArgs;

    public Api(IConfiguration appConfig, IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, PromptExecutionSettings promptExecutionSettings)
    {
        _kernel = SKHelpers.CreateAgenticKernel(appConfig, httpClientFactory, loggerFactory);

        var settings = OpenAIPromptExecutionSettings.FromExecutionSettings(promptExecutionSettings);
        settings.ResponseFormat = typeof(DocumentReview);
        _kernelArgs = new(promptExecutionSettings);
    }

    public record DocumentReview(bool Passed, string reasoning);

    [KernelFunction, Description("Given a collection of FILLED loan documents, will review the documents and return a summary of the review.")]
    [return: Description("A JSON object indicating whether the documents passed review and the reasoning behind the decision.")]
    public async Task<DocumentReview?> ReviewDocumentsAsync(
        [Description("The loan documents to review. All fields should have a `RESPONSE` tag in them to indicate the user has responded and filled them out.")]
        LoanDocument[] loanDocuments)
    {
        DocumentReview? completion = await _kernel.InvokePromptAsync<DocumentReview>($@"Given the following filled loan documents, please review the documents and provide a summary of the review.

DOCUMENTS

{JsonSerializer.Serialize(loanDocuments)}", _kernelArgs).ConfigureAwait(false);

        return completion;
    }
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
}
