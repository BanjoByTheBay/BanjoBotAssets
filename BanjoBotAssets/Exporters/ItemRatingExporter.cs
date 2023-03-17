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
namespace BanjoBotAssets.Exporters
{
    internal sealed class ItemRatingExporter : BaseExporter
    {
        protected override bool InterestedInAsset(string name) => name.EndsWith("ItemRating.uasset", StringComparison.OrdinalIgnoreCase);

        public override async Task ExportAssetsAsync(IProgress<ExportProgress> progress, IAssetOutput output, CancellationToken cancellationToken)
        {
            progress.Report(new ExportProgress { TotalSteps = 2, CompletedSteps = 0, AssetsLoaded = assetsLoaded, CurrentItem = Resources.Status_ExportingItemRatings });

            await ExportDefaultItemRatingsAsync(output);

            progress.Report(new ExportProgress { TotalSteps = 2, CompletedSteps = 1, AssetsLoaded = assetsLoaded, CurrentItem = Resources.Status_ExportingItemRatings });

            await ExportSurvivorItemRatingsAsync(output);

            progress.Report(new ExportProgress { TotalSteps = 2, CompletedSteps = 2, AssetsLoaded = assetsLoaded, CurrentItem = Resources.Status_ExportedItemRatings });
        }

        private async Task ExportDefaultItemRatingsAsync(IAssetOutput output)
        {
            var baseItemRatingPath = assetPaths.Find(p => Path.GetFileNameWithoutExtension(p).Equals("BaseItemRating", StringComparison.OrdinalIgnoreCase));

            if (baseItemRatingPath == null)
            {
                logger.LogError(Resources.Error_SpecificAssetNotFound, "BaseItemRating");
                return;
            }

            var file = provider[baseItemRatingPath];

            Interlocked.Increment(ref assetsLoaded);
            var curveTable = await provider.LoadObjectAsync<UCurveTable>(file.PathWithoutExtension);

            if (curveTable == null)
            {
                logger.LogError(Resources.Warning_CouldNotLoadAsset, baseItemRatingPath);
                return;
            }

            output.AddDefaultItemRatings(EvaluateItemRatingCurve(curveTable, "Default"));
        }

        private async Task ExportSurvivorItemRatingsAsync(IAssetOutput output)
        {
            var survivorItemRatingPath = assetPaths.Find(p => Path.GetFileNameWithoutExtension(p).Equals("SurvivorItemRating", StringComparison.OrdinalIgnoreCase));

            if (survivorItemRatingPath == null)
            {
                logger.LogError(Resources.Error_SpecificAssetNotFound, "SurvivorItemRating");
                return;
            }

            var file = provider[survivorItemRatingPath];

            Interlocked.Increment(ref assetsLoaded);
            var curveTable = await provider.LoadObjectAsync<UCurveTable>(file.PathWithoutExtension);

            if (curveTable == null)
            {
                logger.LogError(Resources.Warning_CouldNotLoadAsset, survivorItemRatingPath);
                return;
            }

            output.AddSurvivorItemRatings(EvaluateItemRatingCurve(curveTable, "Default"));
            output.AddLeadSurvivorItemRatings(EvaluateItemRatingCurve(curveTable, "Manager", true));
        }

        private static readonly (string rarity, int maxTier)[] rarityTiers =
        {
            ("C", 5),
            ("UC", 5),
            ("R", 5),
            ("VR", 5),
            ("SR", 5),
            ("UR", 5),
        };
        private static readonly (int tier, int minLevel, int maxLevel)[] tierLevels =
        {
            (1, 1, 10),
            (2, 10, 20),
            (3, 20, 30),
            (4, 30, 40),
            (5, 40, 60),    // tier 5 goes up to LV 60 with superchargers
        };

        public ItemRatingExporter(IExporterContext services) : base(services) { }

        private ItemRatingTable EvaluateItemRatingCurve(UCurveTable curveTable, string prefix, bool skipUR = false)
        {
            var tiers = new Dictionary<string, ItemRatingTier>(StringComparer.OrdinalIgnoreCase);

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
                        logger.LogError(Resources.Warning_MissingCurveTableRow, rowNameStr);
                        continue;
                    }

                    var curve = curveTable.FindCurve(rowFName);

                    if (curve == null)
                    {
                        logger.LogError(Resources.Warning_CouldNotLoadCurveTableRow, rowNameStr);
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
