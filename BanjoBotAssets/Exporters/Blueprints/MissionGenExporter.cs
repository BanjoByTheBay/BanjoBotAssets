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
using BanjoBotAssets.Artifacts.Models;
using CUE4Parse.UE4.Objects.Engine;

namespace BanjoBotAssets.Exporters.Blueprints
{
    internal sealed class MissionGenExporter : BlueprintExporter
    {
        public MissionGenExporter(IExporterContext services) : base(services) { }

        protected override string Type => "MissionGen";

        protected override string DisplayNameProperty => "MissionName";

        protected override string? DescriptionProperty => "MissionDescription";

        //protected override (ImageType type, string property)[]? ImageResources => new[] {
        //    (ImageType.Icon, "MissionIcon.ResourceObject.ObjectPath"),
        //    (ImageType.LoadingScreen, "LoadingScreenConfig.BackgroundImage.AssetPathName"),
        //};

        protected override bool InterestedInAsset(string name) =>
            name.Contains("/MissionGens/", StringComparison.OrdinalIgnoreCase) && name.Contains("/World/", StringComparison.OrdinalIgnoreCase);

        protected override Task<bool> ExportAssetAsync(UBlueprintGeneratedClass bpClass, UObject classDefaultObject, NamedItemData namedItemData,
            Dictionary<ImageType, string> imagePaths)
        {
            if (classDefaultObject.GetResourceObjectPath("MissionIcon") is string path)
            {
                imagePaths.Add(ImageType.Icon, path);
            }

            var bgImage = classDefaultObject.GetOrDefault<FStructFallback>("LoadingScreenConfig")?.GetSoftAssetPath("BackgroundImage");
            if (bgImage != null)
            {
                imagePaths.Add(ImageType.LoadingScreen, bgImage);
            }

            return Task.FromResult(true);
        }
    }
}
