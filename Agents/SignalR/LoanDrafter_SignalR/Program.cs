namespace LoanDrafter_SignalR;
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
        b.Services.AddSingleton<Api>();
        b.AddExpert<Agent>();
        b.AddSemanticKernel(configureKernelBuilder: (sp, kb) => kb.Plugins.AddFromObject(sp.GetRequiredService<Api>()));

        await b.Build().RunAsync(cts.Token).ConfigureAwait(false);
    }
}