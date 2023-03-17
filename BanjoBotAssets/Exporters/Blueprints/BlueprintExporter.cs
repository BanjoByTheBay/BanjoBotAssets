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
using CUE4Parse.UE4.Objects.Engine;
using System.Collections.Concurrent;

namespace BanjoBotAssets.Exporters.Blueprints
{
    internal abstract class BlueprintExporter : BaseExporter
    {
        private int numToProcess, processedSoFar;
        private readonly ConcurrentDictionary<string, byte> failedAssets = new();

        protected BlueprintExporter(IExporterContext services) : base(services)
        {
        }

        protected abstract string Type { get; }
        protected abstract string DisplayNameProperty { get; }
        protected abstract string? DescriptionProperty { get; }

        protected virtual Task<bool> ExportAssetAsync(UBlueprintGeneratedClass bpClass, UObject classDefaultObject, NamedItemData namedItemData, Dictionary<ImageType, string> imagePaths)
        {
            return Task.FromResult(true);
        }

        private void Report(IProgress<ExportProgress> progress, string current)
        {
            progress.Report(new ExportProgress
            {
                TotalSteps = numToProcess,
                CompletedSteps = processedSoFar,
                AssetsLoaded = assetsLoaded,
                CurrentItem = current,
                FailedAssets = failedAssets.Keys,
            });
        }

        public override async Task ExportAssetsAsync(IProgress<ExportProgress> progress, IAssetOutput output, CancellationToken cancellationToken)
        {
            numToProcess = assetPaths.Count;
            processedSoFar = 0;

            Report(progress, string.Format(CultureInfo.CurrentCulture, Resources.FormatString_Status_ExportingGroup, Type));

            var assetsToProcess = scopeOptions.Value.Limit != null ? assetPaths.Take((int)scopeOptions.Value.Limit) : assetPaths;
            var opts = new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = performanceOptions.Value.MaxParallelism };

            await Parallel.ForEachAsync(assetsToProcess, opts, async (path, _) =>
            {
                try
                {
                    var file = provider![path];

                    var num = Interlocked.Increment(ref processedSoFar);
                    logger.LogInformation(Resources.Status_ProcessingTypeNumOfNum, Type, num, numToProcess);

                    //logger.LogInformation("Loading {0}", file.PathWithoutExtension);
                    Interlocked.Increment(ref assetsLoaded);

                    Report(progress, file.PathWithoutExtension);

                    var pkg = await provider.LoadPackageAsync(file.PathWithoutExtension);

                    if (pkg.GetExports().First() is not UBlueprintGeneratedClass bpClass)
                    {
                        logger.LogWarning(Resources.Warning_FailedToLoadFile, file.PathWithoutExtension);
                        failedAssets.TryAdd(file.PathWithoutExtension, 0);
                        return;
                    }

                    var bpClassPath = bpClass.GetPathName();

                    Interlocked.Increment(ref assetsLoaded);
                    var cdo = await bpClass.ClassDefaultObject.LoadAsync();

                    var displayName = (await cdo.GetInheritedOrDefaultAsync<FText>(DisplayNameProperty, this))?.Text ?? $"<{bpClass.Name}>";
                    var description = DescriptionProperty == null ? null : (await cdo.GetInheritedOrDefaultAsync<FText>(DescriptionProperty, this))?.Text;

                    var namedItemData = new NamedItemData
                    {
                        AssetPath = file.PathWithoutExtension,
                        Description = description,
                        DisplayName = displayName.Trim(),
                        Name = bpClass.Name,
                        Type = Type,
                    };

                    var imagePaths = new Dictionary<ImageType, string>();

                    if (!await ExportAssetAsync(bpClass, cdo, namedItemData, imagePaths))
                    {
                        return;
                    }

                    output.AddNamedItem(bpClassPath, namedItemData);

                    foreach (var (t, p) in imagePaths)
                    {
                        output.AddImageForNamedItem(bpClassPath, t, p);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, Resources.Error_ExceptionWhileProcessingAsset, path);
                    failedAssets.TryAdd(Path.ChangeExtension(path, null), 0);
                }
            });

            Report(progress, "");
            logger.LogInformation(Resources.Status_ExportedGroup, Type, assetsToProcess.Count(), failedAssets.Count);
        }
    }
}
