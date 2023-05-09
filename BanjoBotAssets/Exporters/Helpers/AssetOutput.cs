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
using System.Collections.Concurrent;

namespace BanjoBotAssets.Exporters.Helpers
{
    internal sealed class AssetOutput : IAssetOutput
    {
        private ItemRatingTable? defaultItemRatings, survivorItemRatings, leadSurvivorItemRatings;
        private readonly ConcurrentDictionary<string, NamedItemData> namedItems = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<ImageType, ConcurrentDictionary<string, string>> namedItemImages = new();
        private readonly ConcurrentDictionary<string, DifficultyInfo> difficultyInfo = new(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, int>> craftingRecipes = new(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, string> displayNameCorrections = new(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, string[][]> mainQuestLines = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, string[][]> eventQuestLines = new(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, ConcurrentDictionary<int, XPRewardLevel>> venturesLevelRewards = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, SeasonPastLevelData> venturesPastLevelRewards = new(StringComparer.OrdinalIgnoreCase);

        private sealed record SeasonPastLevelData()
        {
            public int XPStepAmount { get; set; } = 200_000;
            public ConcurrentDictionary<int, XPRewardLevel> Rewards = new();
        }

        public void AddDefaultItemRatings(ItemRatingTable itemRatings)
        {
            defaultItemRatings = itemRatings;
        }

        public void AddDifficultyInfo(string name, DifficultyInfo difficultyInfo)
        {
            this.difficultyInfo.TryAdd(name, difficultyInfo);
        }

        public void AddLeadSurvivorItemRatings(ItemRatingTable itemRatings)
        {
            leadSurvivorItemRatings = itemRatings;
        }

        public void AddNamedItem(string name, NamedItemData itemData)
        {
            namedItems.TryAdd(name, itemData);
        }

        public void AddImageForNamedItem(string name, ImageType type, string assetPath)
        {
            var dict = namedItemImages.GetOrAdd(type, _ => new(StringComparer.OrdinalIgnoreCase));
            dict.TryAdd(name, assetPath);
        }

        public void AddSurvivorItemRatings(ItemRatingTable itemRatings)
        {
            survivorItemRatings = itemRatings;
        }

        public void CopyTo(ExportedAssets exportedAssets, IList<ExportedRecipe> exportedRecipes, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var (k, v) in namedItems)
            {
                exportedAssets.NamedItems.TryAdd(k, v);
            }

            cancellationToken.ThrowIfCancellationRequested();

            foreach (var (imageType, dict) in namedItemImages)
            {
                foreach (var (k, v) in dict)
                {
                    var ni = exportedAssets.NamedItems[k];
                    ni.ImagePaths ??= new SortedDictionary<ImageType, string>();
                    ni.ImagePaths.TryAdd(imageType, v);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            foreach (var (k, v) in difficultyInfo)
            {
                exportedAssets.DifficultyInfo.TryAdd(k, v);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (defaultItemRatings != null)
                exportedAssets.ItemRatings.Default = defaultItemRatings;
            if (survivorItemRatings != null)
                exportedAssets.ItemRatings.Survivor = survivorItemRatings;
            if (leadSurvivorItemRatings != null)
                exportedAssets.ItemRatings.LeadSurvivor = leadSurvivorItemRatings;

            cancellationToken.ThrowIfCancellationRequested();

            foreach (var (k, v) in mainQuestLines)
            {
                exportedAssets.MainQuestLines.TryAdd(k, v);
            }

            cancellationToken.ThrowIfCancellationRequested();

            foreach (var (k, v) in eventQuestLines)
            {
                exportedAssets.EventQuestLines.TryAdd(k, v);
            }

            foreach (var (templateId, recipe) in craftingRecipes)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var ingredients = new Queue<KeyValuePair<string, int>>(recipe);

                var exportedRecipe = new ExportedRecipe { ItemName = templateId };

                if (ingredients.TryDequeue(out var pair))
                    (exportedRecipe.Ingredient1, exportedRecipe.Quantity1!) = pair;
                if (ingredients.TryDequeue(out pair))
                    (exportedRecipe.Ingredient2, exportedRecipe.Quantity2!) = pair;
                if (ingredients.TryDequeue(out pair))
                    (exportedRecipe.Ingredient3, exportedRecipe.Quantity3!) = pair;
                if (ingredients.TryDequeue(out pair))
                    (exportedRecipe.Ingredient4, exportedRecipe.Quantity4!) = pair;
                if (ingredients.TryDequeue(out pair))
                    (exportedRecipe.Ingredient5, exportedRecipe.Quantity5!) = pair;

                exportedRecipes.Add(exportedRecipe);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (!venturesLevelRewards.IsEmpty)
            {
                foreach (var (eventTag, levels) in venturesLevelRewards)
                {
                    var season = new VenturesSeason { EventTag = eventTag };

                    foreach (var (_, data) in levels)
                    {
                        var seasonLevel = new VenturesSeasonLevel()
                        {
                            IsMajorReward = data.IsMajorReward,
                            TotalRequiredXP = data.TotalRequiredXP,
                        };
                        for (int i = 0; i < data.Rewards.Length; i++)
                        {
                            seasonLevel.Rewards.Add(data.Rewards[i]);
                        }
                        season.Levels.Add(seasonLevel);
                    }

                    if (!venturesPastLevelRewards.IsEmpty)
                    {
                        var seasonPastLevelData = venturesPastLevelRewards[eventTag];

                        season.PastLevelXPRequirement = seasonPastLevelData.XPStepAmount;

                        foreach (var (pastLevel, data) in seasonPastLevelData.Rewards)
                        {
                            var seasonLevel = new VenturesSeasonLevel()
                            {
                                IsMajorReward = data.IsMajorReward,
                                TotalRequiredXP = data.TotalRequiredXP,
                            };
                            for (int i = 0; i < data.Rewards.Length; i++)
                            {
                                seasonLevel.Rewards.Add(data.Rewards[i]);
                            }
                            season.PastLevels.Add(seasonLevel.Rewards);
                        }
                    }

                    exportedAssets.VenturesSeasons.Add(eventTag, season);
                }
            }
        }

        [Obsolete]
        public void AddCraftingRecipe(string name, IReadOnlyDictionary<string, int> ingredients)
        {
            craftingRecipes.TryAdd(name, ingredients);
        }

        public void AddMainQuestLine(string name, string[][] quests)
        {
            mainQuestLines.TryAdd(name, quests);
        }

        public void AddEventQuestLine(string name, string[][] quests)
        {
            eventQuestLines.TryAdd(name, quests);
        }

        public void AddDisplayNameCorrection(string templateId, string correctedName)
        {
            displayNameCorrections.TryAdd(templateId, correctedName);
        }

        public void ApplyDisplayNameCorrections(ExportedAssets exportedAssets)
        {
            foreach (var (templateId, displayName) in displayNameCorrections)
            {
                if (exportedAssets.NamedItems.TryGetValue(templateId, out var item))
                {
                    item.DisplayName = displayName;
                }
            }
        }

        public void AddVenturesLevelReward(string eventTag, int level, int totalRequiredXP, bool isMajorReward, List<QuestReward> convertedRewards)
        {
            var dict = venturesLevelRewards.GetOrAdd(eventTag, _ => new());
            dict.TryAdd(level, new()
            {
                IsMajorReward = isMajorReward,
                TotalRequiredXP = totalRequiredXP,
                Rewards = convertedRewards.ToArray(),
            });
        }

        public void AddVenturesPastLevelReward(string eventTag, int pastLevel, QuestReward convertedReward)
        {
            var seasonPastLevelData = venturesPastLevelRewards.GetOrAdd(eventTag, _ => new());
            seasonPastLevelData.Rewards.TryAdd(pastLevel, new()
            {
                IsMajorReward = false,
                Rewards = new[] { convertedReward },
            });
        }

        public void AddVenturesPastLevelXPRequirement(string eventTag, int xpAmount)
        {
            if (venturesPastLevelRewards.TryGetValue(eventTag, out var seasonPastLevelData))
            {
                seasonPastLevelData.XPStepAmount = xpAmount;
            }
        }
    }
}
