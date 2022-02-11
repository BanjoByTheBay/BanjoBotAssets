using BanjoBotAssets.Exporters.Helpers;
using CUE4Parse.UE4.AssetRegistry;

namespace BanjoBotAssets.Exporters
{
    /// <summary>
    /// Extracts the display names of weapons and traps so they can be applied to the exported schematics.
    /// </summary>
    internal class AssetRegistryExporter : BaseExporter
    {
        private static readonly Regex weaponOrTrapAssetClassRegex = new("^Fort(?:Weapon(?:Ranged|Melee)|Trap)ItemDefinition$");
        private static readonly Regex nsLocTextRegex = new(@"^NSLOCTEXT\(\s*""(?<ns>(?:[^""]|\"")*)""\s*,\s*""(?<id>(?:[^""]|\"")*)""\s*,\s*""(?<text>(?:[^""]|\"")*)""\s*\)");

        public override Task ExportAssetsAsync(IProgress<ExportProgress> progress, IAssetOutput output, CancellationToken cancellationToken)
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

                    using var reader = file.CreateReader();
                    var assetRegistry = new FAssetRegistryState(reader);
                    foreach (var buffer in assetRegistry.PreallocatedAssetDataBuffers)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (buffer.ObjectPath.Text.Contains("/Athena/"))
                        {
                            continue;
                        }

                        if (weaponOrTrapAssetClassRegex.IsMatch(buffer.AssetClass.Text))
                        {
                            var schematicTemplateId = $"Schematic:SID_{buffer.AssetName.Text[4..]}";
                            var displayName = buffer.TagsAndValues.FirstOrDefault(pair => pair.Key.Text == "DisplayName").Value;

                            if (displayName == null)
                            {
                                continue;
                            }

                            if (nsLocTextRegex.Match(displayName) is { Success: true, Groups: var g })
                            {
                                var goodName = provider.GetLocalizedString(g["ns"].Value, g["id"].Value, g["text"].Value);
                                output.AddDisplayNameCorrection(schematicTemplateId, goodName.Trim());
                            }
                            else
                            {
                                // shouldn't get here...
                                output.AddDisplayNameCorrection(schematicTemplateId, $"<{displayName.Trim()}>");
                            }
                        }
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, Resources.Error_ExceptionWhileProcessingAsset, path);
                }
            }

            return Task.CompletedTask;
        }

        private static readonly Regex assetRegistryFileRegex = new(@"^(?:.*/)?AssetRegistry[0-9A-F]*\.bin$", RegexOptions.IgnoreCase);

        public AssetRegistryExporter(IExporterContext services) : base(services)
        {
        }

        protected override bool InterestedInAsset(string name) => assetRegistryFileRegex.IsMatch(name);
    }
}
