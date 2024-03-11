/* Copyright 2023 Tara "Dino" Cassatt
 * 
 * This file is part of BanjoBotAssets.
 * 
 * BanjoBotAssets is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * BanjoBotAssets is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with BanjoBotAssets.  If not, see <http://www.gnu.org/licenses/>.
 */
using BanjoBotAssets.Aes;
using BanjoBotAssets.Artifacts;
using BanjoBotAssets.Config;
using BanjoBotAssets.Exporters;
using BanjoBotAssets.PostExporters;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.Objects.Core.Misc;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace BanjoBotAssets
{
    internal sealed partial class AssetExportService(
        ILogger<AssetExportService> logger,
            IHostApplicationLifetime lifetime,
            IEnumerable<IExporter> allExporters,
            IEnumerable<IAesProvider> aesProviders,
            IAesCacheUpdater aesCacheUpdater,
            IEnumerable<IExportArtifact> exportArtifacts,
            AbstractVfsFileProvider provider,
            ITypeMappingsProviderFactory typeMappingsProviderFactory,
            IOptions<ScopeOptions> scopeOptions,
            IEnumerable<IPostExporter> allPostExporters,
            LanguageProvider languageProvider) : BackgroundService
    {
        private readonly List<IExporter> exportersToRun = MakeExportersToRun(allExporters, scopeOptions);
        private readonly ConcurrentDictionary<string, byte> failedAssets = new();

        private static List<IExporter> MakeExportersToRun(IEnumerable<IExporter> allExporters, IOptions<ScopeOptions> scopeOptions)
        {
            var result = allExporters.ToList();

            if (!string.IsNullOrWhiteSpace(scopeOptions.Value.Only))
            {
                var wanted = scopeOptions.Value.Only.Split(',');
                result.RemoveAll(e => !wanted.Contains(e.GetType().Name, StringComparer.OrdinalIgnoreCase));
            }

            return result;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Environment.ExitCode = await RunAsync(cancellationToken);
            lifetime.StopApplication();
        }

        private sealed class CriticalFailureException : ApplicationException
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

        private readonly record struct AssetLoadingStats(int AssetsLoaded, TimeSpan Elapsed)
        {
            public static AssetLoadingStats operator +(AssetLoadingStats a, AssetLoadingStats b)
            {
                return new(a.AssetsLoaded + b.AssetsLoaded, a.Elapsed + b.Elapsed);
            }
        }

        private async Task<int> RunAsync(CancellationToken cancellationToken)
        {
            // by the time this method is called, the CUE4Parse file provider has already been created,
            // and the game files have been located but not decrypted. we need to supply the AES keys,
            // from cache or from an external API.
            await DecryptGameFilesAsync(cancellationToken);

            // load virtual paths
            LoadVirtualPaths();

            // load the type mappings CUE4Parse uses to parse UE structures
            await LoadMappingsAsync(cancellationToken);

            // load localized resources
            LoadLocalization(cancellationToken);

            // register the export classes used to expose UE structures as strongly-typed C# objects
            RegisterExportTypes();

            // feed the file list to each exporter so they can record the paths they're interested in
            OfferFileListToExporters();

            // run exporters and collect their intermediate results
            var (exportedAssets, exportedRecipes, stats1) = await RunSelectedExportersAsync(cancellationToken);

            // run post-exporters to refine the intermediate results
            var stats2 = await RunSelectedPostExportersAsync(exportedAssets, exportedRecipes, cancellationToken);

            // report assets loaded and time elapsed
            ReportAssetLoadingStats(stats1 + stats2);

            // generate output artifacts
            await GenerateSelectedArtifactsAsync(exportedAssets, exportedRecipes, cancellationToken);

            // report cache stats
            (provider as CachingFileProvider)?.ReportCacheStats();

            // report any export failures
            ReportFailedAssets();

            // done!
            return 0;
        }

        private void ReportAssetLoadingStats(AssetLoadingStats stats)
        {
            logger.LogInformation(Resources.Status_LoadedAssets, stats.AssetsLoaded, stats.Elapsed, stats.Elapsed.TotalMilliseconds / Math.Max(stats.AssetsLoaded, 1));
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

            if (aes.MainKey != null)
            {
                logger.LogDebug(Resources.Status_SubmittingMainKey);
                provider.SubmitKey(new FGuid(), new FAesKey(aes.MainKey));
            }
            else
            {
                logger.LogDebug(Resources.Status_SkippingNullMainKey);
            }

            foreach (var dk in aes.DynamicKeys)
            {
                logger.LogDebug(Resources.Status_SubmittingDynamicKey, dk.PakFilename);
                provider.SubmitKey(new FGuid(dk.PakGuid), new FAesKey(dk.Key));
            }
        }

        private void LoadVirtualPaths()
        {
            var numPaths = provider.LoadVirtualPaths();
            logger.LogInformation(Resources.Status_LoadedVirtualPaths, numPaths);
        }

        private async Task LoadMappingsAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation(Resources.Status_LoadingMappings);

            if (provider.InternalGameName.Equals("FortniteGame", StringComparison.OrdinalIgnoreCase))
            {
                provider.MappingsContainer = typeMappingsProviderFactory.Create();
            }

            // sometimes the mappings don't load, and then nothing works
            if (provider.MappingsForGame is null or { Enums.Count: 0, Types.Count: 0 })
            {
                await Task.Delay(5 * 1000, cancellationToken);
                logger.LogWarning(Resources.Status_RetryingMappings);
                provider.MappingsContainer = typeMappingsProviderFactory.Create();

                if (provider.MappingsForGame is null or { Enums.Count: 0, Types.Count: 0 })
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
            ObjectTypeRegistry.RegisterClass("StWFortAccoladeItemDefinition", typeof(UFortItemDefinition));
            ObjectTypeRegistry.RegisterClass("FortQuestItemDefinition_Campaign", typeof(UFortQuestItemDefinition));
        }

        private async Task GenerateSelectedArtifactsAsync(ExportedAssets exportedAssets, IList<ExportedRecipe> exportedRecipes, CancellationToken cancellationToken)
        {
            foreach (var art in exportArtifacts)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await art.RunAsync(exportedAssets, exportedRecipes, cancellationToken);
            }
        }

        private async Task<AssetLoadingStats> RunSelectedPostExportersAsync(ExportedAssets exportedAssets, IList<ExportedRecipe> exportedRecipes, CancellationToken cancellationToken)
        {
            var stopwatch = new Stopwatch();

            var progress = new Progress<ExportProgress>(HandleProgressReport);
            var postExporters = allPostExporters.ToList();

            stopwatch.Start();

            await Task.WhenAll(postExporters.Select(pe => pe.ProcessExportsAsync(exportedAssets, exportedRecipes, cancellationToken)));

            stopwatch.Stop();

            return new AssetLoadingStats(postExporters.Sum(pe => pe.AssetsLoaded), stopwatch.Elapsed);
        }

        private async Task<(ExportedAssets, IList<ExportedRecipe>, AssetLoadingStats)> RunSelectedExportersAsync(CancellationToken cancellationToken)
        {
            // run the exporters and collect their outputs
            var stopwatch = new Stopwatch();

            // give each exporter its own output object to use,
            // we'll combine the results when the tasks all complete.
            var allPrivateExports = new List<IAssetOutput>(exportersToRun.Select(_ => new AssetOutput()));

            // run the exporters!
            if (!string.IsNullOrWhiteSpace(scopeOptions.Value.Only))
            {
                logger.LogInformation(Resources.Status_RunningSelectedExporters, exportersToRun.Count, string.Join(", ", exportersToRun.Select(t => t.GetType().Name)));
            }
            else
            {
                logger.LogInformation(Resources.Status_RunningAllExporters);
            }

            var progress = new Progress<ExportProgress>(HandleProgressReport);

            stopwatch.Start();

            await Task.WhenAll(
                exportersToRun.Zip(allPrivateExports, (e, r) => e.ExportAssetsAsync(progress, r, cancellationToken)));

            stopwatch.Stop();

            var exportedAssets = new ExportedAssets();
            var exportedRecipes = new List<ExportedRecipe>();
            var assetsLoaded = exportersToRun.Sum(e => e.AssetsLoaded);

            // combine intermediate outputs
            foreach (var privateExport in allPrivateExports)
            {
                cancellationToken.ThrowIfCancellationRequested();
                privateExport.CopyTo(exportedAssets, exportedRecipes, cancellationToken);
            }

            foreach (var privateExport in allPrivateExports)
            {
                cancellationToken.ThrowIfCancellationRequested();
                privateExport.ApplyDisplayNameCorrections(exportedAssets);
            }

            allPrivateExports.Clear();
            return (exportedAssets, exportedRecipes, new AssetLoadingStats { AssetsLoaded = assetsLoaded, Elapsed = stopwatch.Elapsed });
        }

        private void HandleProgressReport(ExportProgress progress)
        {
            if (progress.FailedAssets?.Any() == true)
            {
                foreach (var i in progress.FailedAssets)
                {
                    failedAssets.TryAdd(i, 0);
                }
            }

            // TODO: do something more with progress reports
        }

        private void ReportFailedAssets()
        {
            if (!failedAssets.IsEmpty)
            {
                logger.LogError(Resources.Error_FinishedWithFailedAssets, failedAssets.Count);

                foreach (var i in failedAssets.Keys.OrderBy(i => i))
                {
                    logger.LogError(Resources.Error_FailedAsset, i);
                }
            }
        }

        [GeneratedRegex(@"/Athena/|\.[^.]+(?<!\.uasset|\.bin)$|\.o\.[^.]+$", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex ExcludedAssetPathRegex();

        private void OfferFileListToExporters()
        {
            logger.LogInformation(Resources.Status_AnalyzingFileList);

            foreach (var (name, file) in provider.Files)
            {
                if (ExcludedAssetPathRegex().IsMatch(name))
                {
                    continue;
                }

                foreach (var e in exportersToRun)
                {
                    e.ObserveAsset(name);
                }
            }
        }

        private void LoadLocalization(CancellationToken cancellationToken)
        {
            logger.LogInformation(Resources.Status_LoadingLocalization, languageProvider.Language.ToString());
            provider.LoadLocalization(languageProvider.Language, cancellationToken);
        }
    }
}
