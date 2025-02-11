namespace DocReviewer_SignalR;

using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.Json;

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
internal sealed class Api
{
    private readonly Kernel _kernel;
    private readonly KernelArguments _kernelArgs;

    public Api(Kernel kernel, PromptExecutionSettings promptExecutionSettings)
    {
        _kernel = kernel;
        var settings = OpenAIPromptExecutionSettings.FromExecutionSettings(promptExecutionSettings);
        settings.ResponseFormat = typeof(DocumentReview);
        _kernelArgs = new(promptExecutionSettings);
    }

    public record DocumentReview(bool Passed, string reasoning);

    [KernelFunction, Description("Given a collection of loan documents, will review the documents and return a summary of the review.")]
    public Task<DocumentReview> ReviewDocumentsAsync(
        [Description("The loan documents to review. All fields should have a `RESPONSE` tag in them to indicate the user has responded and filled them out.")]
        DocCollector_SignalR.Api.LoanDocument[] loanDocuments)
    {
        return _kernel.InvokePromptAsync<DocumentReview>($@"Given the following loan documents, please review the documents and provide a summary of the review.

DOCUMENTS

{JsonSerializer.Serialize(loanDocuments)}", _kernelArgs);
    }
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
}
