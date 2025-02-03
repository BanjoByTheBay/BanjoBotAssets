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

using CUE4Parse.FN.Structs.GA;

namespace BanjoBotAssets.Exporters.UObjects
{
    internal sealed class AccoladeExporter(IExporterContext services) : UObjectExporter<UObject, AccoladeItemData>(services)
    {
        protected override string Type => "Accolades";

        protected override bool InterestedInAsset(string name) => name.Contains("/AccoladeId_STW_", StringComparison.OrdinalIgnoreCase);

        protected override Task<bool> ExportAssetAsync(UObject asset, AccoladeItemData itemData, Dictionary<ImageType, string> imagePaths)
        {
            if (asset.GetOrDefault<FScalableFloat>("XpRewardAmount") is FScalableFloat xpAmount)
            {
                itemData.AccoladeXP = (int)xpAmount.GetScaledValue(logger);
            }
            return base.ExportAssetAsync(asset, itemData, imagePaths);
        }
    }
}
