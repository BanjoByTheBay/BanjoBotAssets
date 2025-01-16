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
using BanjoBotAssets.Exporters.Groups;
using BanjoBotAssets.UExports;
using CUE4Parse.FN.Enums.FortniteGame;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.GameplayTags;
using SevenZip.CommandLineParser;
using System.Collections.Concurrent;
using System.Data;
using System.IO;
using System.Linq;

namespace BanjoBotAssets.Exporters.UObjects
{
    internal sealed partial class SchematicExporter(IExporterContext services) : UObjectExporter<UObject, SchematicItemData>(services)
    {
        private readonly Dictionary<string, string> craftingResultPaths = new(StringComparer.OrdinalIgnoreCase);
        private string? craftingPath, alterationGroupPath, slotDefsPath, slotLoadoutsPath, meleeWeaponsPath, rangedWeaponsPath, trapsPath, durabilityPath, namedExclusionsPath, metaPath;
        private Dictionary<string, FRecipe>? craftingTable;
        private Dictionary<string, FStructFallback>? alterationGroupTable, slotDefsTable, slotLoadoutsTable, meleeWeaponsTable, rangedWeaponsTable, trapsTable, durabilityTable, namedExclusionsTable;

        protected override string Type => "Schematic";
        protected override bool RequireRarity => true;

        [GeneratedRegex("(?<!/Schematics/.*)/(?:WID_|TID_|G_|Ingredient_|AmmoData)[^/]+\\.uasset$", RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
        private static partial Regex CraftingResultNameRegex();

        protected override bool InterestedInAsset(string name)
        {
            // we only export SID_ assets directly, but we also want to keep track of:
            //   Assets that might be crafting results: WID_*, TID_*, G_*, Ingredient_*, AmmoData*
            //   AlterationGroups
            //   CraftingRecipes_New
            //   MeleeWeapons
            //   NamedExclusions
            //   RangedWeapons
            //   SlotDefs
            //   SlotLoadouts
            //   Traps
            //   WeaponDurabilityRarity

            if (CraftingResultNameRegex().IsMatch(name))
                craftingResultPaths.Add(Path.GetFileNameWithoutExtension(name), name);

            switch (Path.GetFileName(name))
            {
                case string s when s.Equals("CraftingRecipes_New.uasset", StringComparison.OrdinalIgnoreCase):
                    craftingPath = name;
                    break;
                case string s when s.Equals("AlterationGroups.uasset", StringComparison.OrdinalIgnoreCase):
                    alterationGroupPath = name;
                    break;
                case string s when s.Equals("SlotDefs.uasset", StringComparison.OrdinalIgnoreCase):
                    slotDefsPath = name;
                    break;
                case string s when s.Equals("SlotLoadouts.uasset", StringComparison.OrdinalIgnoreCase):
                    slotLoadoutsPath = name;
                    break;
                case string s when s.Equals("MeleeWeapons.uasset", StringComparison.OrdinalIgnoreCase):
                    meleeWeaponsPath = name;
                    break;
                case string s when s.Equals("NamedExclusions.uasset", StringComparison.OrdinalIgnoreCase):
                    namedExclusionsPath = name;
                    break;
                case string s when s.Equals("RangedWeapons.uasset", StringComparison.OrdinalIgnoreCase):
                    rangedWeaponsPath = name;
                    break;
                case string s when s.Equals("Traps.uasset", StringComparison.OrdinalIgnoreCase):
                    trapsPath = name;
                    break;
                case string s when s.Equals("WeaponDurabilityRarity.uasset", StringComparison.OrdinalIgnoreCase):
                    durabilityPath = name;
                    break;
                case string s when s.Equals("MetaRecipes.uasset", StringComparison.OrdinalIgnoreCase):
                    metaPath = name;
                    break;
            }

            if (name.Contains("/songs/", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!name.Contains("/SaveTheWorld/", StringComparison.OrdinalIgnoreCase))
                return false;

            return 
                name.Contains("/SID_", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Schematics/Ammo/Ammo_", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Schematics/Misc/", StringComparison.OrdinalIgnoreCase);
        }

        public override async Task ExportAssetsAsync(IProgress<ExportProgress> progress, IAssetOutput output, CancellationToken cancellationToken)
        {
            var craftingTask = TryLoadTableAsync(craftingPath);
            var alterationGroupTask = TryLoadTableAsync(alterationGroupPath);
            var slotDefsTask = TryLoadTableAsync(slotDefsPath);
            var slotLoadoutsTask = TryLoadTableAsync(slotLoadoutsPath);
            var meleeWeaponsTask = TryLoadTableAsync(meleeWeaponsPath);
            var rangedWeaponsTask = TryLoadTableAsync(rangedWeaponsPath);
            var trapsTask = TryLoadTableAsync(trapsPath);
            var durabilityTask = TryLoadTableAsync(durabilityPath);
            var namedExclusionsTask = TryLoadTableAsync(namedExclusionsPath);
            var metaTask = TryLoadTableAsync(metaPath);

            craftingTable = (await craftingTask)?.ToDictionary<FRecipe>();
            alterationGroupTable = (await alterationGroupTask)?.ToDictionary();
            slotDefsTable = (await slotDefsTask)?.ToDictionary();
            slotLoadoutsTable = (await slotLoadoutsTask)?.ToDictionary();
            meleeWeaponsTable = (await meleeWeaponsTask)?.ToDictionary();
            rangedWeaponsTable = (await rangedWeaponsTask)?.ToDictionary();
            trapsTable = (await trapsTask)?.ToDictionary();
            durabilityTable = (await durabilityTask)?.ToDictionary();
            namedExclusionsTable = (await namedExclusionsTask)?.ToDictionary();
            metaRecipeTable = (await metaTask)?.ToDictionary<FRecipe>();

            await base.ExportAssetsAsync(progress, output, cancellationToken);
        }

        protected override async Task<bool> ExportAssetAsync(UObject asset, SchematicItemData itemData, Dictionary<ImageType, string> imagePaths)
        {
            var craftingRowHandle = asset.GetOrDefault<FDataTableRowHandle>("CraftingRecipe");
            if (craftingRowHandle == null)
            {
                logger.LogError(Resources.Warning_NoCraftingRowForSchematic, asset.Name);
                return false;
            }

            var craftingResultItem = await LoadCraftingResultItemAsync(craftingRowHandle);

            if (craftingResultItem == null)
            {
                logger.LogError(Resources.Warning_NoWeaponTrapDefinitionForCraftingRow, craftingRowHandle.RowName.Text);
                return false;
            }

            itemData.DisplayName = craftingResultItem.ItemName?.Text ?? $"<{asset.Name}>";
            itemData.Description = craftingResultItem.ItemDescription?.Text;
            var (category, subType) = CategoryAndSubTypeFromTags(craftingResultItem.GameplayTags);
            itemData.Category = category;
            itemData.SubType = subType;
            if (subType is not null)
            {
                itemData.Tier = (int)craftingResultItem.GetOrDefaultFromDataList<EFortItemTier>("Tier");
                itemData.DisplayTier = craftingResultItem.GetOrDefault<EFortDisplayTier>("DisplayTier").ToString();
                if (itemData.DisplayTier == "Handmade")
                    itemData.Tier = 0;
                if (itemData.DisplayName=="Primal Stink Bow" && itemData.DisplayTier == "Shadowshard")
                    itemData.Tier = 4; // damn you epic with your typos and misclicks
                itemData.TriggerType = craftingResultItem.GetOrDefault<EFortWeaponTriggerType>("TriggerType").ToString();
            }
            if (itemData.Name?.StartsWith("Ammo_", StringComparison.OrdinalIgnoreCase) ?? false)
                itemData.Category = "Ammo";
            else if(itemData.Name?.StartsWith("Ingredient_", StringComparison.OrdinalIgnoreCase) ?? false)
                itemData.Category = "Ingredient";

            EFortRarity rarity = craftingResultItem.GetOrDefault("Rarity", EFortRarity.Uncommon);
            itemData.Rarity = rarity.GetNameText().Text;

            imagePaths.Clear();
            if (craftingResultItem.GetSoftAssetPathFromDataList("Icon") is string smallPreviewPath)
                imagePaths.Add(ImageType.SmallPreview, smallPreviewPath);
            if (craftingResultItem.GetSoftAssetPathFromDataList("LargeIcon") is string largePreviewPath)
                imagePaths.Add(ImageType.LargePreview, largePreviewPath);

            if (asset.Name.Contains("_ore_", StringComparison.OrdinalIgnoreCase))
                itemData.EvoType = "ore";
            if (asset.Name.Contains("_crystal_", StringComparison.OrdinalIgnoreCase))
                itemData.EvoType = "crystal";


            var statRow = craftingResultItem.GetOrDefault<FDataTableRowHandle?>("WeaponStatHandle")?.RowName.Text;
            string? namedWeightRow = null;
            var ammoType = await AmmoTypeFromPathAsync(craftingResultItem.GetOrDefault<FSoftObjectPath>("AmmoData"));
            if(statRow is null)
            {
                logger.LogWarning(Resources.Warning_NoStatsLocatedForSchematicUsingPrefix, asset.Name, "<>");
            }
            else if (rangedWeaponsTable?.TryGetValue(statRow, out var rangedStats) ?? false)
            {
                itemData.RangedWeaponStats = ConvertRangedWeaponStats(rangedStats, ammoType, rarity);
                namedWeightRow ??= rangedStats.GetOrDefault<FName>("NamedWeightRow").Text;
            }
            else if (meleeWeaponsTable?.TryGetValue(statRow, out var meleeStats) ?? false)
            {
                itemData.MeleeWeaponStats = ConvertMeleeWeaponStats(meleeStats, rarity);
                namedWeightRow ??= meleeStats.GetOrDefault<FName>("NamedWeightRow").Text;
            }
            else if (trapsTable?.TryGetValue(statRow, out var trapStats) ?? false)
            {
                itemData.TrapStats = ConvertTrapStats(trapStats, rarity);
                namedWeightRow ??= trapStats.GetOrDefault<FName>("NamedWeightRow").Text;

                // DisplayTier and TriggerType are meaningless for traps, and always the same
                itemData.DisplayTier = null;
                itemData.TriggerType = null;
            }
            else
            {
                logger.LogWarning(Resources.Warning_NoStatsLocatedForSchematicUsingPrefix, asset.Name, "<>");
            }

            var evoHandles = asset.GetOrDefault<FDataTableRowHandle[]>("ConversionRecipes", []);
            var altEvoHandle = evoHandles.Length > 1 ? evoHandles[1] : null;

            if (!(altEvoHandle is null or { RowName.IsNone: true } or { DataTable: null }))
            {
                var recipe = metaRecipeTable?[altEvoHandle.RowName.Text];
                if (recipe is not null)
                    itemData.AlternateTierUpRecipe = ConvertRecipe(recipe);
            }

            if (craftingTable?.TryGetValue(craftingRowHandle.RowName.Text, out var craftingRecipe) ?? false)
            {
                var craftingRecipeData = ConvertRecipe(craftingRecipe);
                itemData.CraftingCost = craftingRecipeData.Cost!;
                var firstResult = craftingRecipeData.Result!.First();
                itemData.CraftingResult = firstResult.Key;
                itemData.CraftingAmount = firstResult.Value == 1 ? null : firstResult.Value;
            }

            var alterationSlotsLoadoutRow = craftingResultItem.GetOrDefault<FName>("AlterationSlotsLoadoutRow").Text;
            if (slotLoadoutsTable?.TryGetValue(alterationSlotsLoadoutRow, out var slotLoadout) == true &&
                slotLoadout.GetOrDefault<FStructFallback[]>("AlterationSlots") is FStructFallback[] slots)
            {
                ISet<string> namedExclusions;
                if (namedWeightRow != null && namedExclusionsTable != null && namedExclusionsTable.TryGetValue(namedWeightRow, out var group))
                {
                    namedExclusions = new HashSet<string>(group.GetOrDefault<string[]>("ExclusionNames"));
                }
                else
                {
                    namedExclusions = new HashSet<string>(0);
                }

                var convertedSlots = new List<AlterationSlot>(slots.Length);

                foreach (var slot in slots)
                {
                    var unlockRarity = slot.GetOrDefault("UnlockRarity", EFortRarity.Uncommon);
                    if (unlockRarity <= rarity &&
                        ConvertAlterationSlot(slot, namedExclusions) is AlterationSlot converted)
                    {
                        convertedSlots.Add(converted);
                    }
                }

                itemData.AlterationSlots = [.. convertedSlots];
            }

            return true;
        }

        private async Task<UFortItemDefinition?> LoadCraftingResultItemAsync(FDataTableRowHandle craftingRowHandle)
        {
            if (craftingTable == null)
            {
                logger.LogError(Resources.Error_SpecificAssetNotFound, "CraftingRecipes_New");
                return null;
            }

            if (!craftingTable.TryGetValue(craftingRowHandle.RowName.Text, out var craftingRow))
            {
                logger.LogError(Resources.Warning_MissingCraftingTableRow, craftingRowHandle.RowName);
                return null;
            }

            var recipeResults = craftingRow.RecipeResults;
            var assetName = recipeResults[0].ItemPrimaryAssetId.PrimaryAssetName.Text;
            if (!craftingResultPaths.TryGetValue(assetName, out var craftResultPath))
            {
                logger.LogWarning(Resources.Warning_UnindexedWeaponTrapPath, assetName);
                return null;
            }
            var craftResultFile = provider[craftResultPath];
            Interlocked.Increment(ref assetsLoaded);
            return await provider.LoadObjectAsync<UFortItemDefinition>(craftResultFile.PathWithoutExtension);
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

        private static (string? category, string? subType) CategoryAndSubTypeFromTags(FGameplayTagContainer tags)
        {
            foreach (var tag in tags.GameplayTags)
            {
                var match = SchematicSubTypeRegex().Match(tag.ToString());

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
                        "ceiling" => (Resources.Field_Recipe_Trap, Resources.Field_Schematic_Ceiling),
                        "floor" => (Resources.Field_Recipe_Trap, Resources.Field_Schematic_Floor),
                        "pistol" => (Resources.Field_Recipe_Ranged, Resources.Field_Schematic_Pistol),
                        "scythe" => (Resources.Field_Recipe_Melee, Resources.Field_Schematic_Scythe),
                        "shotgun" => (Resources.Field_Recipe_Ranged, Resources.Field_Schematic_Shotgun),
                        "sniper" => (Resources.Field_Recipe_Ranged, Resources.Field_Schematic_Sniper),
                        "spear" => (Resources.Field_Recipe_Melee, Resources.Field_Schematic_Spear),
                        "sword" => (Resources.Field_Recipe_Melee, Resources.Field_Schematic_Sword),
                        "wall" => (Resources.Field_Recipe_Trap, Resources.Field_Schematic_Wall),
                        _ => (null, null),
                    };
                }
            }

            return (null, null);
        }
        [GeneratedRegex(@"^(?:Weapon\.(?:Ranged|Melee\.(?:Edged|Blunt|Piercing))|Trap(?=\.(?:Ceiling|Floor|Wall)))\.([^.]+)", RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
        private static partial Regex SchematicSubTypeRegex();

        private RangedWeaponStats ConvertRangedWeaponStats(FStructFallback row, string? ammoType, EFortRarity rarity)
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
        private MeleeWeaponStats ConvertMeleeWeaponStats(FStructFallback row, EFortRarity rarity)
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
        private TrapStats ConvertTrapStats(FStructFallback row, EFortRarity rarity)
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

        private AlterationSlot? ConvertAlterationSlot(FStructFallback slot, ISet<string> namedExclusions)
        {
            if (slotDefsTable == null || alterationGroupTable == null)
                return null;

            var alterationsByRarity = new List<(EFortRarity rarity, string[] alts)>(EFortRarity.Legendary - EFortRarity.Common + 1);

            var slotDefRow = slot.GetOrDefault<FName>("SlotDefinitionRow").Text;

            if (!slotDefsTable.TryGetValue(slotDefRow, out var slotDef))
                return null;

            var altGroupRow = slotDef.GetOrDefault<FName>("InitTierGroup").Text;

            var respecCost = slotDef.GetOrDefault<FFortItemQuantityPair[]>("BaseRespecCosts")?.ToDictionary(
                    p => $"{p.ItemPrimaryAssetId.PrimaryAssetType.Name.Text}:{p.ItemPrimaryAssetId.PrimaryAssetName.Text}",
                    p => p.Quantity,
                    StringComparer.OrdinalIgnoreCase
                );

            if (!alterationGroupTable.TryGetValue(altGroupRow, out var altGroup))
                return null;

            var mapping = altGroup.GetOrDefault("RarityMapping", new UScriptMap());

            foreach (var (k, v) in mapping.Properties)
            {
                if (k?.GetValue(typeof(EFortRarity)) is EFortRarity rarity &&
                    v?.GenericValue is UScriptStruct { StructType: FStructFallback weightedAlts })
                {
                    var alts = weightedAlts.GetOrDefault<FStructFallback[]>("WeightData")
                        .Where(wd => !namedExclusions.Overlaps(wd.GetOrDefault<string[]>("ExclusionNames")))
                        .Select(wd => wd.GetOrDefault<string>("AID"))
                        .ToArray();
                    alterationsByRarity.Add((rarity, alts));
                }
            }

            return new AlterationSlot
            {
                RequiredLevel = slot.GetOrDefault<int>("UnlockLevel"),
                Alterations = alterationsByRarity.OrderBy(abr => abr.rarity).Select(abr => abr.alts).ToArray(),
                BaseRespecCost = respecCost?.Count == 0 ? null : respecCost
            };
        }
    }
}
