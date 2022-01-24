using BanjoBotAssets.Aes;
using BanjoBotAssets.Exporters;
using BanjoBotAssets.Exporters.Artifacts;
using BanjoBotAssets.Exporters.Helpers;
using BanjoBotAssets.Exporters.Options;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Versions;
using Microsoft.Extensions.Options;

namespace BanjoBotAssets.Extensions
{
    internal static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAesProviders(this IServiceCollection services)
        {
            services
                .AddTransient<IAesProvider, FileAesProvider>()
                .AddTransient<IAesProvider, FortniteApiAesProvider>()
                .AddTransient<IAesCacheUpdater, FileAesProvider>();

            services
                .AddOptions<AesOptions>()
                .Configure<IConfiguration>((options, config) =>
                {
                    options.AesApiUri = "https://fortnite-api.com/v2/aes";
                    config.GetRequiredSection(nameof(AesOptions)).Bind(options);
                });

            return services;
        }

        public static IServiceCollection AddAssetExporters(this IServiceCollection services)
        {
            // all IExporter implementations derived from BaseExporter, and their service aggregator
            services
                .AddDerivedServices<IExporter, BaseExporter>(ServiceLifetime.Transient)
                .AddTransient<IExporterContext, ExporterContext>();

            // performance options affecting the exporters
            services
                .AddOptions<PerformanceOptions>()
                .Configure<IConfiguration>((options, config) =>
                {
                    options.MaxParallelism = 1;
                    config.GetRequiredSection("PerformanceOptions").Bind(options);
                });

            // artifact generator for assets.json and its options
            services
                .AddTransient<IExportArtifact, AssetsJsonArtifact>()
                .AddOptions<ExportedFileOptions<AssetsJsonArtifact>>()
                .Configure<IConfiguration>((options, config) =>
                {
                    options.Path = Resources.File_assets_json;
                    config.GetRequiredSection("ExportedAssets").Bind(options);
                });

            // artifact generator for schematics.json and its options
            services
                .AddTransient<IExportArtifact, SchematicsJsonArtifact>()
                .AddOptions<ExportedFileOptions<SchematicsJsonArtifact>>()
                .Configure<IConfiguration>((options, config) =>
                {
                    options.Path = Resources.File_schematics_json;
                    config.GetRequiredSection("ExportedSchematics").Bind(options);
                });

            return services;
        }

        public static IServiceCollection AddGameFileProvider(this IServiceCollection services)
        {
            services.AddSingleton((Func<IServiceProvider, AbstractVfsFileProvider>)(sp =>
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
                 }));

            services.AddOptions<GameFileOptions>()
                .Configure<IConfiguration>((options, config) => config.GetSection(nameof(GameFileOptions)).Bind(options));

            return services;
        }
    }
}