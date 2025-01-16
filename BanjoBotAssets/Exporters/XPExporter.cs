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
    internal sealed class XPExporter(IExporterContext services) : BaseExporter(services)
    {
        protected override bool InterestedInAsset(string name) =>
            name.EndsWith("XPAccountItemRewards.uasset", StringComparison.OrdinalIgnoreCase);

        public override async Task ExportAssetsAsync(IProgress<ExportProgress> progress, IAssetOutput output, CancellationToken cancellationToken)
        {
            progress.Report(new ExportProgress { TotalSteps = 1, CompletedSteps = 0, AssetsLoaded = assetsLoaded, CurrentItem = Resources.Status_ExportedRecipes+" (V2)" });

            var levelToXP = await ExportLevelToXP();
            if(levelToXP is not null)
                output.AddLevelToXPTable(levelToXP);

            progress.Report(new ExportProgress { TotalSteps = 1, CompletedSteps = 1, AssetsLoaded = assetsLoaded, CurrentItem = Resources.Status_ExportedRecipes + " (V2)" });
        }

        private async Task<Dictionary<string, int[]>?> ExportLevelToXP()
        {
            var tablePath = assetPaths.Find(p => Path.GetFileNameWithoutExtension(p).Equals("XPAccountItemRewards", StringComparison.OrdinalIgnoreCase));

            if (tablePath is null)
            {
                logger.LogError(Resources.Error_SpecificAssetNotFound, "XPAccountItemRewards");
                return null;
            }

            var file = provider[tablePath];
            Interlocked.Increment(ref assetsLoaded);

            var tableRecipes = await provider.LoadObjectAsync<UCurveTable>(file.PathWithoutExtension);

            if (tableRecipes is null)
            {
                logger.LogError(Resources.Error_ExceptionWhileProcessingAsset, "XPAccountItemRewards");
                return null;
            }

            Dictionary<string, int[]> result = [];
            foreach ( var row in tableRecipes.RowMap)
            {
                result.Add(row.Key.Text, Enumerable.Range(1, 60).Select(i => (int)(tableRecipes.FindCurve(row.Key)?.Eval(i) ?? 0)).ToArray());
            }
            return result;
        }
    }
}
