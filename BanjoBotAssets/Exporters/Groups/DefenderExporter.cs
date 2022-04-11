using BanjoBotAssets.Exporters.Helpers;

namespace BanjoBotAssets.Exporters.Groups
{
    internal sealed class DefenderExporter : GroupExporter<UFortHeroType>
    {
        protected override string Type => "Defender";

        protected override bool InterestedInAsset(string name) =>
            name.Contains("Defenders/DID_", StringComparison.OrdinalIgnoreCase);

        private static readonly Regex defenderAssetNameRegex = new(@".*/([^/]+)_(C|UC|R|VR|SR|UR)_T(\d+)(?:\..*)?$", RegexOptions.IgnoreCase);

        public DefenderExporter(IExporterContext services) : base(services) { }

        protected override BaseParsedItemName? ParseAssetName(string name)
        {
            var match = defenderAssetNameRegex.Match(name);

            if (!match.Success)
            {
                logger.LogWarning(Resources.Warning_CannotParseDefenderName, name);
                return null;
            }

            return new BaseParsedItemName(BaseName: match.Groups[1].Value, Rarity: match.Groups[2].Value, Tier: int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture));
        }

        protected override async Task<BaseItemGroupFields> ExtractCommonFieldsAsync(UFortHeroType asset, IGrouping<string?, string> grouping)
        {
            var result = await base.ExtractCommonFieldsAsync(asset, grouping);

            var category = asset.AttributeInitKey?.AttributeInitCategory.Text;
            string? subType;

            if (category != null)
            {
                var i = category.LastIndexOf('_');
                var weapon = category[(i + 1)..];

                subType = string.Format(CultureInfo.CurrentCulture, Resources.FormatString_Field_Defender_NameFormat, weapon);
            }
            else
            {
                subType = null;
            }

            return result with { SubType = subType };
        }

        protected override string GetDisplayName(BaseParsedItemName parsedName, UFortHeroType primaryAsset, BaseItemGroupFields fields)
        {
            if (primaryAsset.DisplayName is FText ft)
                return ft.Text;

            var rarity = GetRarity(parsedName, primaryAsset, fields);
            return string.Format(CultureInfo.CurrentCulture, Resources.FormatString_Field_Defender_DisplayNameFormat, rarity.GetNameText(), fields.SubType ?? Resources.Field_Defender_DefaultName);
        }
    }
}
