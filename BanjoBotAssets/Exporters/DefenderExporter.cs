﻿namespace BanjoBotAssets.Exporters
{
    internal sealed class DefenderExporter : GroupExporter<UFortHeroType>
    {
        protected override string Type => "Defender";

        protected override bool InterestedInAsset(string name) => name.Contains("Defenders/DID_");

        private static readonly Regex defenderAssetNameRegex = new(@".*/([^/]+)_(C|UC|R|VR|SR|UR)_T(\d+)(?:\..*)?$");

        public DefenderExporter(DefaultFileProvider provider) : base(provider) { }

        protected override BaseParsedItemName? ParseAssetName(string name)
        {
            var match = defenderAssetNameRegex.Match(name);

            if (!match.Success)
            {
                Console.WriteLine("WARNING: Can't parse defender name: {0}", name);
                return null;
            }

            return new BaseParsedItemName(BaseName: match.Groups[1].Value, Rarity: match.Groups[2].Value, Tier: int.Parse(match.Groups[3].Value));
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

                subType = $"{weapon} Defender";
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
            return $"{rarity.GetNameText()} {fields.SubType ?? "Defender"}";
        }
    }
}