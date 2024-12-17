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
using BanjoBotAssets.UExports;
using CUE4Parse.FN.Structs.GA;
using CUE4Parse.UE4.Objects.Engine;
using System.Data;

namespace BanjoBotAssets.Exporters.UObjects
{
    internal sealed class AbilityExporter(IExporterContext services) : UObjectExporter<UObject, AbilityItemData>(services)
    {
        private string? gadgetPath;
        private string? meleePath;
        private string? rangedPath;
        private Dictionary<string, FStructFallback>? gadgetTable;
        private Dictionary<string, FStructFallback>? meleeTable;
        private Dictionary<string, FStructFallback>? rangedTable;

        protected override string Type => "Ability";

        protected override bool InterestedInAsset(string name)
        {
            if (name.Contains("DataTables/GadgetScaling.uasset", StringComparison.OrdinalIgnoreCase))
            {
                gadgetPath = name;
            }
            if (name.Contains("DataTables/MeleeWeapons.uasset", StringComparison.OrdinalIgnoreCase))
            {
                meleePath = name;
            }
            if (name.Contains("DataTables/RangedWeapons.uasset", StringComparison.OrdinalIgnoreCase))
            {
                rangedPath = name;
            }

            return (name.Contains("/Actives/", StringComparison.OrdinalIgnoreCase) || name.Contains("/Perks/", StringComparison.OrdinalIgnoreCase)) && name.Contains("/Kit_", StringComparison.OrdinalIgnoreCase);
        }

        public override async Task ExportAssetsAsync(IProgress<ExportProgress> progress, IAssetOutput output, CancellationToken cancellationToken)
        {
            gadgetTable = (await TryLoadTableAsync(gadgetPath))?.ToDictionary();
            meleeTable = (await TryLoadTableAsync(meleePath))?.ToDictionary();
            rangedTable = (await TryLoadTableAsync(rangedPath))?.ToDictionary();
            await base.ExportAssetsAsync(progress, output, cancellationToken);
        }

        protected override async Task<bool> ExportAssetAsync(UObject asset, AbilityItemData namedItemData, Dictionary<ImageType, string> imagePaths)
        {
            /* From the ability kit itself, we extract:
             *  - the icon (IconBrush)
             *  - the display name
             *  - the gadget (Gadgets[0], an FSoftObjectPath referencing a UFortGadgetItemDefinition)
             *  - the granted gameplay effect (GrantedGameplayEffects[0]->GameplayEffect, a UFortGameplayEffectItemDefinition)
             *
             * From the gadget, we extract:
             *  - damage stats (DamageStatHandle)
             *  - preferred quickbar slot (PreferredQuickbarSlot)
             *  - the gameplay ability (GameplayAbility)
             *
             * From the gameplay effect's ClassDefaultObject, we extract:
             *  - the granted tag (InheritableOwnedTagsContainer->Added[0])
             *
             * From the gameplay ability's ClassDefaultObject, we extract:
             *  - the energy cost (AbilityCosts)
             *  - the cooldown effect (CooldownGameplayEffectClass)
             *  - the tooltip (ToolTip)
             *
             * From the tooltip's ClassDefaultObject, we extract:
             *  - the description (Description)
             *
             * From the cooldown effect's ClassDefaultObject, we extract:
             *  - the cooldown time (DurationMagnitude->ScalableFloatMagnitude->Curve)
             */

            if (asset.GetResourceObjectPath("IconBrush") is string path)
            {
                imagePaths.Add(ImageType.Icon, path);
            }

            var gadgets = asset.GetOrDefault<FSoftObjectPath[]>("Gadgets");

            if (gadgets == null)
            {
                // not a hero ability, might be a hero perk
                var itemDescription = await abilityDescription.GetForPerkAbilityKitAsync(asset, this);
                if (itemDescription is null)// wasnt a hero perk
                    return false;
                namedItemData.Description ??= itemDescription;
                return true;
            }

            if (gadgets.Length != 1)
            {
                logger.LogWarning(Resources.Error_UnexpectedNumberOfGadgetsInAbility, asset.Name, gadgets.Length);
                return false;
            }

            var gameplayEffects = asset.GetOrDefault<FGameplayEffectApplicationInfoHard[]>("GrantedGameplayEffects");

            /* We only care about the gameplay effect that contains the granted tag, but there may be others, e.g.
             * Shock Tower also has 3 gameplay effects related to the fragment. */
            if (gameplayEffects.Length < 1)
            {
                logger.LogWarning(Resources.Error_NoGameplayEffectsInAbility, asset.Name);
                return false;
            }

            var loadFromGadgetTask = LoadFromGadgetAsync(namedItemData, gadgets[0]);
            var loadFromGameplayEffectTasks = gameplayEffects.Select(geaih => LoadFromGameplayEffectAsync(namedItemData, geaih));

            await loadFromGadgetTask;
            await Task.WhenAll(loadFromGameplayEffectTasks);

            return true;
        }

        private async Task LoadFromGadgetAsync(AbilityItemData namedItemData, FSoftObjectPath gadgetPath)
        {
            Interlocked.Increment(ref assetsLoaded);
            //logger.LogInformation($"{gadgetPath.AssetPathName} ==== {gadgetPath.SubPathString}");
            var gadget = await gadgetPath.LoadAsync<UFortGadgetItemDefinition>(provider);

            var damageStats = gadget.DamageStatHandle;
            if (damageStats != null)
            {
                FStructFallback? row = gadgetTable?.TryGetValue(damageStats.RowName.Text, out var gadgetRow) == true ? gadgetRow : null;
                row ??= meleeTable?.TryGetValue(damageStats.RowName.Text, out var meleeRow) == true ? meleeRow : null;
                row ??= rangedTable?.TryGetValue(damageStats.RowName.Text, out var rangedRow) == true ? rangedRow : null;

                if (row is not null)
                {
                    namedItemData.AbilityStats ??= new AbilityStats
                    {
                        Damage = row.GetOrDefault<float?>("DmgPB"),
                        EnvDamage = row.GetOrDefault<float?>("EnvDmgPB"),
                        ImpactDamage = row.GetOrDefault<float?>("ImpactDmgPB"),
                        BaseCritChance = row.GetOrDefault<float?>("DiceCritChance"),
                        BaseCritDamage = row.GetOrDefault<float?>("DiceCritDamageMultiplier"),
                        StunTime = row.GetOrDefault<float?>("StunTime"),
                        FireRate = row.GetOrDefault<float?>("FiringRate")
                    };
                }
            }
            namedItemData.AbilityStats ??= new();

            namedItemData.PreferredQuickbarSlot ??= gadget.GetOrDefaultFromDataList<int?>("PreferredQuickbarSlot");

            await LoadFromGameplayAbilityAsync(namedItemData, gadget.GameplayAbility);
        }

        private async Task LoadFromGameplayAbilityAsync(AbilityItemData namedItemData, FSoftObjectPath gameplayAbilityPath)
        {
            Interlocked.Increment(ref assetsLoaded);
            var ga = await gameplayAbilityPath.LoadAsync<UBlueprintGeneratedClass>(provider);
            Interlocked.Increment(ref assetsLoaded);
            var gaCdo = await ga.ClassDefaultObject.LoadAsync();

            if (gaCdo == null)
            {
                return;
            }

            var abilityCosts = gaCdo.GetOrDefault<FFortAbilityCost[]>("AbilityCosts");
            var staminaCost = abilityCosts?.SingleOrDefault(ac => ac.CostSource == EFortAbilityCostSource.Stamina);
            namedItemData.EnergyCost ??= staminaCost?.CostValue?.GetScaledValue(logger);

            // load from cooldown effect
            Interlocked.Increment(ref assetsLoaded);
            var cooldownEffect = gaCdo.GetOrDefault<UBlueprintGeneratedClass>("CooldownGameplayEffectClass");
            Interlocked.Increment(ref assetsLoaded);
            var cooldownCdo = cooldownEffect is not null ? (await cooldownEffect.ClassDefaultObject.LoadAsync()) : null;

            var dm = cooldownCdo?.GetOrDefault<FStructFallback>("DurationMagnitude");
            var sfm = dm?.GetOrDefault<FScalableFloat>("ScalableFloatMagnitude");
            namedItemData.CooldownSeconds ??= sfm?.GetScaledValue(logger);

            namedItemData.AbilityStats ??= new();

            // TODO: instead of hardcoding the definitions of each token, ideally we'd extract them from the ability asset itself
            // load extra ability stats from GameplayAbility
            // plasma pulse
            namedItemData.AbilityStats.Duration ??= GetStatFromGameplayAbility(gaCdo, "Pulse Duration");
            //namedItemData.AbilityStats.Debug = gaCdo.Properties.Find(p=>p.Name.Text=="Pulse Duration")?.PropertyType.Text ?? "=/";
            //namedItemData.AbilityStats.Debug += " [][] " + ;

            // teddy
            namedItemData.AbilityStats.FireRate ??= GetStatFromGameplayAbility(gaCdo, "F_BearRoundsPerSec");
            namedItemData.AbilityStats.Duration ??= GetStatFromGameplayAbility(gaCdo, "F_BearLifeSpanDefault", true);

            // frag grenade
            namedItemData.AbilityStats.Radius ??= GetStatFromGameplayAbility(gaCdo, "ExplosionRadiusDefault");

            //bull rush (using this because the text says 3 tiles and all other distances are only 2 tiles so \_(:/)_/ )
            namedItemData.AbilityStats.Duration ??= GetStatFromGameplayAbility(gaCdo, "UpgradeDistance");

            // generic duration
            namedItemData.AbilityStats.Duration ??= gaCdo.GetOrDefault<FScalableFloat>("AbilityDuration")?.GetScaledValue(logger);
            namedItemData.AbilityStats.Duration ??= gaCdo.GetOrDefault<FScalableFloat>("SF_AbilityDuration")?.GetScaledValue(logger);

            // load extra ability stats from Tooltip
            Interlocked.Increment(ref assetsLoaded);
            var tooltip = gaCdo.GetOrDefault<UBlueprintGeneratedClass?>("ToolTip");

            Interlocked.Increment(ref assetsLoaded);
            UObject? tooltipCdo = tooltip is null ? null : await tooltip.ClassDefaultObject.LoadAsync();

            // war cry
            namedItemData.AbilityStats.Damage ??= GetStatFromTooltip(tooltipCdo, "SF_DamageMult");
            namedItemData.AbilityStats.AbilityLine4 ??= GetStatFromTooltip(tooltipCdo, "SF_AttackSpeed", true);
            namedItemData.AbilityStats.FireRate ??=
                namedItemData.AbilityStats.AbilityLine5 ??= GetStatFromTooltip(tooltipCdo, "SF_FireRate", true);

            // goin constructor
            namedItemData.AbilityStats.AbilityLine2 ??= GetStatFromTooltip(tooltipCdo, "ShieldBlock_Percentage");
            namedItemData.AbilityStats.AbilityLine3 ??= GetStatFromTooltip(tooltipCdo, "BaseArmor_Value", true);

            // rosie
            namedItemData.AbilityStats.Duration ??= GetStatFromTooltip(tooltipCdo, "TotalShots");

            // bull rush
            //namedItemData.AbilityStats.Distance ??= GetStatFromTooltip(tooltipCdo, "SF_Distance");

            //phase shift
            namedItemData.AbilityStats.AbilityLine2 ??= GetStatFromTooltip(tooltipCdo, "Row_MovementDuration");
            namedItemData.AbilityStats.AbilityLine3 ??= GetStatFromTooltip(tooltipCdo, "Row_Movementspeed", true);

            // generic duration
            namedItemData.AbilityStats.Duration ??= GetStatFromTooltip(tooltipCdo, "SF_Duration");

            // generate tooltip text
            namedItemData.Description ??= await AbilityDescription.GetForActiveAbilityAsync(ga, gaCdo, this, tooltipCdo, namedItemData.AbilityStats);
        }

        private Dictionary<string, float>? GetStatDictionaryFromGameplayEffectCdo(UObject? modifierGeCdo)
        {
            var modifiers = modifierGeCdo?.GetOrDefault<FStructFallback[]>("Modifiers");
            return modifiers
                ?.Select(m => m
                    .GetOrDefault<FStructFallback>("ModifierMagnitude")
                    ?.GetOrDefault<FScalableFloat>("ScalableFloatMagnitude")
                    )
                .Where(f => f is not null)
                .GroupBy(f => f?.Curve.RowName)
                .Select(f => new KeyValuePair<string, float>(f.Key?.Text!, f.First()!.GetScaledValue(logger)))
                .ToDictionary();
        }

        private bool tooltipStatLastSuccess;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tooltipCdo"></param>
        /// <param name="property"></param>
        /// <param name="chain"><see langword="true"/> if this stat is dependent on a previous stat being found</param>
        /// <returns></returns>
        private float? GetStatFromTooltip(UObject? tooltipCdo, string property, bool chain = false)
        {
            if (chain && !tooltipStatLastSuccess)
                return null;
            var sf = tooltipCdo?.GetOrDefault<FScalableFloat>(property);
            float? result = sf?.Curve.RowName is null ? null : sf.GetScaledValue(logger);
            tooltipStatLastSuccess = result is not null;
            return result;
        }

        private bool gameplayAbilityStatLastSuccess;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gaCdo"></param>
        /// <param name="property"></param>
        /// <param name="chain"><see langword="true"/> if this stat is dependent on a previous stat being found</param>
        /// <returns></returns>
        private float? GetStatFromGameplayAbility(UObject gaCdo, string property, bool chain = false)
        {
            if (chain && !gameplayAbilityStatLastSuccess)
                return null;

            var matchedProp = gaCdo.Properties.Find(p => p.Name.Text == property);
            //scuffed workaround because GetOrDefault was being weird
            float? result = matchedProp?.PropertyType.Text switch
            {
                "DoubleProperty" => (float?)matchedProp?.Tag?.GetValue<double>(),
                "IntProperty" => matchedProp?.Tag?.GetValue<int>(),
                "LongProperty" => matchedProp?.Tag?.GetValue<long>(),
                _ => null
            };
            gameplayAbilityStatLastSuccess = result is not null;
            return result;
        }

        private async Task LoadFromGameplayEffectAsync(AbilityItemData namedItemData, FGameplayEffectApplicationInfoHard geaih)
        {
            // we only care about the gameplay effect that contains the granted tag
            Interlocked.Increment(ref assetsLoaded);
            var ge = await geaih.GameplayEffect.LoadAsync<UBlueprintGeneratedClass>();

            Interlocked.Increment(ref assetsLoaded);
            var cdo = await ge.ClassDefaultObject.LoadAsync();
            var tags = cdo?.GetOrDefault<FInheritedTagContainer?>("InheritableOwnedTagsContainer");
            var tagToAdd = tags?.Added.FirstOrDefault(t => t.ToString().StartsWith("Granted.Ability.", StringComparison.OrdinalIgnoreCase)) ?? default;
            if (tagToAdd != default)
                namedItemData.GrantedTag ??= tagToAdd.ToString();
        }
    }
}
