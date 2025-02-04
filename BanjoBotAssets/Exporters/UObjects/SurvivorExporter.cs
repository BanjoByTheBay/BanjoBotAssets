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

using CUE4Parse.UE4.Assets.Exports;

namespace BanjoBotAssets.Exporters.UObjects
{
    internal sealed class SurvivorExporter(IExporterContext services) : UObjectExporter<UFortWorkerType, SurvivorItemData>(services)
    {
        protected override string Type => "Worker";

        protected override bool InterestedInAsset(string name) =>
            name.Contains("Workers/Worker", StringComparison.OrdinalIgnoreCase) || name.Contains("Managers/Manager", StringComparison.OrdinalIgnoreCase);

        protected override async Task<bool> ExportAssetAsync(UFortWorkerType asset, SurvivorItemData itemData, Dictionary<ImageType, string> imagePaths)
        {
            if (asset.GetOrDefault("Rarity", EFortRarity.Uncommon) is EFortRarity rarity)
                itemData.Rarity = rarity.GetNameText().Text;
            itemData.SubType = asset.bIsManager ? GetManagerJob(asset) : null;
            itemData.DisplayName = asset.ItemName?.Text ?? MakeSurvivorDisplayName(asset);
            itemData.Personality = asset.FixedPersonalityTag.GameplayTags is { Length: 1 }
                ? asset.FixedPersonalityTag.GameplayTags[0].ToString().Split('.')[^1]
                : null;
            if(asset.GetSoftAssetPath("FixedPortrait") is string portraitPath)
            {
                Interlocked.Increment(ref assetsLoaded);
                var portrait = await provider.LoadObjectAsync<UObject>(portraitPath);
                if (portrait.GetSoftAssetPath("SmallImage") is string smallPreviewPath)
                {
                    imagePaths.Remove(ImageType.SmallPreview);
                    imagePaths.Add(ImageType.SmallPreview, smallPreviewPath);
                }

                if (portrait.GetSoftAssetPath("LargeImage") is string largePreviewPath)
                {
                    imagePaths.Remove(ImageType.LargePreview);
                    imagePaths.Add(ImageType.LargePreview, largePreviewPath);
                }
            }

            return true;
        }

        private static readonly Dictionary<string, string> managerSynergyToJob = new(StringComparer.OrdinalIgnoreCase)
        {
                { "Homebase.Manager.IsDoctor", Resources.Field_Survivor_Doctor },
                { "Homebase.Manager.IsEngineer", Resources.Field_Survivor_Engineer },
                { "Homebase.Manager.IsExplorer", Resources.Field_Survivor_Explorer },
                { "Homebase.Manager.IsGadgeteer", Resources.Field_Survivor_Gadgeteer },
                { "Homebase.Manager.IsInventor", Resources.Field_Survivor_Inventor },
                { "Homebase.Manager.IsMartialArtist", Resources.Field_Survivor_MartialArtist },
                { "Homebase.Manager.IsSoldier", Resources.Field_Survivor_Marksman },
                { "Homebase.Manager.IsTrainer", Resources.Field_Survivor_Trainer },
        };

        private static string GetManagerJob(UFortWorkerType worker)
        {
            if (!worker.bIsManager)
                throw new AssetFormatException(Resources.Error_NotAManager);

            string synergyTag = worker.ManagerSynergyTag.First().ToString();
            if (managerSynergyToJob.TryGetValue(synergyTag, out var job))
                return job;

#pragma warning disable CA1863 // Use 'CompositeFormat'
            throw new AssetFormatException(string.Format(CultureInfo.CurrentCulture, Resources.FormatString_Error_UnexpectedManagerSynergy, synergyTag));
#pragma warning restore CA1863 // Use 'CompositeFormat'
        }

        private static string MakeSurvivorDisplayName(UFortWorkerType worker) =>
            worker.bIsManager ? string.Format(CultureInfo.CurrentCulture, FormatStrings.SurvivorLeadName, GetManagerJob(worker)) : Resources.Field_Survivor_DefaultName;
    }
}
