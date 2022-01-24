using BanjoBotAssets;
using BanjoBotAssets.Extensions;
using System.Reflection;

await Host.CreateDefaultBuilder(args)
#if DEBUG
    .UseEnvironment("Development")
#endif
    .UseContentRoot(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
    .ConfigureServices(services =>
    {
        services
            .AddHostedService<AssetExportService>()
            .AddHttpClient()
            .AddAesProviders()
            .AddGameFileProvider()
            .AddAssetExporters();
    })
    .RunConsoleAsync();

return Environment.ExitCode;
