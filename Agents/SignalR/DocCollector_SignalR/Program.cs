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
        b.AddExpert<Agent>();
        b.AddSemanticKernel();

        var h = b.Build();
        var sp = h.Services;
        var kernel = sp.GetRequiredService<Kernel>();
        kernel.Plugins.AddFromObject(new Api(kernel, sp.GetRequiredService<PromptExecutionSettings>()));

        await h.RunAsync(cts.Token).ConfigureAwait(false);
    }
}