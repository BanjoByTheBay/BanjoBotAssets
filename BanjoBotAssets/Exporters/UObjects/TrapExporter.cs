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
using BanjoBotAssets.UExports;
using CUE4Parse.FN.Enums.FortniteGame;
using CUE4Parse.UE4.Objects.GameplayTags;
using Microsoft.Extensions.Hosting;

namespace BanjoBotAssets.Exporters.UObjects
{
    internal sealed partial class TrapExporter(IExporterContext services) : UObjectExporter<UFortItemDefinition, TrapItemData>(services)
    {
        private string? trapsPath, durabilityPath, disassemblePath;
        private Dictionary<string, FStructFallback>? trapsTable, durabilityTable;
        private Dictionary<string, FRecipe>? disassembleTable;

        protected override string Type => "Trap";
        protected override bool RequireRarity => true;

        protected override bool InterestedInAsset(string name)
        {
            // we also want to keep track of:
            //   Traps
            //   WeaponDurabilityRarity
            //   DisassembleRecipes

            switch (Path.GetFileName(name))
            {
                case string s when s.Equals("Traps.uasset", StringComparison.OrdinalIgnoreCase):
                    trapsPath = name;
                    break;
                case string s when s.Equals("WeaponDurabilityRarity.uasset", StringComparison.OrdinalIgnoreCase):
                    durabilityPath = name;
                    break;
                case string s when s.Equals("DisassembleRecipes.uasset", StringComparison.OrdinalIgnoreCase):
                    disassemblePath = name;
                    break;
            }

            if (!name.Contains("/SaveTheWorld/", StringComparison.OrdinalIgnoreCase))
                return false;

            return
                name.Contains("/TID_", StringComparison.OrdinalIgnoreCase);
        }

        public override async Task ExportAssetsAsync(IProgress<ExportProgress> progress, IAssetOutput output, CancellationToken cancellationToken)
        {
            var trapsTask = TryLoadTableAsync(trapsPath);
            var durabilityTask = TryLoadTableAsync(durabilityPath);
            var disassembleTask = TryLoadTableAsync(disassemblePath);

            trapsTable = (await trapsTask)?.ToDictionary();
            durabilityTable = (await durabilityTask)?.ToDictionary();
            disassembleTable = (await disassembleTask)?.ToDictionary<FRecipe>();

            await base.ExportAssetsAsync(progress, output, cancellationToken);
        }

        protected override Task<bool> ExportAssetAsync(UFortItemDefinition asset, TrapItemData itemData, Dictionary<ImageType, string> imagePaths)
        {
            var subType = SubTypeFromTags(asset.GameplayTags);
            itemData.SubType = subType;
            if (subType is not null)
            {
                itemData.Tier = (int)asset.GetOrDefaultFromDataList<EFortItemTier>("Tier");
            }


            var disassembleHandle = asset.GetOrDefaultFromDataList<FDataTableRowHandle>("DisassembleRecipe");
            if (!(disassembleHandle is null or { RowName.IsNone: true } or { DataTable: null }))
            {
                var recipe = disassembleTable?[disassembleHandle.RowName.Text];
                if (recipe is not null)
                    itemData.DismantleResults = recipe.RecipeResults.ToDictionary(
                            p => $"{p.ItemPrimaryAssetId.PrimaryAssetType.Name.Text}:{p.ItemPrimaryAssetId.PrimaryAssetName.Text}",
                            p => p.Quantity,
                            StringComparer.OrdinalIgnoreCase
                        );
            }

            var statRow = asset.GetOrDefault<FDataTableRowHandle?>("WeaponStatHandle")?.RowName.Text;
            if (statRow is null)
            {
                logger.LogWarning(Resources.Warning_NoStatsLocatedForSchematicUsingPrefix, asset.Name, "<>");
            }
            else if (trapsTable?.TryGetValue(statRow, out var trapStats) ?? false)
            {
                EFortRarity rarity = asset.GetOrDefault("Rarity", EFortRarity.Uncommon);
                itemData.TrapStats = ConvertTrapStats(trapStats, rarity, durabilityTable);
            }
            else
            {
                logger.LogWarning(Resources.Warning_NoStatsLocatedForSchematicUsingPrefix, asset.Name, "<>");
            }

            return Task.FromResult(true);
        }

        public static string? SubTypeFromTags(FGameplayTagContainer tags)
        {
            foreach (var tag in tags.GameplayTags)
            {
                var match = TrapSubTypeRegex().Match(tag.ToString());

                if (match.Success)
                {
                    return match.Groups[1].Value.ToLower(CultureInfo.InvariantCulture) switch
                    {
                        "ceiling" => Resources.Field_Schematic_Ceiling,
                        "floor" => Resources.Field_Schematic_Floor,
                        "wall" => Resources.Field_Schematic_Wall,
                        _ => null,
                    };
                }
            }

            return null;
        }

        [GeneratedRegex(@"^(?:Weapon\.Trap(?=\.(?:Ceiling|Floor|Wall)))\.([^.]+)", RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
        private static partial Regex TrapSubTypeRegex();

        public static TrapStats ConvertTrapStats(FStructFallback row, EFortRarity rarity, Dictionary<string, FStructFallback>? durabilityTable)
        {
            var result = new TrapStats
            {
                ArmTime = row.GetOrDefault<float>("ArmTime"),
                FireDelay = row.GetOrDefault<float>("FireDelay"),
                ReloadTime = row.GetOrDefault<float>("ReloadTime"),
                Damage = row.GetOrDefault<float>("DmgPB"),
                ImpactDamage = row.GetOrDefault<float>("ImpactDmgPB"),
                KnockbackMagnitude = row.GetOrDefault<float>("KnockbackMagnitude"),
                KnockbackZAngle = row.GetOrDefault<float>("KnockbackZAngle"),
                StunTime = row.GetOrDefault<float>("StunTime"),
                BaseCritChance = row.GetOrDefault<float>("DiceCritChance"),
                BaseCritDamage = row.GetOrDefault<float>("DiceCritDamageMultiplier")
            };
            var durabilityRow = row.GetOrDefault<FName>("DurabilityRowName").Text;
            result.Durability = durabilityTable?[durabilityRow].GetOrDefault<int>(rarity.GetNameText().Text);

            return result;
        }
    }
}
