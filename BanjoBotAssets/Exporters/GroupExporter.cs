namespace BanjoBotAssets.Exporters
{
    internal record BaseParsedItemName(string BaseName, string Rarity, int Tier);

    internal record BaseItemGroupFields(string DisplayName, string? Description, string? SubType)
    {
        public BaseItemGroupFields() : this("", null, null) { }
    }

    internal abstract class GroupExporter<TAsset, TParsedName, TFields, TItemData> : BaseExporter
        where TAsset : UObject
        where TParsedName : BaseParsedItemName
        where TFields : BaseItemGroupFields, new()
        where TItemData : NamedItemData, new()
    {
        private int numToProcess, processedSoFar;

        protected GroupExporter(DefaultFileProvider provider) : base(provider) { }

        protected abstract string Type { get; }
        protected abstract TParsedName? ParseAssetName(string name);

        private void Report(IProgress<ExportProgress> progress, string current)
        {
            progress.Report(new ExportProgress
            {
                TotalSteps = numToProcess,
                CompletedSteps = processedSoFar,
                AssetsLoaded = assetsLoaded,
                CurrentItem = current
            });
        }

        public override Task ExportAssetsAsync(IProgress<ExportProgress> progress, IAssetOutput output)
        {
            var uniqueAssets = assetPaths.ToLookup(path => ParseAssetName(path)?.BaseName, StringComparer.OrdinalIgnoreCase);
            numToProcess = uniqueAssets.Count;

            Report(progress, string.Format(CultureInfo.CurrentCulture, Resources.Status_ExportingGroup, Type));

            return Parallel.ForEachAsync(uniqueAssets, async (grouping, _) =>
            {
                var baseName = grouping.Key;

                if (baseName == null)
                {
                    Console.WriteLine(Resources.Status_SkippingNullGroup, Type, grouping.Count());
                    return;
                }

                var primaryAssetPath = SelectPrimaryAsset(grouping);
                var file = provider[primaryAssetPath];

                var num = Interlocked.Increment(ref processedSoFar);
                Console.WriteLine(Resources.Status_ProcessingTypeGroupNumOfNum, Type, num, numToProcess);

                //Console.WriteLine("Loading {0}", file.PathWithoutExtension);
                Interlocked.Increment(ref assetsLoaded);

                Report(progress, file.PathWithoutExtension);

                var asset = await provider.LoadObjectAsync<TAsset>(file.PathWithoutExtension);

                if (asset == null)
                {
                    Console.WriteLine(Resources.Warning_FailedToLoadFile, file.PathWithoutExtension);
                    return;
                }

                if (!WantThisAsset(asset))
                {
                    Console.WriteLine(Resources.Status_SkippingEarlyAsInstructed, file.PathWithoutExtension);
                    return;
                }

                var fields = await ExtractCommonFieldsAsync(asset, grouping);

                LogAssetName(baseName, fields);

                foreach (var path in grouping)
                {
                    var templateId = $"{Type}:{Path.GetFileNameWithoutExtension(path)}";
                    var parsed = ParseAssetName(path);

                    if (parsed == null)
                    {
                        Console.WriteLine(Resources.Status_SkippingUnparsable, templateId, path);
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
                    };

                    if (!await ExportAssetAsync(parsed, asset, fields, path, itemData))
                    {
                        Console.WriteLine(Resources.Status_SkippingLateAsInstructed, templateId);
                        return;
                    }

                    output.AddNamedItem(templateId, itemData);
                }
            });
        }

        protected virtual Task<bool> ExportAssetAsync(TParsedName parsed, TAsset primaryAsset, TFields fields, string path, TItemData itemData)
        {
            return Task.FromResult(true);
        }

        protected virtual string SelectPrimaryAsset(IGrouping<string?, string> assetGroup)
        {
            return assetGroup.First();
        }

        protected virtual bool WantThisAsset(TAsset asset)
        {
            return true;
        }

        protected virtual Task<TFields> ExtractCommonFieldsAsync(TAsset asset, IGrouping<string?, string> grouping)
        {
            return Task.FromResult(new TFields() with
            {
                Description = asset.GetOrDefault<FText>("Description")?.Text,
                DisplayName = asset.GetOrDefault<FText>("DisplayName")?.Text ?? $"<{grouping.Key}>",
                SubType = null,
            });
        }

        protected virtual void LogAssetName(string baseName, TFields fields)
        {
            //Console.WriteLine("{0} is {1}", baseName, fields.DisplayName);
        }

        protected virtual EFortRarity GetRarity(TParsedName parsedName, TAsset primaryAsset, TFields fields) =>
            parsedName.Rarity switch
            {
                "C" => EFortRarity.Common,
                "R" => EFortRarity.Rare,
                "VR" => EFortRarity.Epic,
                "SR" => EFortRarity.Legendary,
                "UR" => EFortRarity.Mythic,
                _ => EFortRarity.Uncommon,
            };

        protected virtual string GetDisplayName(TParsedName parsedName, TAsset primaryAsset, TFields fields) => fields.DisplayName;

        protected virtual string? GetDescription(TParsedName parsedName, TAsset primaryAsset, TFields fields) => fields.Description;
    }

    internal abstract class GroupExporter<TAsset> : GroupExporter<TAsset, BaseParsedItemName, BaseItemGroupFields, NamedItemData>
        where TAsset : UObject
    {
        protected GroupExporter(DefaultFileProvider provider) : base(provider)
        {
        }
    }
}
