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

namespace BanjoBotAssets.Exporters.Groups
{
    internal record BaseParsedItemName(string BaseName, string Rarity, int Tier);

    internal record BaseItemGroupFields(string DisplayName, string? Description, string? SubType)
    {
        public BaseItemGroupFields() : this("", null, null) { }

        public string? SmallPreviewImagePath { get; set; }
        public string? LargePreviewImagePath { get; set; }

        public bool IsPermanent { get; set; }
        public bool IsInventoryLimitExempt { get; set; }
    }

    /// <summary>
    /// A base class for exporting items that have variants for different rarities and tiers.
    /// </summary>
    /// <typeparam name="TAsset">The type of the asset.</typeparam>
    /// <typeparam name="TParsedName">A type containing properties extracted from each variant's
    /// asset name, which are combined with the data extracted from the group's primary asset to
    /// export the variant.</typeparam>
    /// <typeparam name="TFields">A type containing properties extracted from a group's primary
    /// asset, which are combined with the data extracted from each variant's asset name to
    /// export the variant.</typeparam>
    /// <typeparam name="TItemData">The type of the exported item.</typeparam>
    /// <remarks>
    /// <para>This base class is intended for items such as heroes and schematics, where most
    /// properties of the item are the same across all variants, and the differences can be
    /// determined by parsing the asset names.</para>
    /// <para><see cref="ParseAssetName(string)">ParseAssetName</see> is called for every matched
    /// asset, and the returned <see cref="BaseParsedItemName.BaseName">BaseName</see> is used to
    /// group the assets.</para>
    /// <para>Each group's primary asset, chosen by
    /// <see cref="SelectPrimaryAsset(IGrouping{string?, string})">SelectPrimaryAsset</see>, is
    /// loaded, and data common to all assets in the group is extracted from it by
    /// <see cref="ExtractCommonFieldsAsync(TAsset, IGrouping{string?, string})">ExtractCommonFieldsAsync</see>.</para>
    /// <para><see cref="ExportAssetAsync(TParsedName, TAsset, TFields, string, TItemData)">ExportAssetAsync</see>
    /// is then called to produce the exported item for each asset in the group, by combining the
    /// common fields with each variant's parsed name.</para>
    /// </remarks>
    internal abstract class GroupExporter<TAsset, TParsedName, TFields, TItemData>(IExporterContext services) : BaseExporter(services)
        where TAsset : UObject
        where TParsedName : BaseParsedItemName
        where TFields : BaseItemGroupFields, new()
        where TItemData : NamedItemData, new()
    {
        private int numToProcess, processedSoFar;
        private readonly ConcurrentDictionary<string, byte> failedAssets = new();

        /// <summary>
        /// The asset type, which appears before the colon in the asset's template ID.
        /// </summary>
        protected abstract string Type { get; }
        /// <summary>
        /// Overridden in a derived class to parse the asset names, separating the base name
        /// used to group the assets from other properties that differ between variants within
        /// a group (such as rarity and tier).
        /// </summary>
        /// <param name="name">The name of the asset.</param>
        /// <returns>An instance of <typeparamref name="TParsedName"/>.</returns>
        protected abstract TParsedName? ParseAssetName(string name);

        private void Report(IProgress<ExportProgress> progress, string current)
        {
            progress.Report(new ExportProgress
            {
                TotalSteps = numToProcess,
                CompletedSteps = processedSoFar,
                AssetsLoaded = assetsLoaded,
                CurrentItem = current,
                FailedAssets = failedAssets.Keys,
            });
        }

        /// <inheritdoc/>
        public override async Task ExportAssetsAsync(IProgress<ExportProgress> progress, IAssetOutput output, CancellationToken cancellationToken)
        {
            // grouping is deprecated: it misses some differences between variants, and actually seems to hurt performance now that we're using CachingFileProvider.
            // TODO: Rewrite all GroupExporter subclasses to use UObjectExporter instead, and remove this class. (https://github.com/BanjoByTheBay/BanjoBotAssets/issues/41)
            var uniqueAssets = assetPaths.ToLookup(path => ParseAssetName(path) == null ? null : path, StringComparer.OrdinalIgnoreCase);
            numToProcess = uniqueAssets.Count;

            Report(progress, string.Format(CultureInfo.CurrentCulture, FormatStrings.ExportingGroup, Type));

            var assetsToProcess = scopeOptions.Value.Limit != null ? uniqueAssets.Take((int)scopeOptions.Value.Limit) : uniqueAssets;
            var opts = new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = performanceOptions.Value.MaxParallelism };

            await Parallel.ForEachAsync(assetsToProcess, opts, async (grouping, _) =>
            {
                try
                {
                    var baseName = grouping.Key;

                    if (baseName == null)
                    {
                        logger.LogInformation(Resources.Status_SkippingNullGroup, Type, grouping.Count());
                        return;
                    }

                    var primaryAssetPath = SelectPrimaryAsset(grouping);
                    var file = provider[primaryAssetPath];

                    var num = Interlocked.Increment(ref processedSoFar);
                    logger.LogInformation(Resources.Status_ProcessingTypeGroupNumOfNum, Type, num, numToProcess);

                    //logger.LogInformation("Loading {0}", file.PathWithoutExtension);
                    Interlocked.Increment(ref assetsLoaded);

                    Report(progress, file.PathWithoutExtension);

                    var asset = await provider.LoadObjectAsync<TAsset>(file.PathWithoutExtension);

                    if (asset == null)
                    {
                        failedAssets.TryAdd(baseName, 0);
                        logger.LogError(Resources.Warning_FailedToLoadFile, file.PathWithoutExtension);
                        return;
                    }

                    if (!WantThisAsset(asset))
                    {
                        logger.LogInformation(Resources.Status_SkippingEarlyAsInstructed, file.PathWithoutExtension);
                        return;
                    }

                    var fields = await ExtractCommonFieldsAsync(asset, grouping);

                    foreach (var path in grouping)
                    {
                        var templateId = $"{Type}:{Path.GetFileNameWithoutExtension(path)}";
                        var parsed = ParseAssetName(path);

                        if (parsed == null)
                        {
                            failedAssets.TryAdd(baseName, 0);
                            logger.LogWarning(Resources.Status_SkippingUnparsable, templateId, path);
                            continue;
                        }

                        var itemData = new TItemData
                        {
                            AssetPath = provider.FixPath(Path.Combine(Path.GetDirectoryName(path)!, Path.GetFileNameWithoutExtension(path))),
                            Description = GetDescription(parsed, asset, fields),
                            DisplayName = GetDisplayName(parsed, asset, fields).Trim(),
                            Name = Path.GetFileNameWithoutExtension(path),
                            SubType = fields.SubType,
                            Type = Type,
                            Rarity = GetRarity(parsed, asset, fields).GetNameText().Text,
                            Tier = parsed.Tier,
                            IsPermanent = fields.IsPermanent,
                            IsInventoryLimitExempt = fields.IsInventoryLimitExempt,
                        };

                        if (!await ExportAssetAsync(parsed, asset, fields, path, itemData))
                        {
                            logger.LogInformation(Resources.Status_SkippingLateAsInstructed, templateId);
                            return;
                        }

                        output.AddNamedItem(templateId, itemData);

                        if (fields.SmallPreviewImagePath != null)
                        {
                            output.AddImageForNamedItem(templateId, ImageType.SmallPreview, fields.SmallPreviewImagePath);
                        }

                        if (fields.LargePreviewImagePath != null)
                        {
                            output.AddImageForNamedItem(templateId, ImageType.LargePreview, fields.LargePreviewImagePath);
                        }
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    if (grouping.Key != null)
                        failedAssets.TryAdd(grouping.Key, 0);

                    logger.LogError(ex, Resources.Error_ExceptionWhileProcessingAssetGroup, grouping.Key);
                }
            });

            Report(progress, "");
            logger.LogInformation(Resources.Status_ExportedGroup, Type, assetsToProcess.Count(), failedAssets.Count);
        }

        /// <summary>
        /// Overridden in a derived class to combine the common fields from a group's primary
        /// asset with the fields parsed from a variant's name.
        /// </summary>
        /// <param name="parsed">The fields parsed from the variant's name.</param>
        /// <param name="primaryAsset">The loaded primary asset of the group.</param>
        /// <param name="fields">The common fields extracted from the primary asset.</param>
        /// <param name="path">The variant's asset path.</param>
        /// <param name="itemData">The exported item for this variant.</param>
        /// <returns><see langword="true"/> if this variant should be exported, or
        /// <see langword="false"/> if it should be skipped.</returns>
        /// <remarks>
        /// The base properties defined on <see cref="NamedItemData"/> are already populated on
        /// <paramref name="itemData"/> by the time this method is called. Generally, this method
        /// only needs to be overridden if <typeparamref name="TItemData"/> contains additional
        /// properties that need to be populated. The default implementation does nothing and
        /// returns <see langword="true"/>.
        /// </remarks>
        protected virtual Task<bool> ExportAssetAsync(TParsedName parsed, TAsset primaryAsset, TFields fields, string path, TItemData itemData)
        {
            return Task.FromResult(true);
        }

        /// <summary>
        /// Overridden in a derived class to select one asset from a group to be loaded as
        /// the primary asset.
        /// </summary>
        /// <param name="assetGroup">The set of asset paths comprising the group.</param>
        /// <returns>The path of the asset from which the common fields should be read.</returns>
        /// <remarks>
        /// The default implementation returns the first asset in the group.
        /// </remarks>
        protected virtual string SelectPrimaryAsset(IGrouping<string?, string> assetGroup)
        {
            return assetGroup.First();
        }

        /// <summary>
        /// Overridden in a derived class to determine whether a group should be exported
        /// by examining the primary asset.
        /// </summary>
        /// <param name="asset">The group's primary asset.</param>
        /// <returns><see langword="true"/> to continue processing the group, or
        /// <see langword="false"/> to skip it.</returns>
        /// <remarks>
        /// The default implementation always returns <see langword="true"/>.
        /// </remarks>
        protected virtual bool WantThisAsset(TAsset asset)
        {
            return true;
        }

        /// <summary>
        /// Overridden in a derived class to extract the common fields from a group's primary asset.
        /// </summary>
        /// <param name="asset">The group's primary asset.</param>
        /// <param name="grouping">The set of asset paths comprising the group.</param>
        /// <returns>An instance of <typeparamref name="TFields"/> containing data extracted from
        /// the primary asset.</returns>
        /// <remarks>
        /// The default implementation extracts all fields defined on <see cref="BaseItemGroupFields"/>,
        /// with the exception of <see cref="BaseItemGroupFields.SubType"/>, which is set to
        /// <see langword="null"/>.
        /// </remarks>
        protected virtual Task<TFields> ExtractCommonFieldsAsync(TAsset asset, IGrouping<string?, string> grouping)
        {
            return Task.FromResult(new TFields() with
            {
                Description = asset.GetOrDefault<FText>("ItemDescription")?.Text ?? asset.GetOrDefault<FText>("Description")?.Text,
                DisplayName = asset.GetOrDefault<FText>("ItemName")?.Text ?? asset.GetOrDefault<FText>("DisplayName")?.Text ?? $"<{grouping.Key}>",
                SubType = null,
                SmallPreviewImagePath = asset.GetSoftAssetPathFromDataList("Icon"),
                LargePreviewImagePath = asset.GetSoftAssetPathFromDataList("LargeIcon"),
                IsPermanent = asset.GetOrDefault<FDataTableRowHandle>("SacrificeRecipe") is null or { RowName.IsNone: true } or { DataTable: null },
                IsInventoryLimitExempt = !asset.GetOrDefault("bInventorySizeLimited", true),
        });
        }

        /// <summary>
        /// Overridden in a derived class to adjust the rarity of a variant when the rarity displayed
        /// in game differs from the rarity parsed from the variant's name.
        /// </summary>
        /// <param name="parsedName">The fields parsed from the variant's name.</param>
        /// <param name="primaryAsset">The group's primary asset.</param>
        /// <param name="fields">The fields extracted from the primary asset.</param>
        /// <returns>The in-game rarity.</returns>
        /// <remarks>
        /// <para>The default implementation simply translates the rarity parsed from the variant's name
        /// into the corresponding <see cref="EFortRarity"/> value.</para>
        /// <para>Most mythic items in STW are identified as legendary ("SR") in the asset names, so this method
        /// must be overridden to correctly classify them as mythic.</para>
        /// </remarks>
        protected virtual EFortRarity GetRarity(TParsedName parsedName, TAsset primaryAsset, TFields fields) =>
            parsedName.Rarity.ToUpperInvariant() switch
            {
                "C" => EFortRarity.Common,
                "R" => EFortRarity.Rare,
                "VR" => EFortRarity.Epic,
                "SR" => EFortRarity.Legendary,
                "UR" => EFortRarity.Mythic,
                _ => EFortRarity.Uncommon,
            };

        /// <summary>
        /// Overridden in a derived class to adjust the display name of a variant when the display name
        /// extracted from the primary asset cannot be used as-is.
        /// </summary>
        /// <param name="parsedName">The fields parsed from the variant's name.</param>
        /// <param name="primaryAsset">The group's primary asset.</param>
        /// <param name="fields">The fields extracted from the primary asset.</param>
        /// <returns>The display name.</returns>
        /// <remarks>
        /// The default implementation returns the display name extracted from the primary asset.
        /// </remarks>
        protected virtual string GetDisplayName(TParsedName parsedName, TAsset primaryAsset, TFields fields) => fields.DisplayName;

        /// <summary>
        /// Overridden in a derived class to adjust the description of a variant when the description
        /// extracted from the primary asset cannot be used as-is.
        /// </summary>
        /// <param name="parsedName">The fields parsed from the variant's name.</param>
        /// <param name="primaryAsset">The group's primary asset.</param>
        /// <param name="fields">The fields extracted from the primary asset.</param>
        /// <returns>The display name.</returns>
        /// <remarks>
        /// The default implementation returns the description extracted from the primary asset.
        /// </remarks>
        protected virtual string? GetDescription(TParsedName parsedName, TAsset primaryAsset, TFields fields) => fields.Description;
    }

    /// <summary>
    /// A base class for exporting items that have variants for different rarities and tiers, and that
    /// don't have any additional properties that need to be populated.
    /// </summary>
    /// <typeparam name="TAsset">The type of the asset.</typeparam>
    internal abstract class GroupExporter<TAsset>(IExporterContext services) : GroupExporter<TAsset, BaseParsedItemName, BaseItemGroupFields, NamedItemData>(services)
        where TAsset : UObject
    {
    }
}
