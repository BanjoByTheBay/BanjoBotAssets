using BanjoBotAssets.Artifacts.Models;

namespace BanjoBotAssets.Exporters.UObjects
{
    internal sealed class CardPackExporter : UObjectExporter
    {
        public CardPackExporter(IExporterContext services) : base(services) { }

        protected override string Type => "CardPack";

        protected override bool InterestedInAsset(string name) => name.Contains("/Items/CardPacks/", StringComparison.OrdinalIgnoreCase);

        protected override Task<bool> ExportAssetAsync(UObject asset, NamedItemData itemData, Dictionary<ImageType, string> imagePaths)
        {
            if (asset.GetSoftAssetPath("PackImage") is string path)
            {
                imagePaths.Add(ImageType.PackImage, path);
            }

            return base.ExportAssetAsync(asset, itemData, imagePaths);
        }
    }
}
