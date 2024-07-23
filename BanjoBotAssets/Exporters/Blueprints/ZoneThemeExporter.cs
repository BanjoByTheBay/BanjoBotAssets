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

namespace BanjoBotAssets.Exporters.Blueprints
{
    internal sealed class ZoneThemeExporter(IExporterContext services) : BlueprintExporter(services)
    {
        protected override string Type => "ZoneTheme";

        protected override string DisplayNameProperty => "ZoneName";

        protected override string? DescriptionProperty => "ZoneDescription";

        protected override bool InterestedInAsset(string name) =>
            name.Contains("/ZoneThemes/", StringComparison.OrdinalIgnoreCase) && name.Contains("/BP_ZT_", StringComparison.OrdinalIgnoreCase);

        protected override Task<bool> ExportAssetAsync(UBlueprintGeneratedClass bpClass, UObject classDefaultObject, NamedItemData namedItemData, Dictionary<ImageType, string> imagePaths)
        {
            namedItemData.ImagePaths = [];
            if (classDefaultObject.GetSoftAssetPath("BackgroundImage") is string smallImagePath)
            {
                namedItemData.ImagePaths.Add(ImageType.LoadingScreen, smallImagePath);
                imagePaths.Add(ImageType.LoadingScreen, smallImagePath);
            }
            return base.ExportAssetAsync(bpClass, classDefaultObject, namedItemData, imagePaths);
        }
    }
}
