using BanjoBotAssets.Artifacts.Models;
using BanjoBotAssets.Exporters.Helpers;
using BanjoBotAssets.Extensions;
using CUE4Parse.FN.Enums.FortniteGame;

namespace BanjoBotAssets.Exporters.UObjects
{
    internal abstract class UObjectExporter : UObjectExporter<UObject>
    {
        protected UObjectExporter(IExporterContext services) : base(services) { }
    }

    internal abstract class UObjectExporter<TAsset> : UObjectExporter<TAsset, NamedItemData>
        where TAsset : UObject
    {
        protected UObjectExporter(IExporterContext services) : base(services)
        {
        }
    }

    internal abstract class UObjectExporter<TAsset, TItemData> : BaseExporter
        where TAsset : UObject
        where TItemData : NamedItemData, new()
    {
        protected UObjectExporter(IExporterContext services) : base(services) { }

        protected abstract string Type { get; }

        protected virtual bool IgnoreLoadFailures => false;

        protected virtual Task<bool> ExportAssetAsync(TAsset asset, TItemData itemData, Dictionary<ImageType, string> imagePaths)
        {
            return Task.FromResult(true);
        }

        public override Task ExportAssetsAsync(IProgress<ExportProgress> progress, IAssetOutput output, CancellationToken cancellationToken)
        {
            var numToProcess = assetPaths.Count;
            var processedSoFar = 0;

            var opts = new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = performanceOptions.Value.MaxParallelism };

            return Parallel.ForEachAsync(scopeOptions.Value.Limit != null ? assetPaths.Take((int)scopeOptions.Value.Limit) : assetPaths, opts, async (path, _) =>
            {
                try
                {
                    var file = provider[path];

                    var num = Interlocked.Increment(ref processedSoFar);
                    logger.LogInformation(Resources.Status_ProcessingTypeNumOfNum, Type, num, numToProcess);

                    //logger.LogInformation("Loading {0}", file.PathWithoutExtension);
                    Interlocked.Increment(ref assetsLoaded);

                    TAsset? uobject;
                    if (IgnoreLoadFailures)
                    {
                        var pkg = await provider.TryLoadPackageAsync(file);

                        cancellationToken.ThrowIfCancellationRequested();

                        if (pkg?.GetExport(0) is TAsset asset)
                        {
                            uobject = asset;
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
                            uobject = pkg.GetExport(0) as TAsset;
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, Resources.Warning_FailedToLoadFile, file.PathWithoutExtension);
                            return;
                        }
                    }

                    if (uobject == null)
                    {
                        logger.LogError(Resources.Warning_FailedToLoadFile, file.PathWithoutExtension);
                        return;
                    }

                    var templateId = $"{Type}:{uobject.Name}";
                    var displayName = uobject.GetOrDefault<FText>("DisplayName")?.Text ?? $"<{uobject.Name}>";
                    var description = uobject.GetOrDefault<FText>("Description")?.Text;

                    var itemData = new TItemData
                    {
                        AssetPath = provider.FixPath(path),
                        Name = uobject.Name,
                        Type = Type,
                        DisplayName = displayName.Trim(),
                        Description = description,
                    };

                    if (uobject.GetOrDefault<EFortItemTier>("Tier") is EFortItemTier tier && tier != default)
                    {
                        itemData.Tier = (int)tier;
                    }

                    if (uobject.GetOrDefault<EFortRarity>("Rarity") is EFortRarity rarity && rarity != default)
                    {
                        itemData.Rarity = rarity.GetNameText().Text;
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    var imagePaths = new Dictionary<ImageType, string>();

                    if (uobject.GetSoftAssetPath("SmallPreviewImage") is string smallPreviewPath)
                        imagePaths.Add(ImageType.SmallPreview, smallPreviewPath);

                    if (uobject.GetSoftAssetPath("LargePreviewImage") is string largePreviewPath)
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
                }
            });
        }
    }
}
