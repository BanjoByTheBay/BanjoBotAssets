using BanjoBotAssets.Config;
using BanjoBotAssets.Extensions;
using System.Reflection;

// TODO: export per-difficulty stat clamp tables (GameDifficultyGrowthBounds, CombatStatClampsPerTheater)
// TODO: export collection book categories and recruitment/research/voucher options (CollectionBookSlots)

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
