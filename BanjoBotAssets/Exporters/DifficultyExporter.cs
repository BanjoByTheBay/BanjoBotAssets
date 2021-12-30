using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Objects.Core.i18N;

namespace BanjoBotAssets.Exporters
{
    internal sealed class DifficultyExporter : BaseExporter
    {
        public DifficultyExporter(DefaultFileProvider provider) : base(provider) { }

        public override async Task ExportAssetsAsync(IProgress<ExportProgress> progress, ExportedAssets output)
        {
            var growthBoundsPath = assetPaths.First(p => Path.GetFileNameWithoutExtension(p) == "GameDifficultyGrowthBounds");

            if (growthBoundsPath == null)
            {
                Console.WriteLine("WARNING: GameDifficultyGrowthBounds not found");
                return;
            }

            var file = provider[growthBoundsPath];

            Interlocked.Increment(ref assetsLoaded);
            var dataTable = await provider.LoadObjectAsync<UDataTable>(file.PathWithoutExtension);

            if (dataTable == null)
            {
                Console.WriteLine("WARNING: Could not load {0}", growthBoundsPath);
                return;
            }

            foreach (var (rowKey, data) in dataTable.RowMap)
            {
                var requiredRating = data.GetOrDefault<int>("RequiredRating");
                var maximumRating = data.GetOrDefault<int>("MaximumRating");
                var recommendedRating = data.GetOrDefault<int>("RecommendedRating");
                var displayName = data.GetOrDefault<FText>("ThreatDisplayName")?.Text ?? $"<{recommendedRating}>";

                output.DifficultyInfo.Add(rowKey.Text, new DifficultyInfo
                {
                    RequiredRating = requiredRating,
                    MaximumRating = maximumRating,
                    RecommendedRating = recommendedRating,
                    DisplayName = displayName.Trim(),
                });
            }

        }

        protected override bool InterestedInAsset(string name) => name.EndsWith("GameDifficultyGrowthBounds.uasset");
    }
}
