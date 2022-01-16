namespace BanjoBotAssets.Exporters
{
    internal abstract class UObjectExporter : UObjectExporter<UObject>
    {
        protected UObjectExporter(DefaultFileProvider provider) : base(provider) { }
    }

    internal abstract class UObjectExporter<TAsset> : UObjectExporter<TAsset, NamedItemData>
        where TAsset : UObject
    {
        protected UObjectExporter(DefaultFileProvider provider) : base(provider)
        {
        }
    }

    internal abstract class UObjectExporter<TAsset, TItemData> : BaseExporter
        where TAsset : UObject
        where TItemData : NamedItemData, new()
    {
        protected UObjectExporter(DefaultFileProvider provider) : base(provider) { }

        protected abstract string Type { get; }

        protected virtual bool IgnoreLoadFailures => false;

        protected virtual Task<bool> ExportAssetAsync(TAsset asset, TItemData itemData)
        {
            return Task.FromResult(true);
        }

        public override Task ExportAssetsAsync(IProgress<ExportProgress> progress, IAssetOutput output)
        {
            var numToProcess = assetPaths.Count;
            var processedSoFar = 0;

            return Parallel.ForEachAsync(assetPaths, async (path, _) =>
            {
                var file = provider[path];

                var num = Interlocked.Increment(ref processedSoFar);
                Console.WriteLine(Resources.Status_ProcessingTypeNumOfNum, Type, num, numToProcess);

                //Console.WriteLine("Loading {0}", file.PathWithoutExtension);
                Interlocked.Increment(ref assetsLoaded);

                TAsset? uobject;
                if (IgnoreLoadFailures)
                {
                    var pkg = await provider.TryLoadPackageAsync(file);
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
                    var pkg = await provider.LoadPackageAsync(file);
                    uobject = pkg.GetExport(0) as TAsset;
                }

                if (uobject == null)
                {
                    Console.WriteLine("WARNING: Failed to load {0}", file.PathWithoutExtension);
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

                if (!await ExportAssetAsync(uobject, itemData))
                {
                    return;
                }

                output.AddNamedItem(templateId, itemData);
            });
        }
    }
}
