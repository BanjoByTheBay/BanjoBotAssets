using CUE4Parse.UE4.AssetRegistry;

namespace BanjoBotAssets.Exporters
{
    /// <summary>
    /// Extracts the display names of weapons and traps so they can be applied to the exported schematics.
    /// </summary>
    internal sealed partial class AssetRegistryExporter : BaseExporter
    {
        [GeneratedRegex("^Fort(?:Weapon(?:Ranged|Melee)|Trap)ItemDefinition$", RegexOptions.Compiled)]
        private static partial Regex WeaponOrTrapAssetClassRegex();

        [GeneratedRegex(
            """
            ^NSLOCTEXT\(
            \s* "(?<ns>   (?: [^"] | \\" )* )" \s*,
            \s* "(?<id>   (?: [^"] | \\" )* )" \s*,
            \s* "(?<text> (?: [^"] | \\" )* )" \s*
            \)
            """, RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace)]
        private static partial Regex NSLocTextRegex();

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

        public AssetRegistryExporter(IExporterContext services) : base(services)
        {
        }

        protected override bool InterestedInAsset(string name) => AssetRegistryFileRegex().IsMatch(name);

        [GeneratedRegex(@"^(?:.*/)?AssetRegistry[0-9A-F]*\.bin$", RegexOptions.IgnoreCase, "en-US")]
        private static partial Regex AssetRegistryFileRegex();
    }
}
