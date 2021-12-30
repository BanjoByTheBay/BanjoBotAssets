using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets.Exports.Engine;

namespace BanjoBotAssets.Exporters
{
    internal sealed class ItemRatingExporter : BaseExporter
    {
        public ItemRatingExporter(DefaultFileProvider provider) : base(provider) { }

        protected override bool InterestedInAsset(string name) => name.EndsWith("ItemRating.uasset");

        public override async Task ExportAssetsAsync(IProgress<ExportProgress> progress, ExportedAssets output)
        {
            progress.Report(new ExportProgress { TotalSteps = 2, CompletedSteps = 0, AssetsLoaded = assetsLoaded, CurrentItem = "Exporting item ratings" });

            await ExportDefaultItemRatingsAsync(output);

            progress.Report(new ExportProgress { TotalSteps = 2, CompletedSteps = 1, AssetsLoaded = assetsLoaded, CurrentItem = "Exporting item ratings" });

            await ExportSurvivorItemRatingsAsync(output);

            progress.Report(new ExportProgress { TotalSteps = 2, CompletedSteps = 2, AssetsLoaded = assetsLoaded, CurrentItem = "Exported item ratings" });
        }

        async Task ExportDefaultItemRatingsAsync(ExportedAssets output)
        {
            var baseItemRatingPath = assetPaths.FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == "BaseItemRating");

            if (baseItemRatingPath == null)
            {
                Console.WriteLine("WARNING: BaseItemRating not found");
                return;
            }

            var file = provider[baseItemRatingPath];

            Interlocked.Increment(ref assetsLoaded);
            var curveTable = await provider.LoadObjectAsync<UCurveTable>(file.PathWithoutExtension);

            if (curveTable == null)
            {
                Console.WriteLine("WARNING: Could not load {0}", baseItemRatingPath);
                return;
            }

            output.ItemRatings.Default = EvaluateItemRatingCurve(curveTable, "Default");
        }

        async Task ExportSurvivorItemRatingsAsync(ExportedAssets output)
        {
            var survivorItemRatingPath = assetPaths.FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == "SurvivorItemRating");

            if (survivorItemRatingPath == null)
            {
                Console.WriteLine("WARNING: SurvivorItemRating not found");
                return;
            }

            var file = provider[survivorItemRatingPath];

            Interlocked.Increment(ref assetsLoaded);
            var curveTable = await provider.LoadObjectAsync<UCurveTable>(file.PathWithoutExtension);

            if (curveTable == null)
            {
                Console.WriteLine("WARNING: Could not load {0}", survivorItemRatingPath);
                return;
            }

            output.ItemRatings.Survivor = EvaluateItemRatingCurve(curveTable, "Default");
            output.ItemRatings.LeadSurvivor = EvaluateItemRatingCurve(curveTable, "Manager", true);
        }

        static readonly (string rarity, int maxTier)[] rarityTiers =
        {
            ("C", 2),
            ("UC", 3),
            ("R", 4),
            ("VR", 5),
            ("SR", 5),
            ("UR", 5),
        };

        static readonly (int tier, int minLevel, int maxLevel)[] tierLevels =
        {
            (1, 1, 10),
            (2, 10, 20),
            (3, 20, 30),
            (4, 30, 40),
            (5, 40, 60),    // tier 5 goes up to LV 60 with superchargers
        };

        ItemRatingTable EvaluateItemRatingCurve(UCurveTable curveTable, string prefix, bool skipUR = false)
        {
            var tiers = new Dictionary<string, ItemRatingTier>();

            foreach (var (rarity, maxTier) in rarityTiers)
            {
                if (skipUR && rarity == "UR")
                    continue;

                foreach (var (tier, minLevel, maxLevel) in tierLevels)
                {
                    if (tier > maxTier)
                        break;

                    var rowNameStr = $"{prefix}_{rarity}_T{tier:00}";
                    var rowFName = curveTable.RowMap.Keys.FirstOrDefault(k => k.Text == rowNameStr);

                    if (rowFName.IsNone)
                    {
                        Console.WriteLine("WARNING: Curve table has no row {0}", rowNameStr);
                        continue;
                    }

                    var curve = curveTable.FindCurve(rowFName);

                    if (curve == null)
                    {
                        Console.WriteLine("WARNING: Could not find curve {0}", rowNameStr);
                        continue;
                    }

                    var values = new List<float>();

                    for (int level = minLevel; level <= maxLevel; level++)
                    {
                        values.Add(curve.Eval(level));
                    }

                    tiers.Add($"{rarity}_T{tier:00}",
                        new ItemRatingTier { FirstLevel = minLevel, Ratings = values.ToArray() });
                }
            }

            return new ItemRatingTable { Tiers = tiers };
        }
    }
}
