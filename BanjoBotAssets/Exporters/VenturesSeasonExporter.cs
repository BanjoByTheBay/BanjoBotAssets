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
namespace BanjoBotAssets.Exporters
{
    internal sealed class VenturesSeasonExporter : BaseExporter
    {
        const string LevelRewardsTableName = "PhoenixLevelRewardsTable.uasset";
        const string PastLevelRewardsTableName = "PostMaxPhoenixLevelRewardsTable.uasset";
        const string DefaultGameDataTableName = "DefaultGameDataSTW.uasset";

        public VenturesSeasonExporter(IExporterContext services) : base(services)
        {
        }

        public override async Task ExportAssetsAsync(IProgress<ExportProgress> progress, IAssetOutput output, CancellationToken cancellationToken)
        {
            var levelRewardsPath = assetPaths.Find(p => Path.GetFileName(p).Equals(LevelRewardsTableName, StringComparison.OrdinalIgnoreCase));
            var pastLevelRewardsPath = assetPaths.Find(p => Path.GetFileName(p).Equals(PastLevelRewardsTableName, StringComparison.OrdinalIgnoreCase));
            var defaultGameDataPath = assetPaths.Find(p => Path.GetFileName(p).Equals(DefaultGameDataTableName, StringComparison.OrdinalIgnoreCase));

            if (levelRewardsPath == null)
            {
                logger.LogError(Resources.Error_SpecificAssetNotFound, LevelRewardsTableName);
                return;
            }

            if (pastLevelRewardsPath == null)
            {
                logger.LogError(Resources.Error_SpecificAssetNotFound, PastLevelRewardsTableName);
                return;
            }

            if (defaultGameDataPath == null)
            {
                logger.LogError(Resources.Error_SpecificAssetNotFound, DefaultGameDataTableName);
                return;
            }

            var levelRewardsTask = provider.LoadObjectAsync<UDataTable>(provider[levelRewardsPath].PathWithoutExtension);
            var pastLevelRewardsTask = provider.LoadObjectAsync<UDataTable>(provider[pastLevelRewardsPath].PathWithoutExtension);
            var defaultGameDataTask = provider.LoadObjectAsync<UObject>(provider[defaultGameDataPath].PathWithoutExtension);

            ExportLevelRewards(await levelRewardsTask, output, cancellationToken);
            ExportPastLevelRewards(await pastLevelRewardsTask, output, cancellationToken);
            ExportPastLevelXPRequirements(await defaultGameDataTask, output, cancellationToken);
        }

        private static void ExportPastLevelXPRequirements(UObject defaultGameData, IAssetOutput output, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var map = defaultGameData.Get<UScriptMap>("PhoenixEventOverlevelXPPerLevel");
            foreach (var (p, v) in map.Properties)
            {
                if (p?.GenericValue is string eventTag && v?.GenericValue is int xpAmount)
                {
                    output.AddVenturesPastLevelXPRequirement(eventTag, xpAmount);
                }
            }
        }

        private static void ExportLevelRewards(UDataTable levelRewardsTable, IAssetOutput output, CancellationToken cancellationToken = default)
        {
            int level = 1;

            foreach (var (key, row) in levelRewardsTable.RowMap)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (key.IsNone)
                {
                    continue;
                }

                var eventTag = row.Get<string>("EventTag");
                var totalRequiredXP = row.Get<int>("TotalRequiredXP");
                var isMajorReward = row.Get<bool>("bIsMajorReward");
                var convertedRewards = new List<QuestReward>();

                var visibleRewards = row.Get<FStructFallback[]>("VisibleReward").Select(r => new { r, hidden = false });
                var hiddenRewards = row.Get<FStructFallback[]>("HiddenRewards").Select(r => new { r, hidden = true });

                foreach (var r in visibleRewards.Concat(hiddenRewards))
                {
                    if (r?.r.GetOrDefault<string>("TemplateId") is string tid && r.r.GetOrDefault<int>("Quantity") is int qty)
                    {
                        convertedRewards.Add(new QuestReward
                        {
                            Hidden = r.hidden,
                            Item = tid,
                            Quantity = qty,
                            Selectable = false,
                        });
                    }
                }

                output.AddVenturesLevelReward(eventTag, level, totalRequiredXP, isMajorReward, convertedRewards);

                level++;
            }
        }

        private static void ExportPastLevelRewards(UDataTable pastLevelRewardsTable, IAssetOutput output, CancellationToken cancellationToken = default)
        {
            int pastLevel = 1;

            foreach (var (key, row) in pastLevelRewardsTable.RowMap)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (key.IsNone)
                {
                    continue;
                }

                var eventTag = row.Get<string>("EventTag");
                var isMajorReward = row.Get<bool>("bIsMajorReward");
                QuestReward convertedReward;

                var visibleRewards = row.Get<FStructFallback[]>("VisibleReward");

                var r = visibleRewards.Single();
                if (r?.GetOrDefault<string>("TemplateId") is string tid && r.GetOrDefault<int>("Quantity") is int qty)
                {
                    convertedReward = new QuestReward
                    {
                        Hidden = false,
                        Item = tid,
                        Quantity = qty,
                        Selectable = false,
                    };

                    output.AddVenturesPastLevelReward(eventTag, pastLevel, convertedReward);
                }

                pastLevel++;
            }
        }

        protected override bool InterestedInAsset(string name)
        {
            return name.EndsWith("/" + LevelRewardsTableName, StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith("/" + PastLevelRewardsTableName, StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith("/" + DefaultGameDataTableName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
