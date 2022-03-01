using BanjoBotAssets.Artifacts;
using BanjoBotAssets.Exporters.Helpers;
using BanjoBotAssets.Extensions;
using CUE4Parse.UE4.Objects.GameplayTags;
using System.Collections.Concurrent;

namespace BanjoBotAssets.Exporters.Groups
{
    internal record ParsedSchematicName(string BaseName, string Rarity, int Tier, string EvoType)
        : BaseParsedItemName(BaseName, Rarity, Tier);

    internal record SchematicItemGroupFields(string DisplayName, string? Description, string? SubType, string AlterationSlotsLoadoutRow,
        string? AmmoType, string? WeaponOrTrapStatRowPrefix)
        : BaseItemGroupFields(DisplayName, Description, SubType)
    {
        public SchematicItemGroupFields() : this("", null, null, "", "", "") { }
    }

    internal class SchematicExporter : GroupExporter<UObject, ParsedSchematicName, SchematicItemGroupFields, SchematicItemData>
    {
        private readonly Dictionary<string, string> craftingResultPaths = new(StringComparer.OrdinalIgnoreCase);
        private string? craftingPath, alterationGroupPath, slotDefsPath, slotLoadoutsPath, meleeWeaponsPath, rangedWeaponsPath, trapsPath, durabilityPath, namedExclusionsPath;
        private Dictionary<string, FStructFallback>? craftingTable, alterationGroupTable, slotDefsTable, slotLoadoutsTable, meleeWeaponsTable, rangedWeaponsTable, trapsTable, durabilityTable, namedExclusionsTable;

        protected override string Type => "Schematic";

        //private static readonly Regex craftingResultNameRegex = new("/Items(?!.*/Schematics/)(?:/.*)?/(?:WID_|TID_|G_|Ingredient_|AmmoData)[^/]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex craftingResultNameRegex = new(@"(?<!/Schematics/.*)/(?:WID_|TID_|G_|Ingredient_|AmmoData)[^/]+\.uasset$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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

            if (craftingResultNameRegex.IsMatch(name))
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

            return name.Contains("/SID_", StringComparison.OrdinalIgnoreCase) || name.Contains("Schematics/Ammo/Ammo_", StringComparison.OrdinalIgnoreCase);
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

            craftingTable = (await craftingTask)?.ToDictionary();
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

        private async Task<UDataTable?> TryLoadTableAsync(string? path)
        {
            if (path == null)
                return null;

            var file = provider[path];
            Interlocked.Increment(ref assetsLoaded);
            return await provider.LoadObjectAsync<UDataTable>(file.PathWithoutExtension);
        }

        private static readonly Regex schematicAssetNameRegex = new(@".*/([^/]+?)(?:_(C|UC|R|VR|SR|UR))?(?:_(Ore|Crystal))?(?:_?T(\d+))?(?:\..*)?$", RegexOptions.IgnoreCase);

        protected override ParsedSchematicName? ParseAssetName(string name)
        {
            var match = schematicAssetNameRegex.Match(name);

            if (!match.Success)
            {
                logger.LogError(Resources.Warning_CannotParseSchematicName, name);
                return null;
            }

            return new ParsedSchematicName(
                BaseName: match.Groups[1].Value,
                Rarity: match.Groups[2].Value,
                Tier: match.Groups[4].Success ? int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture) : 0,
                EvoType: match.Groups[3].Value);
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

            var recipeResults = craftingRow.GetOrDefault<FFortItemQuantityPair[]>("RecipeResults");
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

            var displayName = weaponOrTrapDef.DisplayName?.Text ?? $"<{grouping.Key}>";
            var description = weaponOrTrapDef.Description?.Text;
            var subType = SubTypeFromTags(weaponOrTrapDef.GameplayTags);
            var alterationSlotsLoadoutRow = weaponOrTrapDef.GetOrDefault<FName>("AlterationSlotsLoadoutRow").Text;
            var ammoType = await AmmoTypeFromPathAsync(weaponOrTrapDef.GetOrDefault<FSoftObjectPath>("AmmoData"));
            var statRowPrefix = GetStatRowPrefix(weaponOrTrapDef);

            return result with
            {
                Description = description,
                DisplayName = displayName,
                SubType = subType,
                AlterationSlotsLoadoutRow = alterationSlotsLoadoutRow,
                AmmoType = ammoType,
                WeaponOrTrapStatRowPrefix = statRowPrefix,
            };
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
                return String.Join('_', parts[..^2]);
            }

            return null;
        }

        private static readonly Regex schematicSubTypeRegex = new(@"^(?:Weapon\.(?:Ranged|Melee\.(?:Edged|Blunt|Piercing))|Trap(?=\.(?:Ceiling|Floor|Wall)))\.([^.]+)", RegexOptions.IgnoreCase);

        public SchematicExporter(IExporterContext services) : base(services)
        {
        }

        private static string SubTypeFromTags(FGameplayTagContainer tags)
        {
            foreach (var tag in tags.GameplayTags)
            {
                var match = schematicSubTypeRegex.Match(tag.Text);

                if (match.Success)
                {
                    return match.Groups[1].Value.ToLower(CultureInfo.InvariantCulture) switch
                    {
                        "hammer" => Resources.Field_Schematic_Hardware,
                        "heavy" => Resources.Field_Schematic_Explosive,
                        "improvised" => Resources.Field_Schematic_Club,
                        "smg" => Resources.Field_Schematic_SMG,
                        "assault" => Resources.Field_Schematic_Assault,
                        "axe" => Resources.Field_Schematic_Axe,
                        "ceiling" => Resources.Field_Schematic_Ceiling,
                        "floor" => Resources.Field_Schematic_Floor,
                        "pistol" => Resources.Field_Schematic_Pistol,
                        "scythe" => Resources.Field_Schematic_Scythe,
                        "shotgun" => Resources.Field_Schematic_Shotgun,
                        "sniper" => Resources.Field_Schematic_Sniper,
                        "spear" => Resources.Field_Schematic_Spear,
                        "sword" => Resources.Field_Schematic_Sword,
                        "wall" => Resources.Field_Schematic_Wall,
                        _ => Resources.Field_Schematic_Unknown,
                    };
                }
            }

            return Resources.Field_Schematic_Unknown;
        }

        private readonly ConcurrentDictionary<string, Task<string?>> cachedAmmoTypesFromPaths = new();

        private async Task<string?> AmmoTypeFromPathAsync(FSoftObjectPath ammoDataPath)
        {
            if (ammoDataPath.AssetPathName.IsNone)
                return null;

            return await cachedAmmoTypesFromPaths.GetOrAdd(ammoDataPath.AssetPathName.Text, static async (path, provider) =>
            {
                var asset = await provider.LoadObjectAsync<UFortAmmoItemDefinition>(path);
                if (asset.DisplayName?.Text is string str)
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

            var rarity = GetRarity(parsed, primaryAsset, fields);
            string? namedWeightRow = null;

            if (fields.WeaponOrTrapStatRowPrefix is string prefix)
            {
                if (rangedWeaponsTable != null)
                {
                    var weaponStatRow = $"{prefix}_{parsed.Rarity}_{parsed.EvoType}_T{parsed.Tier:00}";
                    if (rangedWeaponsTable.TryGetValue(weaponStatRow, out var weaponStats))
                    {
                        itemData.RangedWeaponStats = ConvertRangedWeaponStats(weaponStats, fields, rarity);
                        namedWeightRow ??= weaponStats.GetOrDefault<FName>("NamedWeightRow").Text;
                    }
                }

                if (meleeWeaponsTable != null)
                {
                    var weaponStatRow = $"{prefix}_{parsed.Rarity}_{parsed.EvoType}_T{parsed.Tier:00}";
                    if (meleeWeaponsTable.TryGetValue(weaponStatRow, out var weaponStats))
                    {
                        itemData.MeleeWeaponStats = ConvertMeleeWeaponStats(weaponStats, rarity);
                        namedWeightRow ??= weaponStats.GetOrDefault<FName>("NamedWeightRow").Text;
                    }
                }

                if (trapsTable != null)
                {
                    var trapStatRow = $"{prefix}_{parsed.Rarity}_T{parsed.Tier:00}";
                    if (trapsTable.TryGetValue(trapStatRow, out var trapStats))
                    {
                        itemData.TrapStats = ConvertTrapStats(trapStats, rarity);
                        namedWeightRow ??= trapStats.GetOrDefault<FName>("NamedWeightRow").Text;
                    }
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

                itemData.AlterationSlots = convertedSlots.ToArray();
            }

            return Task.FromResult(true);
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
