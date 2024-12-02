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

namespace BanjoBotAssets.Exporters.UObjects
{
    internal sealed class DefenderExporter(IExporterContext services) : UObjectExporter<UFortHeroType, NamedItemData>(services)
    {
        protected override string Type => "Defender";
        protected override bool RequireRarity => true;

        protected override bool InterestedInAsset(string name) =>
            name.Contains("Defenders/DID_", StringComparison.OrdinalIgnoreCase);

        protected override Task<bool> ExportAssetAsync(UFortHeroType asset, NamedItemData itemData, Dictionary<ImageType, string> imagePaths)
        {
            var category = asset.AttributeInitKey?.AttributeInitCategory.Text;

            if (category != null)
            {
                string[] splitCategory = category.Split('_');
                itemData.SubType = $"{splitCategory[1]} {splitCategory[0]}";
                itemData.DisplayName = asset.ItemName?.Text ?? $"{itemData.Rarity} {itemData.SubType}";
            }

            return Task.FromResult(true);
        }
    }
}
