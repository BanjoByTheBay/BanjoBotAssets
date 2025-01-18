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
using BanjoBotAssets.UExports;

namespace BanjoBotAssets.Exporters.UObjects
{
    internal sealed class AlterationExporter(IExporterContext services) : UObjectExporter<UFortItemDefinition, AlterationItemData>(services)
    {
        protected override string Type => "Alteration";
        private string? metaPath;
        protected override bool InterestedInAsset(string name)
        {
            if(Path.GetFileName(name).Equals("MetaRecipes.uasset", StringComparison.OrdinalIgnoreCase))
            {
                metaPath = name;
            }

            return name.Contains("/AID_", StringComparison.OrdinalIgnoreCase) &&
                (name.Contains("/Alteration_v2/", StringComparison.OrdinalIgnoreCase) || name.Contains("/Defenders/", StringComparison.OrdinalIgnoreCase));
        }
        public override async Task ExportAssetsAsync(IProgress<ExportProgress> progress, IAssetOutput output, CancellationToken cancellationToken)
        {
            metaRecipeTable = (await TryLoadTableAsync(metaPath))?.ToDictionary<FRecipe>();

            await base.ExportAssetsAsync(progress, output, cancellationToken);
        }

        protected override Task<bool> ExportAssetAsync(UFortItemDefinition asset, AlterationItemData itemData, Dictionary<ImageType, string> imagePaths)
        {
            itemData.DisplayName = asset.ItemDescription?.Text ?? $"<Alteration:{asset.Name}>";
            itemData.Description = null;

            var extraRespecCost = asset.GetOrDefault<FFortItemQuantityPair[]>("AdditionalRespecCosts")?.ToDictionary(
                    p => $"{p.ItemPrimaryAssetId.PrimaryAssetType.Name.Text}:{p.ItemPrimaryAssetId.PrimaryAssetName.Text}",
                    p => p.Quantity,
                    StringComparer.OrdinalIgnoreCase
                );
            itemData.AdditionalRespecCost = extraRespecCost?.Count == 0 ? null : extraRespecCost;

            return Task.FromResult(true);
        }
    }
}
