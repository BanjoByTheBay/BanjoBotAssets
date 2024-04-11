using CUE4Parse.UE4.Assets.Exports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanjoBotAssets.Exporters.UObjects
{
    internal sealed class SurvivorPortraitExporter(IExporterContext services) : UObjectExporter(services)
    {
        protected override string Type => "WorkerPortrait";

        protected override bool InterestedInAsset(string name) => name.Contains("/Icon-Worker/IconDefinitions", StringComparison.OrdinalIgnoreCase);

        protected override Task<bool> ExportAssetAsync(UObject asset, NamedItemData itemData, Dictionary<ImageType, string> imagePaths)
        {
            if (asset.GetSoftAssetPath("SmallImage") is string smallImagePath)
                imagePaths.Add(ImageType.SmallPreview, smallImagePath);

            if (asset.GetSoftAssetPath("LargeImage") is string largeImagePath)
                imagePaths.Add(ImageType.LargePreview, largeImagePath);

            return base.ExportAssetAsync(asset, itemData, imagePaths);
        }
    }
}
