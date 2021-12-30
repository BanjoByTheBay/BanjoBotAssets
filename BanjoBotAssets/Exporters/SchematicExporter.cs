using CUE4Parse.UE4.Objects.GameplayTags;
using System.Text;

// TODO: support traps
// TODO: export alteration possibilities

namespace BanjoBotAssets.Exporters
{
    internal record ParsedSchematicName(string BaseName, string Rarity, int Tier, string EvoType)
        : BaseParsedItemName(BaseName, Rarity, Tier);
    
    internal class SchematicExporter : GroupExporter<UObject, ParsedSchematicName, BaseItemGroupFields, NamedItemData>
    {
        private readonly Dictionary<string, string> weaponPaths = new(StringComparer.OrdinalIgnoreCase);
        private string? craftingPath;
        private UDataTable? craftingTable;

        public SchematicExporter(DefaultFileProvider provider) : base(provider)
        {
        }

        protected override string Type => "Schematic";

        protected override bool InterestedInAsset(string name)
        {
            // we only export SID_ assets directly, but we also want to keep track
            // of the WID_ and TID_ assets, and CraftingRecipes_New

            if (name.Contains("/WID_") || name.Contains("/TID_"))
                weaponPaths.Add(Path.GetFileNameWithoutExtension(name), name);

            if (name.Contains("/CraftingRecipes_New"))
                craftingPath = name;

            return name.Contains("/SID_");
        }

        public override async Task ExportAssetsAsync(IProgress<ExportProgress> progress, IAssetOutput output)
        {
            if (craftingPath != null)
            {
                var file = provider[craftingPath];
                Interlocked.Increment(ref assetsLoaded);
                craftingTable = await provider.LoadObjectAsync<UDataTable>(file.PathWithoutExtension);
            }

            await base.ExportAssetsAsync(progress, output);
        }

        private static readonly Regex schematicAssetNameRegex = new(@".*/([^/]+?)(?:_(C|UC|R|VR|SR|UR))?(?:_(Ore|Crystal))?(?:_?T(\d+))?(?:\..*)?$", RegexOptions.IgnoreCase);
        
        protected override ParsedSchematicName? ParseAssetName(string name)
        {
            var match = schematicAssetNameRegex.Match(name);

            if (!match.Success)
            {
                Console.WriteLine("WARNING: Can't parse schematic name: {0}", name);
                return null;
            }

            return new ParsedSchematicName(
                BaseName: match.Groups[1].Value,
                Rarity: match.Groups[2].Value,
                Tier: match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : 0,
                EvoType: match.Groups[3].Value);
        }

        private async Task<UFortItemDefinition?> LoadWeaponDefinitionAsync(FDataTableRowHandle craftingRowHandle)
        {
            if (craftingTable == null)
            {
                Console.WriteLine("WARNING: No crafting table");
                return null;
            }

            if (!craftingTable.TryGetDataTableRow(craftingRowHandle.RowName.Text,
                StringComparison.OrdinalIgnoreCase, out var craftingRow))
            {
                Console.WriteLine("WARNING: Can't find crafting row {0}", craftingRowHandle.RowName);
                return null;
            }

            var recipeResults = craftingRow.GetOrDefault<FFortItemQuantityPair[]>("RecipeResults");
            var assetName = recipeResults[0].ItemPrimaryAssetId.PrimaryAssetName.Text;
            if (!weaponPaths.TryGetValue(assetName, out var widPath))
            {
                Console.WriteLine("WARNING: No weapon path indexed for {0}", assetName);
                return null;
            }
            var widFile = provider[widPath];
            Interlocked.Increment(ref assetsLoaded);
            return await provider.LoadObjectAsync<UFortItemDefinition>(widFile.PathWithoutExtension);
        }

        private static readonly Regex schematicSubTypeRegex = new(@"^(?:Weapon\.(?:Ranged|Melee\.(?:Edged|Blunt|Piercing))|Trap(?=\.(?:Ceiling|Floor|Wall)))\.([^.]+)", RegexOptions.IgnoreCase);

        protected override async Task<BaseItemGroupFields> ExtractCommonFieldsAsync(UObject asset, IGrouping<string?, string> grouping)
        {
            var result = await base.ExtractCommonFieldsAsync(asset, grouping);

            var craftingRow = asset.GetOrDefault<FDataTableRowHandle>("CraftingRecipe");
            if (craftingRow == null)
            {
                Console.WriteLine("WARNING: No crafting row listed for schematic {0}", asset.Name);
                return result;
            }

            Interlocked.Increment(ref assetsLoaded);
            var weaponDef = await LoadWeaponDefinitionAsync(craftingRow);

            if (weaponDef == null)
            {
                Console.WriteLine("WARNING: No weapon definition for crafting row {0}", craftingRow.RowName);
                return result;
            }

            var displayName = weaponDef.DisplayName?.Text ?? $"<{grouping.Key}>";
            var description = weaponDef.Description?.Text;
            var subType = SubTypeFromTags(weaponDef.GameplayTags);

            return result with
            {
                Description = description,
                DisplayName = displayName,
                SubType = subType,
            };
        }

        private static string SubTypeFromTags(FGameplayTagContainer tags)
        {
            foreach (var tag in tags.GameplayTags)
            {
                var match = schematicSubTypeRegex.Match(tag.Text);

                if (match.Success)
                {
                    switch (match.Groups[1].Value.ToLower())
                    {
                        case "hammer":
                            return "Hardware";

                        case "heavy":
                            return "Launcher";

                        case "improvised":
                            return "Club";

                        case "smg":
                            return "SMG";

                        default:
                            var sb = new StringBuilder(match.Groups[1].Value);

                            sb[0] = char.ToUpper(sb[0]);

                            for (int i = 1; i < sb.Length; i++)
                                sb[i] = char.ToLower(sb[i]);

                            return sb.ToString();
                    }
                }
            }

            return "Unknown";
        }
    }
}
