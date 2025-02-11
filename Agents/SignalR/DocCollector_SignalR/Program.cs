namespace DocCollector_SignalR;
using Common.Extensions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;

internal partial class Program
{
    private static async Task Main(string[] args)
    {
        CancellationTokenSource cts = ProgramHelpers.CreateCancellationTokenSource();

        HostApplicationBuilder b = Host.CreateApplicationBuilder(args);
        b.AddExpert<Agent>()
            .AddSemanticKernel(configureKernelBuilder: (sp, kb) => kb.Plugins.AddFromObject(sp.GetRequiredService<Api>()))
        .Services
            .AddSingleton<Api>()
            .AddHttpLogging(o => o.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All);

        await b.Build().RunAsync(cts.Token).ConfigureAwait(false);
    }
}