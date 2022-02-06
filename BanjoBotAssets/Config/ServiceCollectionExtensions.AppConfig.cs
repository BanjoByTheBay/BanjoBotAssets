using BanjoBotAssets.Aes;
using BanjoBotAssets.Artifacts;
using BanjoBotAssets.Config;
using BanjoBotAssets.Exporters;
using BanjoBotAssets.Exporters.Helpers;
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
                    options.LocalFilePath = "aes.json";
                    config.GetSection(nameof(AesOptions)).Bind(options);
                });

            return services;
        }

        public static IServiceCollection AddAssetExporters(this IServiceCollection services)
        {
            // all IExporter implementations derived from BaseExporter,
            // and their service aggregator and helpers
            services
                .AddDerivedServices<IExporter, BaseExporter>(ServiceLifetime.Transient)
                .AddTransient<IExporterContext, ExporterContext>()
                .AddTransient<AbilityDescription>();

            // performance options affecting the exporters
            services
                .AddOptions<PerformanceOptions>()
                .Configure<IConfiguration>((options, config) =>
                {
                    options.MaxParallelism = 1;
                    config.GetRequiredSection("PerformanceOptions").Bind(options);
                });

            // all artifacts use global Merge option by default
            services
                .ConfigureAll<ExportedFileOptions, IOptions<ScopeOptions>>(
                        (options, scopeOptions) => options.Merge = scopeOptions.Value.Merge);

            // artifact generator for assets.json and its options
            services
                .AddTransientWithNamedOptions<IExportArtifact, AssetsJsonArtifact, ExportedFileOptions>()
                .Configure<IConfiguration, IOptions<ScopeOptions>>((options, config, scopeOptions) =>
                {
                    options.Path = Resources.File_assets_json;
                    options.Merge = scopeOptions.Value.Merge;
                    config.GetRequiredSection("ExportedAssets").Bind(options);
                });

            // artifact generator for schematics.json and its options
            services
                .AddTransientWithNamedOptions<IExportArtifact, SchematicsJsonArtifact, ExportedFileOptions>()
                .Configure<IConfiguration, IOptions<ScopeOptions>>((options, config, scopeOptions) =>
                {
                    options.Path = Resources.File_schematics_json;
                    options.Merge = scopeOptions.Value.Merge;
                    config.GetRequiredSection("ExportedSchematics").Bind(options);
                });

            return services;
        }

        public static IServiceCollection ConfigureAll<TOptions, TDep1>(this IServiceCollection services, Action<TOptions, TDep1> configureOptions)
            where TOptions : class
            where TDep1 : class
        {
            return services
                .AddTransient<IConfigureOptions<TOptions>>(sp =>
                    new ConfigureNamedOptions<TOptions, TDep1>(
                        name: null,
                        sp.GetRequiredService<TDep1>(),
                        configureOptions));
        }

        public static OptionsBuilder<TOptions> AddTransientWithNamedOptions<TService, TImplementation, TOptions>(this IServiceCollection services)
            where TService : class
            where TImplementation : TService
            where TOptions : class
        {
            return services
                .AddTransient<TService>(sp =>
                {
                    using var scope = sp.CreateScope();
                    var opts = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<TOptions>>();
                    return ActivatorUtilities.CreateInstance<TImplementation>(sp, opts.Get(typeof(TImplementation).FullName));
                })
                .AddOptions<TOptions>(typeof(TImplementation).FullName);
        }

        public static IServiceCollection AddGameFileProvider(this IServiceCollection services)
        {
            services.AddSingleton((Func<IServiceProvider, AbstractVfsFileProvider>)(sp =>
                 {
                     var options = sp.GetRequiredService<IOptions<GameFileOptions>>();
                     string gameDirectory;
                     try
                     {
                         gameDirectory = options.Value.GameDirectories.First(Directory.Exists);
                     }
                     catch (InvalidOperationException ex)
                     {
                         throw new InvalidOperationException(Resources.Error_GameNotFound, ex);
                     }
                     var provider = new DefaultFileProvider(
                         gameDirectory,
                         SearchOption.TopDirectoryOnly,
                         isCaseInsensitive: false,
                         new VersionContainer(EGame.GAME_UE5_LATEST));
                     provider.Initialize();
                     return provider;
                 }));

            services.AddOptions<GameFileOptions>()
                .Configure<IConfiguration>((options, config) =>
                {
                    options.ELanguage = "";
                    options.GameDirectories = Array.Empty<string>();
                    config.GetSection(nameof(GameFileOptions)).Bind(options);
                });

            return services;
        }

        public static IServiceCollection AddMappingsProviders(this IServiceCollection services)
        {
            services
                .AddTransient<ITypeMappingsProviderFactory, CachingBenBotMappingsProviderFactory>();

            services.AddOptions<MappingsOptions>()
                .Configure<IConfiguration>((options, config) =>
                {
                    options.MappingsApiUri = "https://benbot.app/api/v1/mappings";
                    options.LocalFilePath = "mappings.usmap";
                    config.GetSection(nameof(MappingsOptions)).Bind(options);
                });

            return services;
        }
    }
}