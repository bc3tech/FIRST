namespace Agent.Core;

using Azure.Identity;

using Common;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

public static class SKHelpers
{
    public static PromptExecutionSettings CreateAgenticPromptSettings(IConfiguration appConfig, Action<OpenAIPromptExecutionSettings>? configurePromptSettings = null)
    {
        var settings = new OpenAIPromptExecutionSettings
        {
            ChatSystemPrompt = Throws.IfNullOrWhiteSpace(appConfig[Constants.Configuration.VariableNames.SystemPrompt], message: "Missing SystemPrompt environment variable"),
            Temperature = 0.1,
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            User = Environment.MachineName
        };

        configurePromptSettings?.Invoke(settings);

        return settings;
    }

    public static Kernel CreateAgenticKernel(IConfiguration appConfiig, IHttpClientFactory httpClientFactory, ILoggerFactory? loggerFactory = null, Action<IKernelBuilderPlugins>? plugins = null, Action<IKernelBuilder>? configureKernelBuilder = default, Action<Kernel>? configureKernel = default)
    {
        IKernelBuilder kernelBuilder = Kernel.CreateBuilder();

        if (loggerFactory is not null)
        {
            kernelBuilder.Services.AddSingleton(loggerFactory);
        }

        plugins?.Invoke(kernelBuilder.Plugins);

        var endpoint = appConfiig[Constants.Configuration.VariableNames.AzureOpenAIEndpoint];
        if (endpoint is not null)
        {
            if (appConfiig["AzureOpenAIKey"] is not null)
            {
                kernelBuilder.AddAzureOpenAIChatCompletion(
                    appConfiig[Constants.Configuration.VariableNames.AzureOpenAIModelDeployment]!,
                endpoint,
                    appConfiig["AzureOpenAIKey"]!,
                    httpClient: httpClientFactory.CreateClient("AzureOpenAi"));
            }
            else
            {
                kernelBuilder.AddAzureOpenAIChatCompletion(
                    appConfiig[Constants.Configuration.VariableNames.AzureOpenAIModelDeployment]!,
                endpoint,
                    new DefaultAzureCredential(),
                    httpClient: httpClientFactory.CreateClient("AzureOpenAi"));
            }
        }

        endpoint = appConfiig["OpenAIEndpoint"];
        if (endpoint is not null)
        {
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            kernelBuilder.AddOpenAIChatCompletion(appConfiig["OpenAIModelId"]!, new Uri(endpoint), appConfiig["OpenAIKey"] ?? string.Empty);
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        }

        configureKernelBuilder?.Invoke(kernelBuilder);

        Kernel kernel = kernelBuilder.Build();
        configureKernel?.Invoke(kernel);

        return kernel;
    }
}
