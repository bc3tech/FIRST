namespace Common.Extensions;

using Agent.Core;

using Assistants;

using Azure.Identity;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using TBAAPI.V3Client.Client;

public static class HostExtensions
{
    public static HostApplicationBuilder AddExpert<T>(this HostApplicationBuilder b) where T : notnull, Expert
    {
        ValidateConfigForExpert(b.Configuration);

        b.Services.AddHostedService<T>()
            .AddHttpClient()
            .AddTransient<DebugHttpHandler>()
            .AddLogging(lb =>
            {
                lb.AddSimpleConsole(o =>
                {
                    o.SingleLine = true;
                    o.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Enabled;
                    o.IncludeScopes = true;
                });
            });

        return b;
    }

    private static void ValidateConfigForExpert(ConfigurationManager configuration)
    {
        Throws.IfNullOrWhiteSpace(configuration[Constants.Configuration.Paths.AgentName]);
        Throws.IfNullOrWhiteSpace(configuration[Constants.Configuration.VariableNames.SignalREndpoint]);
    }

    public static HostApplicationBuilder AddSemanticKernel(this HostApplicationBuilder b, Action<IServiceProvider, OpenAIPromptExecutionSettings>? configurePromptSettings = default, Action<IServiceProvider, IKernelBuilder>? configureKernelBuilder = default, Action<IServiceProvider, Kernel>? configureKernel = default)
    {
        ValidateConfigForSemanticKernel(b.Configuration);

        b.Services
            .AddSingleton(sp => SKHelpers.CreateAgenticPromptSettings(b.Configuration, s => configurePromptSettings?.Invoke(sp, s)))
            .AddSingleton(sp =>
            {
                IHttpClientFactory httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                ILoggerFactory loggerFactory = sp.GetRequiredService<ILoggerFactory>();

                return SKHelpers.CreateAgenticKernel(b.Configuration, httpClientFactory, loggerFactory,
                    p => p.AddFromType<Calendar>(),
                    b => configureKernelBuilder?.Invoke(sp, b),
                    k => configureKernel?.Invoke(sp, k));
            });

        return b;
    }

    private static void ValidateConfigForSemanticKernel(IConfiguration config)
    {
        var azureOpenAiEndpointValue = config[Constants.Configuration.VariableNames.AzureOpenAIEndpoint];

        if (!string.IsNullOrEmpty(azureOpenAiEndpointValue))
        {
            Throws.IfNullOrWhiteSpace(config[Constants.Configuration.VariableNames.AzureOpenAIModelDeployment]);
        }

        var openaiEndpoint = config["OpenAIEndpoint"];
        if (!string.IsNullOrEmpty(openaiEndpoint))
        {
            if (!string.IsNullOrEmpty(azureOpenAiEndpointValue))
            {
                throw new ArgumentException("Only one of 'AzureOpenAIEndpoint' or 'OpenAIEndpoint' can be specified. Check your configuration and try again.");
            }

            Throws.IfNullOrWhiteSpace(config["OpenAIModelId"]);

            if (string.IsNullOrWhiteSpace(config["OpenAIKey"]))
            {
                if (!openaiEndpoint.Contains("localhost", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("Missing 'OpenAIKey'");
                }
            }
        }
    }

    public static HostApplicationBuilder AddSemanticKernel<TApi>(this HostApplicationBuilder b, Action<IServiceProvider, OpenAIPromptExecutionSettings>? configurePromptSettings = default, Action<IServiceProvider, IKernelBuilder>? configureKernel = default) => AddSemanticKernel(b, configurePromptSettings,
        (sp, kb) =>
        {
            var expert = (TApi)Activator.CreateInstance(typeof(TApi), new Configuration(new Dictionary<string, string>(),
                new Dictionary<string, string>() { { "X-TBA-Auth-Key", Throws.IfNullOrWhiteSpace(b.Configuration[Constants.Configuration.VariableNames.TBA_API_KEY], message: "Missing TBA_API_KEY environment variable") } },
                new Dictionary<string, string>()), sp.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(TApi).Name))!;
            kb.Plugins.AddFromObject(expert);

            configureKernel?.Invoke(sp, kb);
        });
}
