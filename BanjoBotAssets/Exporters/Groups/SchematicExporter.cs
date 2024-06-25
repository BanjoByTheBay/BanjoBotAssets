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

namespace BanjoBotAssets.Exporters.Groups
{
    internal sealed record ParsedSchematicName(string BaseName, string Rarity, int Tier, string EvoType)
        : BaseParsedItemName(BaseName, Rarity, Tier);

    internal sealed record SchematicItemGroupFields(string DisplayName, string? Description, string? SubType, string AlterationSlotsLoadoutRow,
        string? AmmoType, string? WeaponOrTrapStatRowPrefix, string? CraftingRowPrefix, string? TriggerType, string? Category)
        : BaseItemGroupFields(DisplayName, Description, SubType)
    {
        public SchematicItemGroupFields() : this("", null, null, "", "", "", "", "", "") { }
    }

    internal sealed partial class SchematicExporter(IExporterContext services) : GroupExporter<UObject, ParsedSchematicName, SchematicItemGroupFields, SchematicItemData>(services)
    {
        private readonly Dictionary<string, string> craftingResultPaths = new(StringComparer.OrdinalIgnoreCase);
        private string? craftingPath, alterationGroupPath, slotDefsPath, slotLoadoutsPath, meleeWeaponsPath, rangedWeaponsPath, trapsPath, durabilityPath, namedExclusionsPath;
        private Dictionary<string, FRecipe>? craftingTable;
        private Dictionary<string, FStructFallback>? alterationGroupTable, slotDefsTable, slotLoadoutsTable, meleeWeaponsTable, rangedWeaponsTable, trapsTable, durabilityTable, namedExclusionsTable;

        protected override string Type => "Schematic";

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
            }

            if (name.Contains("/songs/", StringComparison.OrdinalIgnoreCase))
                return false;

            return name.Contains("/SID_", StringComparison.OrdinalIgnoreCase) || name.Contains("Schematics/Ammo/Ammo_", StringComparison.OrdinalIgnoreCase);
        }

        protected override string SelectPrimaryAsset(IGrouping<string?, string> assetGroup)
        {
            /* Select the first schematic that:
             *   (1) Isn't crystal, because some crystal schematics are invalid (yet still exist)
             *   (2) Doesn't have an unusual tier number
             */
            return assetGroup.FirstOrDefault(p =>
            {
                ParsedSchematicName? parsed = ParseAssetName(p);
                return parsed?.EvoType.Equals("Crystal", StringComparison.OrdinalIgnoreCase) == false &&
                       parsed.Tier >= 1 && parsed.Tier <= 5;
            }) ?? assetGroup.First();
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

            craftingTable = (await craftingTask)?.ToDictionary<FRecipe>();
            alterationGroupTable = (await alterationGroupTask)?.ToDictionary();
            slotDefsTable = (await slotDefsTask)?.ToDictionary();
            slotLoadoutsTable = (await slotLoadoutsTask)?.ToDictionary();
            meleeWeaponsTable = (await meleeWeaponsTask)?.ToDictionary();
            rangedWeaponsTable = (await rangedWeaponsTask)?.ToDictionary();
            trapsTable = (await trapsTask)?.ToDictionary();
            durabilityTable = (await durabilityTask)?.ToDictionary();
            namedExclusionsTable = (await namedExclusionsTask)?.ToDictionary();

            await base.ExportAssetsAsync(progress, output, cancellationToken);
        }

        /* Rarity is included in "key" so groups will be restricted to a single rarity.
         * For example, the Ski Cleaver and Claxe have the same filename pattern, and non-overlapping rarities,
         * but they must be considered separate weapons because they have separate stats rows. (They
         * also live in separate directories.)
         *
         * The epic and legendary versions of the Walloper also have separate stats rows, but otherwise they're
         * indistinguishable from other schematics.*/
        [GeneratedRegex(
    """
            ^
            (?<key>.+?_(?<rarity>C|UC|R|VR|SR|UR))?     # key includes path and rarity
            (?:_(?<evotype>Ore|Crystal))?               # evolution type for tier 4+
            (?:_?T(?<tier>\d+))?                        # tier
            (?:\.[^/]*)?                                # (ignored) extension
            $
            """, RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace)]
        private static partial Regex SchematicAssetNameRegex();

        protected override ParsedSchematicName? ParseAssetName(string name)
        {
            var match = SchematicAssetNameRegex().Match(name);

            if (!match.Success)
            {
                logger.LogError(Resources.Warning_CannotParseSchematicName, name);
                return null;
            }

            return new ParsedSchematicName(
                BaseName: match.Groups["key"].Value,
                Rarity: match.Groups["rarity"].Value,
                Tier: match.Groups["tier"].Success ? int.Parse(match.Groups["tier"].Value, CultureInfo.InvariantCulture) : 0,
                EvoType: match.Groups["evotype"].Value);
        }

        private async Task<UFortItemDefinition?> LoadWeaponOrTrapDefinitionAsync(FDataTableRowHandle craftingRowHandle)
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
            if (!craftingResultPaths.TryGetValue(assetName, out var widOrTidPath))
            {
                logger.LogWarning(Resources.Warning_UnindexedWeaponTrapPath, assetName);
                return null;
            }
            var widOrTidFile = provider[widOrTidPath];
            Interlocked.Increment(ref assetsLoaded);
            return await provider.LoadObjectAsync<UFortItemDefinition>(widOrTidFile.PathWithoutExtension);
        }

        protected override async Task<SchematicItemGroupFields> ExtractCommonFieldsAsync(UObject asset, IGrouping<string?, string> grouping)
        {
            var result = await base.ExtractCommonFieldsAsync(asset, grouping);

            var craftingRow = asset.GetOrDefault<FDataTableRowHandle>("CraftingRecipe");
            if (craftingRow == null)
            {
                logger.LogError(Resources.Warning_NoCraftingRowForSchematic, asset.Name);
                return result;
            }

            var weaponOrTrapDef = await LoadWeaponOrTrapDefinitionAsync(craftingRow);

            if (weaponOrTrapDef == null)
            {
                logger.LogError(Resources.Warning_NoWeaponTrapDefinitionForCraftingRow, craftingRow.RowName);
                return result;
            }

            var displayName = weaponOrTrapDef.ItemName?.Text ?? $"<{grouping.Key}>";
            var description = weaponOrTrapDef.ItemDescription?.Text;
            var (category, subType) = CategoryAndSubTypeFromTags(weaponOrTrapDef.GameplayTags);
            var alterationSlotsLoadoutRow = weaponOrTrapDef.GetOrDefault<FName>("AlterationSlotsLoadoutRow").Text;
            var ammoType = await AmmoTypeFromPathAsync(weaponOrTrapDef.GetOrDefault<FSoftObjectPath>("AmmoData"));
            var statRowPrefix = GetStatRowPrefix(weaponOrTrapDef);
            var craftingRowPrefix = GetCraftingRowPrefix(craftingRow.RowName.Text);
            var triggerType = weaponOrTrapDef.GetOrDefault<EFortWeaponTriggerType>("TriggerType").ToString();

            return result with
            {
                Description = description,
                DisplayName = displayName,
                Category = category,
                SubType = subType,
                AlterationSlotsLoadoutRow = alterationSlotsLoadoutRow,
                AmmoType = ammoType,
                WeaponOrTrapStatRowPrefix = statRowPrefix,
                CraftingRowPrefix = craftingRowPrefix,
                TriggerType = triggerType,
                SmallPreviewImagePath = weaponOrTrapDef.GetSoftAssetPathFromDataList("Icon"),
                LargePreviewImagePath = weaponOrTrapDef.GetSoftAssetPathFromDataList("LargeIcon"),
            };
        }

        /**
         * For mythic schematics, we want to use the display rarity stored in a property (mythic)
         * instead of the rarity parsed from the asset filename (legendary). But we don't want to
         * load every asset to read its properties.
         *
         * Instead, we rely on the knowledge that mythic items don't occur in any other rarities.
         *
         * We use a similar technique here and in <see cref="HeroExporter"/>: ensuring the primary asset has
         * "sr" parsed rarity in <see cref="SelectPrimaryAsset"/>, and checking the primary asset's
         * display rarity in <see cref="GetRarity"/>.
         */

        protected override EFortRarity GetRarity(ParsedSchematicName parsedName, UObject primaryAsset, SchematicItemGroupFields fields)
        {
            // SR weapons can be legendary or mythic
            if (parsedName.Rarity.Equals("SR", StringComparison.OrdinalIgnoreCase) && primaryAsset.GetOrDefault("Rarity", EFortRarity.Uncommon) == EFortRarity.Mythic)
                return EFortRarity.Mythic;

            return base.GetRarity(parsedName, primaryAsset, fields);
        }

        private static string? GetStatRowPrefix(UFortItemDefinition weaponOrTrapDef)
        {
            var rowName = weaponOrTrapDef.GetOrDefault<FDataTableRowHandle?>("WeaponStatHandle")?.RowName.Text;

            if (rowName == null)
                return null;

            if (weaponOrTrapDef.Name.StartsWith("WID_", StringComparison.OrdinalIgnoreCase))
            {
                var parts = rowName.Split('_');
                return string.Join('_', parts[..^3]);
            }

            if (weaponOrTrapDef.Name.StartsWith("TID_", StringComparison.OrdinalIgnoreCase))
            {
                var parts = rowName.Split('_');
                return string.Join('_', parts[..^2]);
            }

            return null;
        }

        private static string? GetCraftingRowPrefix(string rowName)
        {
            if (rowName.StartsWith("Ranged.", StringComparison.OrdinalIgnoreCase) || rowName.StartsWith("Melee.", StringComparison.OrdinalIgnoreCase))
            {
                var parts = rowName.Split('.');
                return string.Join('.', parts[..^3]);
            }

            if (rowName.StartsWith("Trap.", StringComparison.OrdinalIgnoreCase))
            {
                var parts = rowName.Split('.');
                return string.Join('.', parts[..^2]);
            }

            return rowName;
        }

        [GeneratedRegex(@"^(?:Weapon\.(?:Ranged|Melee\.(?:Edged|Blunt|Piercing))|Trap(?=\.(?:Ceiling|Floor|Wall)))\.([^.]+)", RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
        private static partial Regex SchematicSubTypeRegex();

        private static (string category, string subType) CategoryAndSubTypeFromTags(FGameplayTagContainer tags)
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
                        _ => ("", Resources.Field_Schematic_Unknown),
                    };
                }
            }

            return ("", Resources.Field_Schematic_Unknown);
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

        protected override Task<bool> ExportAssetAsync(ParsedSchematicName parsed, UObject primaryAsset, SchematicItemGroupFields fields, string path, SchematicItemData itemData)
        {
            itemData.EvoType = parsed.EvoType;
            itemData.Category = fields.Category;
            itemData.DisplayTier = GetDisplayTier(parsed.Tier, parsed.EvoType);
            itemData.TriggerType = fields.TriggerType;

            var rarity = GetRarity(parsed, primaryAsset, fields);
            string? namedWeightRow = null;

            // use rarity.ToShortString() instead of parsed.Rarity to handle mythics correctly

            if (fields.WeaponOrTrapStatRowPrefix is string prefix)
            {
                if (rangedWeaponsTable != null)
                {
                    var weaponStatRow = $"{prefix}_{rarity.ToShortString()}_{parsed.EvoType}_T{parsed.Tier:00}";
                    if (rangedWeaponsTable.TryGetValue(weaponStatRow, out var weaponStats))
                    {
                        itemData.RangedWeaponStats = ConvertRangedWeaponStats(weaponStats, fields, rarity);
                        namedWeightRow ??= weaponStats.GetOrDefault<FName>("NamedWeightRow").Text;
                    }
                }

                if (meleeWeaponsTable != null)
                {
                    var weaponStatRow = $"{prefix}_{rarity.ToShortString()}_{parsed.EvoType}_T{parsed.Tier:00}";
                    if (meleeWeaponsTable.TryGetValue(weaponStatRow, out var weaponStats))
                    {
                        itemData.MeleeWeaponStats = ConvertMeleeWeaponStats(weaponStats, rarity);
                        namedWeightRow ??= weaponStats.GetOrDefault<FName>("NamedWeightRow").Text;
                    }
                }

                if (trapsTable != null)
                {
                    var trapStatRow = $"{prefix}_{rarity.ToShortString()}_T{parsed.Tier:00}";
                    if (trapsTable.TryGetValue(trapStatRow, out var trapStats))
                    {
                        itemData.TrapStats = ConvertTrapStats(trapStats, rarity);
                        namedWeightRow ??= trapStats.GetOrDefault<FName>("NamedWeightRow").Text;

                        // DisplayTier and TriggerType are meaningless for traps, and always the same
                        itemData.DisplayTier = "";
                        itemData.TriggerType = "";
                    }
                }

                if (itemData.RangedWeaponStats == null && itemData.MeleeWeaponStats == null && itemData.TrapStats == null)
                {
                    logger.LogWarning(Resources.Warning_NoStatsLocatedForSchematicUsingPrefix, Path.GetFileNameWithoutExtension(path), prefix);
                }
            }

            if (fields.CraftingRowPrefix is string craftPrefix && craftingTable != null)
            {
                // mythic weapons are SR instead of UR in the crafting table
                string craftRow;
                if (craftPrefix.StartsWith("Ranged.", StringComparison.OrdinalIgnoreCase) || craftPrefix.StartsWith("Melee.", StringComparison.OrdinalIgnoreCase))
                {
                    craftRow = $"{craftPrefix}.{parsed.Rarity}.{parsed.EvoType}.T{parsed.Tier:00}";
                }
                else if (craftPrefix.StartsWith("Trap.", StringComparison.OrdinalIgnoreCase))
                {
                    craftRow = $"{craftPrefix}.{parsed.Rarity}.T{parsed.Tier:00}";
                }
                else
                {
                    craftRow = craftPrefix;
                }

                if (craftingTable.TryGetValue(craftRow, out var recipe))
                {
                    itemData.CraftingCost = ConvertCraftingCost(recipe);
                }
            }

            if (slotLoadoutsTable?.TryGetValue(fields.AlterationSlotsLoadoutRow, out var slotLoadout) == true &&
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
                    if (slot.GetOrDefault("UnlockRarity", EFortRarity.Uncommon) <= rarity &&
                        ConvertAlterationSlot(slot, namedExclusions) is AlterationSlot converted)
                    {
                        convertedSlots.Add(converted);
                    }
                }

                itemData.AlterationSlots = [.. convertedSlots];
            }

            return Task.FromResult(true);
        }

        private static string? GetDisplayTier(int tier, string? evoType) => (tier, evoType) switch
        {
            (_, "" or null) => null,

            (0, _) => Resources.Field_Schematic_Handmade,
            (1, _) => Resources.Field_Schematic_Copper,
            (2, _) => Resources.Field_Schematic_Silver,
            (3, _) => Resources.Field_Schematic_Malachite,
            (4, "ore") => Resources.Field_Schematic_Obsidian,
            (4, "crystal") => Resources.Field_Schematic_Shadowshard,
            (5, "ore") => Resources.Field_Schematic_Brightcore,
            (5, "crystal") => Resources.Field_Schematic_Sunbeam,

            _ => Resources.Field_Schematic_Invalid,
        };

        private static Dictionary<string, int> ConvertCraftingCost(FRecipe recipe)
        {
            return recipe.RecipeCosts.ToDictionary(
                p => $"{p.ItemPrimaryAssetId.PrimaryAssetType.Name.Text}:{p.ItemPrimaryAssetId.PrimaryAssetName.Text}",
                p => p.Quantity,
                StringComparer.OrdinalIgnoreCase);
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
            };
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

        private RangedWeaponStats ConvertRangedWeaponStats(FStructFallback row, SchematicItemGroupFields fields, EFortRarity rarity)
        {
            var result = new RangedWeaponStats
            {
                AmmoType = fields.AmmoType,
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
    }
}
