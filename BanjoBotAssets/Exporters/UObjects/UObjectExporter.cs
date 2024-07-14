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
using CUE4Parse.FN.Enums.FortniteGame;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace BanjoBotAssets.Exporters.UObjects
{
    internal abstract class UObjectExporter(IExporterContext services) : UObjectExporter<UObject>(services)
    {
    }

    internal abstract class UObjectExporter<TAsset>(IExporterContext services) : UObjectExporter<TAsset, NamedItemData>(services)
        where TAsset : UObject
    {
    }


    internal abstract class UObjectExporter<TAsset, TItemData>(IExporterContext services) : BaseExporter(services)
        where TAsset : UObject
        where TItemData : NamedItemData, new()
    {
        private int numToProcess, processedSoFar;
        private readonly ConcurrentDictionary<string, byte> failedAssets = new();

        protected abstract string Type { get; }

        protected virtual bool IgnoreLoadFailures => false;

        protected virtual Task<bool> ExportAssetAsync(TAsset asset, TItemData itemData, Dictionary<ImageType, string> imagePaths)
        {
            // by default, just export it as-is
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

            Report(progress, string.Format(CultureInfo.CurrentCulture, FormatStrings.ExportingGroup, Type));

            var assetsToProcess = scopeOptions.Value.Limit != null ? assetPaths.Take((int)scopeOptions.Value.Limit) : assetPaths;
            var opts = new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = performanceOptions.Value.MaxParallelism };

            await Parallel.ForEachAsync(assetsToProcess, opts, async (path, _) =>
            {
                try
                {
                    var file = provider[path];

                    var num = Interlocked.Increment(ref processedSoFar);
                    logger.LogInformation(Resources.Status_ProcessingTypeNumOfNum, Type, num, numToProcess);

                    //logger.LogInformation("Loading {0}", file.PathWithoutExtension);
                    Interlocked.Increment(ref assetsLoaded);

                    Report(progress, file.PathWithoutExtension);

                    TAsset? uobject;
                    if (IgnoreLoadFailures)
                    {
                        var pkg = await provider.TryLoadPackageAsync(file);

                        cancellationToken.ThrowIfCancellationRequested();

                        if (pkg?.GetExportOrNull(file.NameWithoutExtension, StringComparison.OrdinalIgnoreCase) is TAsset asset)
                        {
                            uobject = asset;
                        }
                        else if (pkg?.GetExportOrNull(file.NameWithoutExtension + "_C", StringComparison.OrdinalIgnoreCase) is TAsset assetC)
                        {
                            uobject = assetC;
                        }
                        else
                        {
                            // ignore
                            return;
                        }
                    }
                    else
                    {
                        try
                        {
                            var pkg = await provider.LoadPackageAsync(file);
                            cancellationToken.ThrowIfCancellationRequested();
                            
                            uobject = pkg.GetExportOrNull(file.NameWithoutExtension, StringComparison.OrdinalIgnoreCase) as TAsset ??
                                pkg.GetExport(file.NameWithoutExtension + "_C", StringComparison.OrdinalIgnoreCase) as TAsset;
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, Resources.Warning_FailedToLoadFile, file.PathWithoutExtension);
                            failedAssets.TryAdd(file.PathWithoutExtension, 0);
                            return;
                        }
                    }

                    if (uobject == null)
                    {
                        logger.LogError(Resources.Warning_FailedToLoadFile, file.PathWithoutExtension);
                        failedAssets.TryAdd(file.PathWithoutExtension, 0);
                        return;
                    }

                    var templateId = $"{Type}:{uobject.Name}";
                    var displayName = uobject.GetOrDefault<FText>("ItemName")?.Text ?? uobject.GetOrDefault<FText>("DisplayName")?.Text ?? $"<{uobject.Name}>";
                    var description = uobject.GetOrDefault<FText>("ItemDescription")?.Text ?? uobject.GetOrDefault<FText>("Description")?.Text;
                    var isInventoryLimitExempt = !uobject.GetOrDefault("bInventorySizeLimited", true);

                    var itemData = new TItemData
                    {
                        AssetPath = provider.FixPath(path),
                        Name = uobject.Name,
                        Type = Type,
                        DisplayName = displayName.Trim(),
                        Description = description,
                        IsInventoryLimitExempt = isInventoryLimitExempt,
                    };
                    
                    if (uobject.GetOrDefaultFromDataList<EFortItemTier>("Tier") is EFortItemTier tier && tier != default)
                    {
                        itemData.Tier = (int)tier;
                    }

                    if (uobject.GetOrDefault("Rarity", EFortRarity.Uncommon) is EFortRarity rarity && rarity != EFortRarity.Uncommon)
                    {
                        itemData.Rarity = rarity.GetNameText().Text;
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    var imagePaths = new Dictionary<ImageType, string>();

                    if (uobject.GetSoftAssetPathFromDataList("Icon") is string smallPreviewPath)
                        imagePaths.Add(ImageType.SmallPreview, smallPreviewPath);

                    if (uobject.GetSoftAssetPathFromDataList("LargeIcon") is string largePreviewPath)
                        imagePaths.Add(ImageType.LargePreview, largePreviewPath);

                    if (!await ExportAssetAsync(uobject, itemData, imagePaths))
                    {
                        return;
                    }

                    output.AddNamedItem(templateId, itemData);

                    foreach (var (t, p) in imagePaths)
                    {
                        output.AddImageForNamedItem(templateId, t, p);
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
