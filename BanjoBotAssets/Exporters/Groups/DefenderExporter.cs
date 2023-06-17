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
namespace BanjoBotAssets.Exporters.Groups
{
    internal sealed partial class DefenderExporter : GroupExporter<UFortHeroType>
    {
        protected override string Type => "Defender";

        protected override bool InterestedInAsset(string name) =>
            name.Contains("Defenders/DID_", StringComparison.OrdinalIgnoreCase);

        private static readonly Regex defenderAssetNameRegex = DefenderAssetNameRegex();

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

        [GeneratedRegex(".*/([^/]+)_(C|UC|R|VR|SR|UR)_T(\\d+)(?:\\..*)?$", RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
        private static partial Regex DefenderAssetNameRegex();
    }
}
