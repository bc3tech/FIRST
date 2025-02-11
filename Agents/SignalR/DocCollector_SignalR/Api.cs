namespace DocCollector_SignalR;

using System.ComponentModel;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
internal sealed partial class Api
{
    private readonly Kernel _kernel;
    private readonly KernelArguments _kernelArgs;

    public Api(Kernel kernel, PromptExecutionSettings promptExecutionSettings)
    {
        _kernel = kernel;
        var settings = OpenAIPromptExecutionSettings.FromExecutionSettings(promptExecutionSettings);
        settings.ResponseFormat = typeof(LoanDocument);
        _kernelArgs = new(promptExecutionSettings);
    }

    [KernelFunction, Description("Given requirements for a loan, will collect and return the list of loan documents as a collection of documents with fields and descriptions which are required to apply for a loan meeting the requirements.")]
    [return: Description("A collection of loan documents with fields and descriptions which are required to apply for a loan meeting the requirements.")]
    public Task<LoanDocument[]?> GetLoanDocumentsAsync(
        [Description("The requirements for the loan.")]
        string requirements) => _kernel.InvokePromptAsync<LoanDocument[]>("Given the requirements for a loan, please provide the list of loan documents with fields and descriptions which are required to apply for a loan meeting the requirements.", _kernelArgs);
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
}
