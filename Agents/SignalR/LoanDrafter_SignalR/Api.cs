namespace LoanDrafter_SignalR;

using System.ComponentModel;

using Microsoft.SemanticKernel;

using static DocCollector_SignalR.Api;

internal sealed class Api(Kernel kernel, PromptExecutionSettings promptExecutionSettings)
{
    private readonly KernelArguments _kernelArgs = new(promptExecutionSettings);

    [KernelFunction, Description("Given a collection of loan documents, will draft a loan application in markdown format using the provided documents as input.")]
    public Task<string?> CreateDraftApplication(
        [Description("The loan documents to draft the application from.")]
        LoanDocument[] documents) => kernel.InvokePromptAsync<string>($@"Given the following loan documents, please draft a loan application in markdown format using the provided documents as input.", _kernelArgs);
}
