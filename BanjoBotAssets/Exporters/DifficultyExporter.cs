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
    internal sealed class DifficultyExporter : BaseExporter
    {
        public DifficultyExporter(IExporterContext services) : base(services)
        {
        }

        public override async Task ExportAssetsAsync(IProgress<ExportProgress> progress, IAssetOutput output, CancellationToken cancellationToken)
        {
            var growthBoundsPath = assetPaths.First(p => Path.GetFileNameWithoutExtension(p).Equals("GameDifficultyGrowthBounds", StringComparison.OrdinalIgnoreCase));

            if (growthBoundsPath == null)
            {
                logger.LogError(Resources.Error_SpecificAssetNotFound, "GameDifficultyGrowthBounds");
                return;
            }

            var file = provider[growthBoundsPath];

            Interlocked.Increment(ref assetsLoaded);
            var dataTable = await provider.LoadObjectAsync<UDataTable>(file.PathWithoutExtension);

            if (dataTable == null)
            {
                logger.LogError(Resources.Warning_CouldNotLoadAsset, growthBoundsPath);
                return;
            }

            foreach (var (rowKey, data) in dataTable.RowMap)
            {
                var requiredRating = data.GetOrDefault<int>("RequiredRating");
                var maximumRating = data.GetOrDefault<int>("MaximumRating");
                var recommendedRating = data.GetOrDefault<int>("RecommendedRating");
                var displayName = data.GetOrDefault<FText>("ThreatDisplayName")?.Text ?? $"<{recommendedRating}>";

                output.AddDifficultyInfo(rowKey.Text, new DifficultyInfo
                {
                    RequiredRating = requiredRating,
                    MaximumRating = maximumRating,
                    RecommendedRating = recommendedRating,
                    DisplayName = displayName.Trim(),
                });
            }
        }

        protected override bool InterestedInAsset(string name) => name.EndsWith("GameDifficultyGrowthBounds.uasset", StringComparison.OrdinalIgnoreCase);
    }
}
