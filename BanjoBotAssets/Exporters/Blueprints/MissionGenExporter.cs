using BanjoBotAssets.Artifacts.Models;
using BanjoBotAssets.Exporters.Helpers;
using BanjoBotAssets.Extensions;
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

            var bgImage = classDefaultObject
                .GetOrDefault<FStructFallback>("LoadingScreenConfig")
                ?.GetOrDefault<FSoftObjectPath>("BackgroundImage")
                .AssetPathName ?? new FName();
            if (!bgImage.IsNone)
            {
                imagePaths.Add(ImageType.LoadingScreen, bgImage.Text);
            }

            return Task.FromResult(true);
        }
    }
}
