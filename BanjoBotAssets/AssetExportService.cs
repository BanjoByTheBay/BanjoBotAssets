using BanjoBotAssets.Exporters;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using BanjoBotAssets.Aes;
using BanjoBotAssets.Exporters.Options;
using BanjoBotAssets.Exporters.Helpers;
using BanjoBotAssets.Artifacts;
using BanjoBotAssets.Config;

namespace BanjoBotAssets
{
    internal sealed class AssetExportService : BackgroundService
    {
        private readonly ILogger<AssetExportService> logger;
        private readonly IHostApplicationLifetime lifetime;
        private readonly IEnumerable<IExporter> exporters;
        private readonly IOptions<GameFileOptions> options;
        private readonly IEnumerable<IAesProvider> aesProviders;
        private readonly IAesCacheUpdater aesCacheUpdater;
        private readonly IEnumerable<IExportArtifact> exportArtifacts;
        private readonly AbstractVfsFileProvider provider;
        private readonly ITypeMappingsProviderFactory typeMappingsProviderFactory;
        private readonly IOptions<ScopeOptions> scopeOptions;

        public AssetExportService(ILogger<AssetExportService> logger,
            IHostApplicationLifetime lifetime,
            IEnumerable<IExporter> exporters,
            IOptions<GameFileOptions> options,
            IEnumerable<IAesProvider> aesProviders,
            IAesCacheUpdater aesCacheUpdater,
            IEnumerable<IExportArtifact> exportArtifacts,
            AbstractVfsFileProvider provider,
            ITypeMappingsProviderFactory typeMappingsProviderFactory,
            IOptions<ScopeOptions> scopeOptions)
        {
            this.logger = logger;
            this.lifetime = lifetime;
            this.exporters = exporters;
            this.options = options;
            this.aesProviders = aesProviders;
            this.aesCacheUpdater = aesCacheUpdater;
            this.exportArtifacts = exportArtifacts;
            this.provider = provider;
            this.typeMappingsProviderFactory = typeMappingsProviderFactory;
            this.scopeOptions = scopeOptions;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Environment.ExitCode = await RunAsync(cancellationToken);
            lifetime.StopApplication();
        }

        private class CriticalFailureException: ApplicationException
        {
            public CriticalFailureException()
            {
            }

            public CriticalFailureException(string? message) : base(message)
            {
            }

            public CriticalFailureException(string? message, Exception? innerException) : base(message, innerException)
            {
            }
        }

        private async Task<int> RunAsync(CancellationToken cancellationToken)
        {
            // by the time this method is called, the CUE4Parse file provider has already been created,
            // and the game files have been located but not decrypted. we need to supply the AES keys,
            // from cache or from an external API.
            await DecryptGameFilesAsync(cancellationToken);

            // load the type mappings CUE4Parse uses to parse UE structures
            await LoadMappingsAsync(cancellationToken);

            // load localized resources
            LoadLocalization(cancellationToken);

            // register the export classes used to expose UE structures as strongly-typed C# objects
            RegisterExportTypes();

            // feed the file list to each exporter so they can record the paths they're interested in
            OfferFileListToExporters();

            // run exporters and collect their intermediate results
            var (exportedAssets, exportedRecipes) = await RunSelectedExportersAsync(cancellationToken);

            // generate output artifacts
            await GenerateSelectedArtifactsAsync(exportedAssets, exportedRecipes, cancellationToken);

            // done!
            return 0;
        }

        private async Task DecryptGameFilesAsync(CancellationToken cancellationToken)
        {
            // get the keys from cache or a web service
            AesApiResponse? aes = null;

            foreach (var ap in aesProviders)
            {
                if (await ap.TryGetAesAsync(cancellationToken) is { } good)
                {
                    aes = good;
                    await aesCacheUpdater.UpdateAesCacheAsync(aes, cancellationToken);
                    break;
                }
            }

            if (aes == null)
                throw new CriticalFailureException(Resources.Error_AesFetchFailed);

            // offer them to CUE4Parse
            logger.LogInformation(Resources.Status_DecryptingGameFiles);

            logger.LogDebug(Resources.Status_SubmittingMainKey);
            provider.SubmitKey(new FGuid(), new FAesKey(aes.Data.MainKey));

            foreach (var dk in aes.Data.DynamicKeys)
            {
                logger.LogDebug(Resources.Status_SubmittingDynamicKey, dk.PakFilename);
                provider.SubmitKey(new FGuid(dk.PakGuid), new FAesKey(dk.Key));
            }
        }

        private async Task LoadMappingsAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation(Resources.Status_LoadingMappings);

            if (provider.GameName.Equals("FortniteGame", StringComparison.OrdinalIgnoreCase))
            {
                provider.MappingsContainer = typeMappingsProviderFactory.Create("fortnitegame");
            }

            // sometimes the mappings don't load, and then nothing works
            // TODO: cache mappings locally
            if (provider.MappingsForThisGame is null or { Enums.Count: 0, Types.Count: 0 })
            {
                await Task.Delay(5 * 1000, cancellationToken);
                logger.LogWarning(Resources.Status_RetryingMappings);
                provider.LoadMappings();

                if (provider.MappingsForThisGame is null or { Enums.Count: 0, Types.Count: 0 })
                    throw new CriticalFailureException(Resources.Error_MappingsFetchFailed);
            }
        }

        private void RegisterExportTypes()
        {
            logger.LogInformation(Resources.Status_RegisteringExportTypes);
            ObjectTypeRegistry.RegisterEngine(typeof(UFortItemDefinition).Assembly);
            ObjectTypeRegistry.RegisterClass("FortDefenderItemDefinition", typeof(UFortHeroType));
            ObjectTypeRegistry.RegisterClass("FortTrapItemDefinition", typeof(UFortItemDefinition));
            ObjectTypeRegistry.RegisterClass("FortAlterationItemDefinition", typeof(UFortItemDefinition));
            ObjectTypeRegistry.RegisterClass("FortResourceItemDefinition", typeof(UFortWorldItemDefinition));
            ObjectTypeRegistry.RegisterClass("FortGameplayModifierItemDefinition", typeof(UFortItemDefinition));
        }

        private async Task GenerateSelectedArtifactsAsync(ExportedAssets exportedAssets, IList<ExportedRecipe> exportedRecipes, CancellationToken cancellationToken)
        {
            foreach (var art in exportArtifacts)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await art.RunAsync(exportedAssets, exportedRecipes, cancellationToken);
            }
        }

        private async Task<(ExportedAssets, IList<ExportedRecipe>)> RunSelectedExportersAsync(CancellationToken cancellationToken)
        {
            // run the exporters and collect their outputs
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // give each exporter its own output object to use,
            // we'll combine the results when the tasks all complete.
            var allPrivateExports = new List<IAssetOutput>(exporters.Select(_ => new AssetOutput()));

            // run the exporters!
            IEnumerable<IExporter> exportersToRun = exporters;
            if (!string.IsNullOrWhiteSpace(scopeOptions.Value.Only))
            {
                var wanted = scopeOptions.Value.Only.Split(',');
                exportersToRun = exportersToRun.Where(e => wanted.Contains(e.GetType().Name, StringComparer.OrdinalIgnoreCase));
            }

            var progress = new Progress<ExportProgress>(_ =>
            {
                // TODO: do something with progress reports
            });
            await Task.WhenAll(
                exportersToRun.Zip(allPrivateExports, (e, r) => e.ExportAssetsAsync(progress, r, cancellationToken)));

            stopwatch.Stop();

            var exportedAssets = new ExportedAssets();
            var exportedRecipes = new List<ExportedRecipe>();
            var assetsLoaded = exporters.Sum(e => e.AssetsLoaded);

            logger.LogInformation(Resources.Status_LoadedAssets, assetsLoaded, stopwatch.Elapsed, stopwatch.ElapsedMilliseconds / Math.Max(assetsLoaded, 1));

            foreach (var privateExport in allPrivateExports)
            {
                cancellationToken.ThrowIfCancellationRequested();
                privateExport.CopyTo(exportedAssets, exportedRecipes, cancellationToken);
            }

            allPrivateExports.Clear();
            return (exportedAssets, exportedRecipes);
        }

        private void OfferFileListToExporters()
        {
            foreach (var (name, file) in provider.Files)
            {
                if (name.Contains("/Athena/"))
                {
                    continue;
                }

                foreach (var e in exporters)
                {
                    e.ObserveAsset(name);
                }
            }
        }


        private void LoadLocalization(CancellationToken cancellationToken)
        {
            logger.LogInformation(Resources.Status_LoadingLocalization);
            provider.LoadLocalization(GetLocalizationLanguage(), cancellationToken);
        }



        private ELanguage GetLocalizationLanguage()
        {
            if (!string.IsNullOrEmpty(options.Value.ELanguage) && Enum.TryParse<ELanguage>(options.Value.ELanguage, out var result))
                return result;

            return Enum.Parse<ELanguage>(Resources.ELanguage);
        }
    }
}
