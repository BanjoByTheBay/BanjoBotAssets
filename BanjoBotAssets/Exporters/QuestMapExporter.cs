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
using CUE4Parse.FN.Structs.CoreUObject;

namespace BanjoBotAssets.Exporters
{
    internal sealed class QuestMapExporter(IExporterContext services) : BaseExporter(services)
    {
        public override async Task ExportAssetsAsync(IProgress<ExportProgress> progress, IAssetOutput output, CancellationToken cancellationToken)
        {
            progress.Report(new ExportProgress { TotalSteps = 1, CompletedSteps = 0, AssetsLoaded = assetsLoaded, CurrentItem = Resources.Status_ExportingQuestMap });

            cancellationToken.ThrowIfCancellationRequested();

            var file = provider[assetPaths[0]];
            Interlocked.Increment(ref assetsLoaded);
            var mapData = await provider.LoadObjectAsync(file.PathWithoutExtension);

            cancellationToken.ThrowIfCancellationRequested();

            if (mapData == null)
            {
                logger.LogError(Resources.Error_SpecificAssetNotFound, assetPaths[0]);
                return;
            }

            List<Task<IEnumerable<QuestLine>>> tasks = [];

            var campaignLink = mapData.GetOrDefault("CampaignQuestMapDataAsset", default(ResolvedObject));
            if (campaignLink == null)
            {
                logger.LogWarning(Resources.Warning_MissingCampaignQuestMapData);
            }
            else
            {
                tasks.Add(LoadQuestLinesAsync(campaignLink, isMainCampaign: true, cancellationToken));
            }

            cancellationToken.ThrowIfCancellationRequested();

            var eventLinks = mapData.GetOrDefault("EventQuestMapDataAssetList", default(ResolvedObject[]));
            if (eventLinks == null)
            {
                logger.LogWarning(Resources.Warning_MissingEventQuestMapData);
            }
            else
            {
                foreach (var eventLink in eventLinks)
                {
                    tasks.Add(LoadQuestLinesAsync(eventLink, isMainCampaign: false, cancellationToken));
                }
            }

            int total = tasks.Count, done = 0;

            while (tasks.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var task = await Task.WhenAny(tasks);
                tasks.Remove(task);

                done++;
                progress.Report(new ExportProgress { TotalSteps = total, CompletedSteps = done, AssetsLoaded = assetsLoaded, CurrentItem = Resources.Status_ExportingQuestMap });

                var questLines = await task;

                foreach (var questLine in questLines)
                {
                    if (questLine.IsMainCampaign)
                    {
                        output.AddMainQuestLine(questLine.Name, questLine.QuestPages);
                    }
                    else
                    {
                        output.AddEventQuestLine(questLine.Name, questLine.QuestPages);
                    }
                }
            }

            progress.Report(new ExportProgress { TotalSteps = total, CompletedSteps = done, AssetsLoaded = assetsLoaded, CurrentItem = Resources.Status_ExportedQuestMap });
        }

        private sealed record QuestLine(string Name, string[][] QuestPages, bool IsMainCampaign = false);

        private async Task<IEnumerable<QuestLine>> LoadQuestLinesAsync(ResolvedObject questMapDataAssetLink, bool isMainCampaign,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // load the FortQuestMapDataAsset
            if (await questMapDataAssetLink.LoadAsync() is not UObject uo)
            {
                logger.LogError(Resources.Warning_MissingCampaignQuestMapData);
                return [];
            }

            cancellationToken.ThrowIfCancellationRequested();

            // the QuestData property may be missing for event callouts that don't have a quest line
            var tableLink = uo.GetOrDefault<ResolvedObject>("QuestData");

            if (tableLink == null)
            {
                if (isMainCampaign)
                {
                    logger.LogError(Resources.Warning_MissingCampaignQuestMapData);
                }

                return [];
            }

            // load the DataTable with the quest line pages
            if (await tableLink.LoadAsync() is not UDataTable table)
            {
                logger.LogError(Resources.Warning_MissingCampaignQuestMapData);
                return [];
            }

            // group the pages into quest lines and return them
            return from page in table.RowMap.Values
                   orderby page.Get<int>("PageIndex")
                   let quests = from q in page.Get<FStructFallback[]>("QuestList")
                                let qid = q.Get<FPrimaryAssetId>("QuestItemDefinitionId")
                                select $"{qid.PrimaryAssetType.Name.Text}:{qid.PrimaryAssetName}"
                   group quests.ToArray() by page.Get<FText>("PageTitle").Text into g
                   select new QuestLine(g.Key, [.. g], isMainCampaign);
        }

        protected override bool InterestedInAsset(string name) => Path.GetFileName(name).Equals("QuestMapData.uasset", StringComparison.OrdinalIgnoreCase);
    }
}
