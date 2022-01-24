using BanjoBotAssets;
using BanjoBotAssets.Exporters;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Versions;
using Microsoft.Extensions.Options;
using System.Reflection;

await Host.CreateDefaultBuilder(args)
    .UseContentRoot(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
    .ConfigureLogging(logging =>
    {
    })
    .ConfigureServices((hostContext, services) =>
    {
        services
            .AddHostedService<AssetExportService>()
            .AddHttpClient()
            .AddSingleton((Func<IServiceProvider, AbstractVfsFileProvider>)(sp =>
            {
                var options = sp.GetRequiredService<IOptions<GameFileOptions>>();
                var gameDirectory = options.Value.GameDirectories.First(Directory.Exists);
                var provider = new DefaultFileProvider(
                    gameDirectory,
                    SearchOption.TopDirectoryOnly,
                    isCaseInsensitive: false,
                    new VersionContainer(EGame.GAME_UE5_LATEST));
                provider.Initialize();
                return provider;
            }))
            .AddTransient(sp =>
            {
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                return loggerFactory.CreateLogger("Uncategorized");
            })
            .AddDerivedServices<IExporter, BaseExporter>(ServiceLifetime.Transient)
            .AddTransient<IExportFinalizer, AssetsJsonFinalizer>()
            .AddTransient<IExportFinalizer, SchematicsJsonFinalizer>();

        services.AddOptions<GameFileOptions>()
            .Bind(hostContext.Configuration.GetRequiredSection(nameof(GameFileOptions)));

        services.AddOptions<ExportedFileOptions<AssetsJsonFinalizer>>("ExportedAssets")
            .Bind(hostContext.Configuration.GetRequiredSection("ExportedAssets"));

        services.AddOptions<ExportedFileOptions<SchematicsJsonFinalizer>>("ExportedSchematics")
            .Bind(hostContext.Configuration.GetRequiredSection("ExportedSchematics"));
    })
    .RunConsoleAsync();

return Environment.ExitCode;
