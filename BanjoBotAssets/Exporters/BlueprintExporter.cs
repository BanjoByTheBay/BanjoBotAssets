using CUE4Parse.UE4.Objects.Engine;

namespace BanjoBotAssets.Exporters
{
    internal abstract class BlueprintExporter : BaseExporter
    {
        private int numToProcess;
        private int processedSoFar;

        public BlueprintExporter(DefaultFileProvider provider) : base(provider) { }

        protected abstract string Type { get; }
        protected abstract string DisplayNameProperty { get; }
        protected abstract string? DescriptionProperty { get; }

        protected virtual Task<bool> ExportAssetAsync(UBlueprintGeneratedClass bpClass, UObject classDefaultObject, NamedItemData namedItemData)
        {
            return Task.FromResult(true);
        }

        public override async Task ExportAssetsAsync(IProgress<ExportProgress> progress, IAssetOutput output)
        {
            numToProcess = assetPaths.Count;

            await Parallel.ForEachAsync(assetPaths, async (path, _cancellationToken) =>
            {
                var file = provider![path];

                var num = Interlocked.Increment(ref processedSoFar);
                Console.WriteLine("Processing {0} {1} of {2}", Type, num, numToProcess);

                //Console.WriteLine("Loading {0}", file.PathWithoutExtension);
                Interlocked.Increment(ref assetsLoaded);
                var pkg = await provider.LoadPackageAsync(file.PathWithoutExtension);

                if (pkg.GetExports().First() is not UBlueprintGeneratedClass bpClass)
                {
                    Console.WriteLine("WARNING: Failed to load {0}", file.PathWithoutExtension);
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

                if (await ExportAssetAsync(bpClass, cdo, namedItemData) == false)
                {
                    return;
                }

                output.AddNamedItem(bpClassPath, namedItemData);
            });
        }

    }
}
