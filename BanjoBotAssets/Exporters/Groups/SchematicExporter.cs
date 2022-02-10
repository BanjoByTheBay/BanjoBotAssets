using BanjoBotAssets.Artifacts;
using BanjoBotAssets.Exporters.Helpers;
using CUE4Parse.UE4.Objects.GameplayTags;

// TODO: export weapon/trap stats
// TODO: export alteration exclusions (e.g. flame grill traps shouldn't have Healing Amount perks)

namespace BanjoBotAssets.Exporters.Groups
{
    internal record ParsedSchematicName(string BaseName, string Rarity, int Tier, string EvoType)
        : BaseParsedItemName(BaseName, Rarity, Tier);

    internal record SchematicItemGroupFields(string DisplayName, string? Description, string? SubType, string AlterationSlotsLoadoutRow)
        : BaseItemGroupFields(DisplayName, Description, SubType)
    {
        public SchematicItemGroupFields() : this("", null, null, "") { }
    }

    internal class SchematicExporter : GroupExporter<UObject, ParsedSchematicName, SchematicItemGroupFields, SchematicItemData>
    {
        private readonly Dictionary<string, string> weaponOrTrapPaths = new(StringComparer.OrdinalIgnoreCase);
        private string? craftingPath, alterationGroupPath, slotDefsPath, slotLoadoutsPath;
        private UDataTable? craftingTable, alterationGroupTable, slotDefsTable, slotLoadoutsTable;

        protected override string Type => "Schematic";

        protected override bool InterestedInAsset(string name)
        {
            // we only export SID_ assets directly, but we also want to keep track of:
            //   WID_ and TID_ assets
            //   CraftingRecipes_New
            //   AlterationGroups
            //   SlotDefs
            //   SlotLoadouts

            if (name.Contains("/WID_") || name.Contains("/TID_"))
                weaponOrTrapPaths.Add(Path.GetFileNameWithoutExtension(name), name);

            if (name.Contains("/CraftingRecipes_New"))
                craftingPath = name;

            if (name.Contains("/AlterationGroups"))
                alterationGroupPath = name;

            if (name.Contains("/SlotDefs"))
                slotDefsPath = name;

            if (name.Contains("/SlotLoadouts"))
                slotLoadoutsPath = name;

            return name.Contains("/SID_") || name.Contains("Schematics/Ammo/Ammo_");
        }

        public override async Task ExportAssetsAsync(IProgress<ExportProgress> progress, IAssetOutput output, CancellationToken cancellationToken)
        {
            craftingTable = await TryLoadTableAsync(craftingPath);
            alterationGroupTable = await TryLoadTableAsync(alterationGroupPath);
            slotDefsTable = await TryLoadTableAsync(slotDefsPath);
            slotLoadoutsTable = await TryLoadTableAsync(slotLoadoutsPath);

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
                logger.LogError(Resources.Warning_SpecificAssetNotFound, "CraftingRecipes_New");
                return null;
            }

            if (!craftingTable.TryGetDataTableRow(craftingRowHandle.RowName.Text,
                StringComparison.OrdinalIgnoreCase, out var craftingRow))
            {
                logger.LogError(Resources.Warning_MissingCraftingTableRow, craftingRowHandle.RowName);
                return null;
            }

            var recipeResults = craftingRow.GetOrDefault<FFortItemQuantityPair[]>("RecipeResults");
            var assetName = recipeResults[0].ItemPrimaryAssetId.PrimaryAssetName.Text;
            if (!weaponOrTrapPaths.TryGetValue(assetName, out var widOrTidPath))
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

            return result with
            {
                Description = description,
                DisplayName = displayName,
                SubType = subType,
                AlterationSlotsLoadoutRow = alterationSlotsLoadoutRow,
            };
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

        protected override Task<bool> ExportAssetAsync(ParsedSchematicName parsed, UObject primaryAsset, SchematicItemGroupFields fields, string path, SchematicItemData itemData)
        {
            itemData.EvoType = parsed.EvoType;

            var rarity = GetRarity(parsed, primaryAsset, fields);

            if (slotLoadoutsTable?.TryGetDataTableRow(fields.AlterationSlotsLoadoutRow, StringComparison.OrdinalIgnoreCase, out var slotLoadout) == true &&
                slotLoadout.GetOrDefault<FStructFallback[]>("AlterationSlots") is FStructFallback[] slots)
            {
                var convertedSlots = new List<AlterationSlot>(slots.Length);

                foreach (var slot in slots)
                {
                    if (slot.GetOrDefault("UnlockRarity", EFortRarity.Uncommon) <= rarity &&
                        ConvertAlterationSlot(slot) is AlterationSlot converted)
                    {
                        convertedSlots.Add(converted);
                    }
                }

                itemData.AlterationSlots = convertedSlots.ToArray();
            }

            return Task.FromResult(true);
        }

        private AlterationSlot? ConvertAlterationSlot(FStructFallback slot)
        {
            if (slotDefsTable == null || alterationGroupTable == null)
                return null;

            var alterationsByRarity = new List<(EFortRarity rarity, string[] alts)>(EFortRarity.Legendary - EFortRarity.Common + 1);

            var slotDefRow = slot.GetOrDefault<FName>("SlotDefinitionRow").Text;

            if (!slotDefsTable.TryGetDataTableRow(slotDefRow, StringComparison.OrdinalIgnoreCase, out var slotDef))
                return null;

            var altGroupRow = slotDef.GetOrDefault<FName>("InitTierGroup").Text;

            if (!alterationGroupTable.TryGetDataTableRow(altGroupRow, StringComparison.OrdinalIgnoreCase, out var altGroup))
                return null;

            var mapping = altGroup.GetOrDefault("RarityMapping", new UScriptMap());

            foreach (var (k, v) in mapping.Properties)
            {
                if (k?.GetValue(typeof(EFortRarity)) is EFortRarity rarity &&
                    v?.GenericValue is UScriptStruct { StructType: FStructFallback weightedAlts })
                {
                    var alts = weightedAlts.GetOrDefault<FStructFallback[]>("WeightData")
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
    }
}
