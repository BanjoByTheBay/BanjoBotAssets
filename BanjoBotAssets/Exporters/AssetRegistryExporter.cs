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
using CUE4Parse.UE4.AssetRegistry;

namespace BanjoBotAssets.Exporters
{
    /// <summary>
    /// Extracts the display names of weapons and traps so they can be applied to the exported schematics.
    /// </summary>
    internal sealed partial class AssetRegistryExporter(IExporterContext services) : BaseExporter(services)
    {
        [GeneratedRegex("^Fort(?:Weapon(?:Ranged|Melee)|Trap)ItemDefinition$", RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled)]
        private static partial Regex WeaponOrTrapAssetClassRegex();

        [GeneratedRegex(
            """
            ^NSLOCTEXT\(
            \s* "(?<ns>   (?: [^"] | \\" )* )" \s*,
            \s* "(?<id>   (?: [^"] | \\" )* )" \s*,
            \s* "(?<text> (?: [^"] | \\" )* )" \s*
            \)
            """, RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace)]
        private static partial Regex NSLocTextRegex();

        public override async Task ExportAssetsAsync(IProgress<ExportProgress> progress, IAssetOutput output, CancellationToken cancellationToken)
        {
            var numToProcess = assetPaths.Count;
            var processedSoFar = 0;

            var opts = new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = performanceOptions.Value.MaxParallelism };

            foreach (var path in scopeOptions.Value.Limit != null ? assetPaths.Take((int)scopeOptions.Value.Limit) : assetPaths)
            {
                try
                {
                    var file = provider[path];

                    var num = Interlocked.Increment(ref processedSoFar);
                    logger.LogInformation(Resources.Status_ProcessingFile, file.Name, num, numToProcess);

                    Interlocked.Increment(ref assetsLoaded);

                    await using var reader = file.CreateReader();
                    var assetRegistry = new FAssetRegistryState(reader);
                    foreach (var buffer in assetRegistry.PreallocatedAssetDataBuffers)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (buffer.ObjectPath.Contains("/Athena/", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (WeaponOrTrapAssetClassRegex().IsMatch(buffer.AssetClass.Text))
                        {
                            var schematicTemplateId = $"Schematic:SID_{buffer.AssetName.Text[4..]}";
                            var displayName = buffer.TagsAndValues.FirstOrDefault(pair => pair.Key.Text == "DisplayName").Value;

                            if (displayName == null)
                            {
                                continue;
                            }

                            if (NSLocTextRegex().Match(displayName) is { Success: true, Groups: var g })
                            {
                                var goodName = provider.GetLocalizedString(
                                    Regex.Unescape(g["ns"].Value),
                                    Regex.Unescape(g["id"].Value),
                                    Regex.Unescape(g["text"].Value));
                                output.AddDisplayNameCorrection(schematicTemplateId, goodName.Trim());
                            }
                            else
                            {
                                output.AddDisplayNameCorrection(schematicTemplateId, displayName.Trim());
                            }
                        }
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, Resources.Error_ExceptionWhileProcessingAsset, path);
                }
            }
        }

        protected override bool InterestedInAsset(string name) => AssetRegistryFileRegex().IsMatch(name);

        [GeneratedRegex(@"^(?:.*(?<!/UEFN)/)?AssetRegistry[0-9A-F]*\.bin$", RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
        private static partial Regex AssetRegistryFileRegex();
    }
}
