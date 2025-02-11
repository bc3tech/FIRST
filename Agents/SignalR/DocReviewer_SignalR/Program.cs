namespace DocReviewer_SignalR;
using Common.Extensions;

using Microsoft.Extensions.Hosting;

internal partial class Program
{
    private static async Task Main(string[] args)
    {
        CancellationTokenSource cts = ProgramHelpers.CreateCancellationTokenSource();

        HostApplicationBuilder b = Host.CreateApplicationBuilder(args);
        _ = b.AddExpert<Agent>();
        _ = b.AddSemanticKernel();

        await b.Build().RunAsync(cts.Token).ConfigureAwait(false);
    }
}