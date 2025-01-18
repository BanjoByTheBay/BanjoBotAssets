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
using System.Collections.Concurrent;

namespace BanjoBotAssets.Exporters.UObjects
{
    internal sealed partial class WeaponExporter(IExporterContext services) : UObjectExporter<UFortItemDefinition, WeaponItemData>(services)
    {
        private string? meleeWeaponsPath, rangedWeaponsPath, durabilityPath, disassemblePath;
        private Dictionary<string, FStructFallback>? meleeWeaponsTable, rangedWeaponsTable, durabilityTable;
        private Dictionary<string, FRecipe>? disassembleTable;

        protected override string Type => "Weapon";
        protected override bool RequireRarity => true;

        protected override bool InterestedInAsset(string name)
        {
            // we also want to keep track of:
            //   MeleeWeapons
            //   RangedWeapons
            //   WeaponDurabilityRarity
            //   DisassembleRecipes

            switch (Path.GetFileName(name))
            {
                case string s when s.Equals("MeleeWeapons.uasset", StringComparison.OrdinalIgnoreCase):
                    meleeWeaponsPath = name;
                    break;
                case string s when s.Equals("RangedWeapons.uasset", StringComparison.OrdinalIgnoreCase):
                    rangedWeaponsPath = name;
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
                name.Contains("/WID_", StringComparison.OrdinalIgnoreCase);
        }

        public override async Task ExportAssetsAsync(IProgress<ExportProgress> progress, IAssetOutput output, CancellationToken cancellationToken)
        {
            var meleeWeaponsTask = TryLoadTableAsync(meleeWeaponsPath);
            var rangedWeaponsTask = TryLoadTableAsync(rangedWeaponsPath);
            var durabilityTask = TryLoadTableAsync(durabilityPath);
            var disassembleTask = TryLoadTableAsync(disassemblePath);

            meleeWeaponsTable = (await meleeWeaponsTask)?.ToDictionary();
            rangedWeaponsTable = (await rangedWeaponsTask)?.ToDictionary();
            durabilityTable = (await durabilityTask)?.ToDictionary();
            disassembleTable = (await disassembleTask)?.ToDictionary<FRecipe>();

            await base.ExportAssetsAsync(progress, output, cancellationToken);
        }

        protected override async Task<bool> ExportAssetAsync(UFortItemDefinition asset, WeaponItemData itemData, Dictionary<ImageType, string> imagePaths)
        {

            var (category, subType) = CategoryAndSubTypeFromTags(asset.GameplayTags);
            itemData.Category = category;
            itemData.SubType = subType;
            if (subType is not null)
            {
                itemData.Tier = (int)asset.GetOrDefaultFromDataList<EFortItemTier>("Tier");
                itemData.DisplayTier = asset.GetOrDefault<EFortDisplayTier>("DisplayTier").ToString();
                if (itemData.DisplayTier == "Handmade")
                    itemData.Tier = 0;
                if (itemData.DisplayName == "Primal Stink Bow" && itemData.DisplayTier == "Shadowshard")
                    itemData.Tier = 4; // damn you epic with your typos and misclicks
                itemData.TriggerType = asset.GetOrDefault<EFortWeaponTriggerType>("TriggerType").ToString();
            }


            if (asset.Name.Contains("_ore_", StringComparison.OrdinalIgnoreCase))
                itemData.EvoType = "ore";
            if (asset.Name.Contains("_crystal_", StringComparison.OrdinalIgnoreCase))
                itemData.EvoType = "crystal";

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

            EFortRarity rarity = asset.GetOrDefault("Rarity", EFortRarity.Uncommon);
            var statRow = asset.GetOrDefault<FDataTableRowHandle?>("WeaponStatHandle")?.RowName.Text;
            if (statRow is null)
            {
                logger.LogWarning(Resources.Warning_NoStatsLocatedForSchematicUsingPrefix, asset.Name, "<>");
            }
            else if (rangedWeaponsTable?.TryGetValue(statRow, out var rangedStats) ?? false)
            {
                var ammoType = await AmmoTypeFromPathAsync(asset.GetOrDefault<FSoftObjectPath>("AmmoData"));
                itemData.RangedWeaponStats = ConvertRangedWeaponStats(rangedStats, ammoType, rarity, durabilityTable);
            }
            else if (meleeWeaponsTable?.TryGetValue(statRow, out var meleeStats) ?? false)
            {
                itemData.MeleeWeaponStats = ConvertMeleeWeaponStats(meleeStats, rarity, durabilityTable);
            }
            else
            {
                logger.LogWarning(Resources.Warning_NoStatsLocatedForSchematicUsingPrefix, asset.Name, "<>");
            }

            return true;
        }

        private readonly ConcurrentDictionary<string, Task<string?>> cachedAmmoTypesFromPaths = new();
        private async Task<string?> AmmoTypeFromPathAsync(FSoftObjectPath ammoDataPath)
        {
            if (ammoDataPath.AssetPathName.IsNone)
                return null;

            return await cachedAmmoTypesFromPaths.GetOrAdd(ammoDataPath.AssetPathName.Text, static async (path, provider) =>
            {
                var asset = await provider.LoadObjectAsync<UFortAmmoItemDefinition>(path);
                if (asset.ItemName?.Text is string str)
                {
                    var i = str.IndexOf(':');
                    return str[(i + 1)..].Trim();
                }
                return null;
            }, provider);
        }

        public static (string? category, string? subType) CategoryAndSubTypeFromTags(FGameplayTagContainer tags)
        {
            foreach (var tag in tags.GameplayTags)
            {
                var match = WeaponSubTypeRegex().Match(tag.ToString());

                if (match.Success)
                {
                    return match.Groups[1].Value.ToLower(CultureInfo.InvariantCulture) switch
                    {
                        "hammer" => (Resources.Field_Recipe_Melee, Resources.Field_Schematic_Hardware),
                        "heavy" => (Resources.Field_Recipe_Ranged, Resources.Field_Schematic_Explosive),
                        "improvised" => (Resources.Field_Recipe_Melee, Resources.Field_Schematic_Club),
                        "smg" => (Resources.Field_Recipe_Ranged, Resources.Field_Schematic_SMG),
                        "assault" => (Resources.Field_Recipe_Ranged, Resources.Field_Schematic_Assault),
                        "axe" => (Resources.Field_Recipe_Melee, Resources.Field_Schematic_Axe),
                        "pistol" => (Resources.Field_Recipe_Ranged, Resources.Field_Schematic_Pistol),
                        "scythe" => (Resources.Field_Recipe_Melee, Resources.Field_Schematic_Scythe),
                        "shotgun" => (Resources.Field_Recipe_Ranged, Resources.Field_Schematic_Shotgun),
                        "sniper" => (Resources.Field_Recipe_Ranged, Resources.Field_Schematic_Sniper),
                        "spear" => (Resources.Field_Recipe_Melee, Resources.Field_Schematic_Spear),
                        "sword" => (Resources.Field_Recipe_Melee, Resources.Field_Schematic_Sword),
                        _ => (null, null),
                    };
                }
            }

            return (null, null);
        }
        [GeneratedRegex(@"^(?:Weapon\.(?:Ranged|Melee\.(?:Edged|Blunt|Piercing)))\.([^.]+)", RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
        private static partial Regex WeaponSubTypeRegex();

        public static RangedWeaponStats ConvertRangedWeaponStats(FStructFallback row, string? ammoType, EFortRarity rarity, Dictionary<string, FStructFallback>? durabilityTable)
        {
            var result = new RangedWeaponStats
            {
                AmmoType = ammoType,
                BulletsPerCartridge = row.GetOrDefault<int>("BulletsPerCartridge"),
                FiringRate = row.GetOrDefault<float>("FiringRate"),
                PointBlank = MakeDamageRange(row, "DmgPB", "EnvDmgPB", "ImpactDmgPB", "KnockbackMagnitude", "RngPB"),
                MidRange = MakeDamageRange(row, "DmgMid", "EnvDmgMid", "ImpactDmgMid", "MidRangeKnockbackMagnitude", "RngMid"),
                LongRange = MakeDamageRange(row, "DmgLong", "EnvDmgLong", "ImpactDmgLong", "LongRangeKnockbackMagnitude", "RngLong"),
                MaxRange = MakeDamageRange(row, "DmgMaxRange", "EnvDmgMaxRange", "ImpactDmgMaxRange", null, "RngMax"),
                DurabilityPerUse = row.GetOrDefault<float>("DurabilityPerUse"),
                BaseCritChance = row.GetOrDefault<float>("DiceCritChance"),
                BaseCritDamage = row.GetOrDefault<float>("DiceCritDamageMultiplier"),
                AmmoCostPerFire = row.GetOrDefault<int>("AmmoCostPerFire"),
                KnockbackZAngle = row.GetOrDefault<float>("KnockbackZAngle"),
                StunTime = row.GetOrDefault<float>("StunTime")
            };

            var durabilityRow = row.GetOrDefault<FName>("DurabilityRowName").Text;
            result.Durability = durabilityTable?[durabilityRow].GetOrDefault<int>(rarity.GetNameText().Text);

            var heatCapacity = row.GetOrDefault<float>("OverheatingMaxValue");
            if (heatCapacity != 0)
            {
                result.Overheat = new OverheatingInfo
                {
                    HeatCapacity = heatCapacity,
                    HeatingRate = row.GetOrDefault<float>("OverheatHeatingValue"),
                    FullChargeHeatingRate = row.GetOrDefault<float>("FullChargeOverheatHeatingValue"),
                    CoolingRate = row.GetOrDefault<float>("OverheatCoolingValue"),
                    OverheatedCoolingRate = row.GetOrDefault<float>("FullyOverheatedCoolingValue"),
                    CooldownDelay = row.GetOrDefault<float>("HeatingCooldownDelay"),
                    OverheatedCooldownDelay = row.GetOrDefault<float>("OverheatedCooldownDelay"),
                };
            }

            var reloadTime = row.GetOrDefault<float>("ReloadTime");
            if (reloadTime != 0)
            {
                result.Reload = new ReloadInfo
                {
                    ReloadTime = reloadTime,
                    ReloadType = row.GetOrDefault<FName>("ReloadType").Text.Replace("EFortWeaponReloadType::", ""),
                    ClipSize = row.GetOrDefault<int>("ClipSize"),
                    InitialClips = row.GetOrDefault<int>("InitialClips"),
                    CartridgePerFire = row.GetOrDefault<int>("CartridgePerFire"),
                };
            }

            var maxChargeTime = row.GetOrDefault<float>("MaxChargeTime");
            if (maxChargeTime != 0)
            {
                result.Charge = new ChargeInfo
                {
                    FullChargeDurabilityPerUse = row.GetOrDefault<float>("FullChargeDurabilityPerUse"),
                    MaxAmmoCostPerFire = row.GetOrDefault<int>("MaxAmmoCostPerFire"),
                    MinChargeTime = row.GetOrDefault<float>("MinChargeTime"),
                    MaxChargeTime = maxChargeTime,
                    ChargeDownTime = row.GetOrDefault<float>("ChargeDownTime"),
                    AutoDischarge = row.GetOrDefault<bool>("bAutoDischarge"),
                    MaxChargeTimeUntilDischarge = row.GetOrDefault<float>("MaxChargeTimeUntilDischarge"),
                    MinChargeDamageMultiplier = row.GetOrDefault<float>("MinChargeDamageMultiplier"),
                    MaxChargeDamageMultiplier = row.GetOrDefault<float>("MaxChargeDamageMultiplier"),
                };
            }

            return result;
        }
        private static DamageRange MakeDamageRange(FStructFallback row, string dmg, string envDmg, string impactDmg, string? knockback, string range)
        {
            return new DamageRange
            {
                Damage = row.GetOrDefault<float>(dmg),
                EnvDamage = row.GetOrDefault<float>(envDmg),
                ImpactDamage = row.GetOrDefault<float>(impactDmg),
                KnockbackMagnitude = knockback == null ? 0 : row.GetOrDefault<float>(knockback),
                Range = row.GetOrDefault<float>(range),
            };
        }
        public static MeleeWeaponStats ConvertMeleeWeaponStats(FStructFallback row, EFortRarity rarity, Dictionary<string, FStructFallback>? durabilityTable)
        {
            var result = new MeleeWeaponStats
            {
                RangeVsEnemies = row.GetOrDefault<float>("RangeVSEnemies"),
                SwingTime = row.GetOrDefault<float>("SwingTime"),
                Damage = row.GetOrDefault<float>("DmgPB"),
                EnvDamage = row.GetOrDefault<float>("EnvDmgPB"),
                ImpactDamage = row.GetOrDefault<float>("ImpactDmgPB"),
                KnockbackMagnitude = row.GetOrDefault<float>("KnockbackMagnitude"),
                KnockbackZAngle = row.GetOrDefault<float>("KnockbackZAngle"),
                DurabilityPerUse = row.GetOrDefault<float>("DurabilityPerUse"),
                BaseCritChance = row.GetOrDefault<float>("DiceCritChance"),
                BaseCritDamage = row.GetOrDefault<float>("DiceCritDamageMultiplier"),
                StunTime = row.GetOrDefault<float>("StunTime"),
            };

            var durabilityRow = row.GetOrDefault<FName>("DurabilityRowName").Text;
            result.Durability = durabilityTable?[durabilityRow].GetOrDefault<int>(rarity.GetNameText().Text);

            return result;
        }
    }
}
