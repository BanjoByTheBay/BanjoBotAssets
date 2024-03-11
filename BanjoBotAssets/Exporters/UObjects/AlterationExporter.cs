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
    internal sealed class AlterationExporter(IExporterContext services) : UObjectExporter<UFortItemDefinition>(services)
    {
        protected override string Type => "Alteration";

        protected override bool InterestedInAsset(string name) =>
            name.Contains("/AID_", StringComparison.OrdinalIgnoreCase) &&
            (name.Contains("/Alteration_v2/", StringComparison.OrdinalIgnoreCase) || name.Contains("/Defenders/", StringComparison.OrdinalIgnoreCase));

        protected override Task<bool> ExportAssetAsync(UFortItemDefinition asset, NamedItemData namedItemData, Dictionary<ImageType, string> imagePaths)
        {
            namedItemData.DisplayName = asset.ItemDescription?.Text ?? $"<Alteration:{asset.Name}>";
            namedItemData.Description = null;
            return Task.FromResult(true);
        }
    }
}
