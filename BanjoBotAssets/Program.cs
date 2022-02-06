using BanjoBotAssets.Config;
using BanjoBotAssets.Extensions;
using System.Reflection;

// TODO: export Ventures reward/level tables

await Host.CreateDefaultBuilder(args)
#if DEBUG
    .UseEnvironment("Development")
#endif
    .UseContentRoot(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
    .ConfigureLogging(logging =>
    {
        logging
            .ClearProviders()
            .AddSimpleConsole(console => console.SingleLine = true);
    })
    .ConfigureServices(services =>
    {
        services
            .AddHostedService<AssetExportService>()
            .AddHttpClient()
            .AddAesProviders()
            .AddMappingsProviders()
            .AddGameFileProvider()
            .AddAssetExporters();

        services
            .AddOptions<ScopeOptions>()
            .Configure<IConfiguration>((scope, config) => config.Bind(scope));
    })
    .RunConsoleAsync(o => o.SuppressStatusMessages = true);

return Environment.ExitCode;
