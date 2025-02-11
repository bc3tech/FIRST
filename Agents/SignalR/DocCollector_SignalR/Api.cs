namespace DocCollector_SignalR;

using System.ComponentModel;
using System.Text.Json;

using global::Agent.Core;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
internal sealed partial class Api
{
    private readonly Kernel _kernel;
    private readonly KernelArguments _kernelArgs;

    public Api(IConfiguration appConfig, IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, PromptExecutionSettings promptExecutionSettings)
    {
        _kernel = SKHelpers.CreateAgenticKernel(appConfig, httpClientFactory, loggerFactory);

        var settings = OpenAIPromptExecutionSettings.FromExecutionSettings(promptExecutionSettings);
        settings.ResponseFormat = @"{
""type"":""json_schema"",
""json_schema"": {
  ""$schema"": ""http://json-schema.org/draft-07/schema#"",
  ""type"": ""array"",
  ""items"": {
    ""type"": ""object"",
    ""properties"": {
      ""Name"": {
        ""type"": ""string""
      },
      ""Fields"": {
        ""type"": ""array"",
        ""items"": {
          ""type"": ""object"",
          ""properties"": {
            ""FieldName"": {
              ""type"": ""string""
            },
            ""Description"": {
              ""type"": ""string""
            }
          },
          ""required"": [""FieldName"", ""Description""]
        }
      }
    },
    ""required"": [""Name"", ""Fields""]
  }
} }
";

        _kernelArgs = new(promptExecutionSettings);
    }

    [KernelFunction, Description("Given requirements for a loan, collects and return loan documents required to apply for a loan meeting the requirements.")]
    [return: Description("A JSON Dictionary representing a collection of loan documents with fields and descriptions which are required to apply for a loan meeting the requirements given. The user should then be prompted ONE AT A TIME to answer each available field in each document.")]
    public async Task<string> GetLoanDocumentsAsync(
        [Description("A detailed list of the requirements for the loan so the correct documents can be located and returned.")]
        string requirements)
    {
        var completion = await _kernel.InvokePromptAsync($@"Given the following requirements for a loan, please provide the list of loan documents with fields and descriptions which are required to apply for a loan meeting these requirements.

REQUIREMENTS
{requirements}", _kernelArgs).ConfigureAwait(false);

        try
        {
            return JsonSerializer.Serialize(completion.GetValue<LoanDocument[]>()!);
        }
        catch (Exception)
        {
            return completion.ToString();
        }
    }
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
}
