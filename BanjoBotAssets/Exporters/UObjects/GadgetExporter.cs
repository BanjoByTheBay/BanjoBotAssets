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
using CUE4Parse.UE4.Objects.Engine;

namespace BanjoBotAssets.Exporters.UObjects
{
    internal sealed class GadgetExporter : UObjectExporter<UFortGadgetItemDefinition>
    {
        public GadgetExporter(IExporterContext services) : base(services) { }

        protected override string Type => "Gadget";

        protected override bool InterestedInAsset(string name) =>
            name.Contains("/Gadgets/", StringComparison.OrdinalIgnoreCase) && name.Contains("/G_", StringComparison.OrdinalIgnoreCase);

        protected override async Task<bool> ExportAssetAsync(UFortGadgetItemDefinition asset, NamedItemData namedItemData, Dictionary<ImageType, string> imagePaths)
        {
            if (asset.GameplayAbility.AssetPathName.IsNone)
            {
                logger.LogInformation(Resources.Status_SkippingGadgetWithoutAbility, asset.Name);
                return false;
            }

            Interlocked.Increment(ref assetsLoaded);
            var gameplayAbility = await asset.GameplayAbility.LoadAsync<UBlueprintGeneratedClass>(provider);
            namedItemData.Description = await abilityDescription.GetForActiveAbilityAsync(gameplayAbility, this);
            return true;
        }
    }
}
