namespace BanjoBotAssets.Exporters
{
    internal abstract class UObjectExporter : UObjectExporter<UObject>
    {
        public UObjectExporter(DefaultFileProvider provider) : base(provider) { }
    }

    internal abstract class UObjectExporter<T> : BaseExporter
        where T : UObject
    {
        public UObjectExporter(DefaultFileProvider provider) : base(provider) { }

        protected abstract string Type { get; }

        protected virtual Task<bool> ExportAssetAsync(T asset, NamedItemData namedItemData)
        {
            return Task.FromResult(true);
        }

        public override async Task ExportAssetsAsync(IProgress<ExportProgress> progress, IAssetOutput output)
        {
            var numToProcess = assetPaths.Count;
            var processedSoFar = 0;

            await Parallel.ForEachAsync(assetPaths, async (path, _cancellationToken) =>
            {
                var file = provider[path];

                var num = Interlocked.Increment(ref processedSoFar);
                Console.WriteLine("Processing {0} {1} of {2}", Type, num, numToProcess);

                Console.WriteLine("Loading {0}", file.PathWithoutExtension);
                Interlocked.Increment(ref assetsLoaded);

                var uobject = await provider.LoadObjectAsync<T>(file.PathWithoutExtension);

                if (uobject == null)
                {
                    Console.WriteLine("Failed to load {0}", file.PathWithoutExtension);
                    return;
                }

                var templateId = $"{Type}:{uobject.Name}";
                var displayName = uobject.GetOrDefault<FText>("DisplayName")?.Text ?? $"<{uobject.Name}>";
                var description = uobject.GetOrDefault<FText>("Description")?.Text;

                var namedItemData = new NamedItemData
                {
                    AssetPath = provider.FixPath(path),
                    Name = uobject.Name,
                    Type = Type,
                    DisplayName = displayName.Trim(),
                    Description = description,
                };

                if (uobject.GetOrDefault<EFortItemTier>("Tier") is EFortItemTier tier && tier != default)
                {
                    namedItemData.Tier = (int)tier;
                }

                if (uobject.GetOrDefault<EFortRarity>("Rarity") is EFortRarity rarity && rarity != default)
                {
                    namedItemData.Rarity = rarity.GetNameText().Text;
                }

                if (await ExportAssetAsync(uobject, namedItemData) == false)
                {
                    return;
                }

                output.AddNamedItem(templateId, namedItemData);
            });
        }
    }
}
