using BanjoBotAssets.Artifacts.Helpers;
using BanjoBotAssets.Artifacts.Models;
using BanjoBotAssets.Config;
using Newtonsoft.Json;

namespace BanjoBotAssets.Artifacts
{
    internal sealed class SchematicsJsonArtifact : IExportArtifact
    {
        private readonly ExportedFileOptions options;
        private readonly ILogger<SchematicsJsonArtifact> logger;
        private readonly NullToEmptyStringContractResolver contractResolver;

        public SchematicsJsonArtifact(ExportedFileOptions options, ILogger<SchematicsJsonArtifact> logger, NullToEmptyStringContractResolver contractResolver)
        {
            this.options = options;
            this.logger = logger;
            this.contractResolver = contractResolver;
        }

        public Task RunAsync(ExportedAssets exportedAssets, IList<ExportedRecipe> exportedRecipes, CancellationToken cancellationToken = default)
        {
            var recipesToExclude = new Stack<int>();

            cancellationToken.ThrowIfCancellationRequested();

            for (int i = 0; i < exportedRecipes.Count; i++)
            {
                var recipe = exportedRecipes[i];

                if (recipe.ItemName == null)
                {
                    recipesToExclude.Push(i);
                    continue;
                }

                // change schematic ID to display name and fill in other fields
                if (!exportedAssets.NamedItems.TryGetValue(recipe.ItemName, out var itemData) || itemData is not SchematicItemData schematic)
                {
                    logger.LogDebug(Resources.Warning_UnmatchedCraftingRecipe, recipe.ItemName);
                    recipesToExclude.Push(i);
                    continue;
                }

                recipe.ItemName = schematic.DisplayName ?? "";
                recipe.Type = schematic.Category ?? "";
                recipe.Subtype = schematic.SubType ?? "";
                recipe.Rarity = schematic.Rarity ?? "";
                recipe.Tier = schematic.Tier ?? 0;

                if (schematic is SchematicItemData { EvoType: string evoType })
                {
                    recipe.Material = evoType.ToLower(CultureInfo.InvariantCulture) switch
                    {
                        "ore" => Resources.Field_Schematic_Ore,
                        "crystal" => Resources.Field_Schematic_Crystal,
                        "" => "",
                        _ => $"<{evoType}>",
                    };
                }

                // change ingredient IDs to display names
                if (recipe.Ingredient1 != null)
                    recipe.Ingredient1 = exportedAssets.NamedItems.GetValueOrDefault(recipe.Ingredient1)?.DisplayName;
                if (recipe.Ingredient2 != null)
                    recipe.Ingredient2 = exportedAssets.NamedItems.GetValueOrDefault(recipe.Ingredient2)?.DisplayName;
                if (recipe.Ingredient3 != null)
                    recipe.Ingredient3 = exportedAssets.NamedItems.GetValueOrDefault(recipe.Ingredient3)?.DisplayName;
                if (recipe.Ingredient4 != null)
                    recipe.Ingredient4 = exportedAssets.NamedItems.GetValueOrDefault(recipe.Ingredient4)?.DisplayName;
                if (recipe.Ingredient5 != null)
                    recipe.Ingredient5 = exportedAssets.NamedItems.GetValueOrDefault(recipe.Ingredient5)?.DisplayName;

                cancellationToken.ThrowIfCancellationRequested();
            }

            while (recipesToExclude.Count > 0)
                exportedRecipes.RemoveAt(recipesToExclude.Pop());

            var settings = new JsonSerializerSettings { ContractResolver = contractResolver, Formatting = Formatting.Indented };
            var serializer = JsonSerializer.CreateDefault(settings);

            string path = options.Path;

            if (options.Merge && File.Exists(path))
            {
                logger.LogInformation(Resources.Status_MergingIntoExistingArtifact, path);

                IList<ExportedRecipe> previous;
                using (var stream = File.OpenText(path))
                {
                    var rdr = new JsonTextReader(stream);
                    previous = serializer.Deserialize<IList<ExportedRecipe>>(rdr)
                        ?? throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.FormatString_Error_CannotReadPreviousArtifact, path));
                }

                Merge(previous, exportedRecipes);
                exportedRecipes = previous;
            }
            else
            {
                logger.LogInformation(Resources.Status_WritingFreshArtifact, path);
            }

            exportedRecipes = exportedRecipes
                .OrderBy(r => r.ItemName)
                .ThenBy(r => r.Type)
                .ThenBy(r => r.Subtype)
                .ThenBy(r => r.Rarity)
                .ThenBy(r => r.Tier)
                .ThenBy(r => r.Material)
                .ToList();

            using (var file = File.CreateText(path))
            {
                serializer.Serialize(file, exportedRecipes);
            }

            return Task.CompletedTask;
        }

        private static IList<ExportedRecipe> Merge(IList<ExportedRecipe> previous, IList<ExportedRecipe> current)
        {
            // duplicate current and build a map of merge key => array index
            var mySrc = new List<ExportedRecipe?>(current.Count);
            var keyedSrc = new Dictionary<string, int>(current.Count);

            foreach (var er in current)
            {
                keyedSrc.Add(er.GetMergeKey(), mySrc.Count);
                mySrc.Add(er);
            }

            // copy previous into result, taking values from current instead whenever possible
            var result = new List<ExportedRecipe>(previous.Count + (mySrc.Count / 10));

            foreach (var recipe in previous)
            {
                var mergeKey = recipe.GetMergeKey();

                if (keyedSrc.TryGetValue(mergeKey, out var i))
                {
                    var item = mySrc[i];
                    System.Diagnostics.Debug.Assert(item != null);
                    result.Add(item);
                    keyedSrc.Remove(mergeKey);
                    mySrc[i] = null;
                }
                else
                {
                    result.Add(recipe);
                }
            }

            // copy any remaining items from current
            foreach (var recipe in mySrc)
            {
                if (recipe != null)
                {
                    mySrc.Add(recipe);
                }
            }

            return result;
        }
    }
}
