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

namespace BanjoBotAssets.Exporters.UObjects
{
    internal sealed class AbilityExporter(IExporterContext services) : UObjectExporter<UObject, AbilityItemData>(services)
    {
        private string? gadgetPath;
        private Dictionary<string, FStructFallback>? gadgetTable;

        protected override string Type => "Ability";

        protected override bool InterestedInAsset(string name)
        {
            if (Path.GetFileName(name).Equals("GadgetScaling.uasset", StringComparison.OrdinalIgnoreCase))
            {
                gadgetPath = name;
            }

            return (name.Contains("/Actives/", StringComparison.OrdinalIgnoreCase) || name.Contains("/Perks/", StringComparison.OrdinalIgnoreCase)) && name.Contains("/Kit_", StringComparison.OrdinalIgnoreCase);
        }

        public override async Task ExportAssetsAsync(IProgress<ExportProgress> progress, IAssetOutput output, CancellationToken cancellationToken)
        {
            gadgetTable = (await TryLoadTableAsync(gadgetPath))?.ToDictionary();
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
            var gadget = await gadgetPath.LoadAsync<UFortGadgetItemDefinition>(provider);

            var damageStats = gadget.DamageStatHandle;
            if (damageStats != null && gadgetTable?.TryGetValue(damageStats.RowName.Text, out var row) == true)
            {
                namedItemData.AbilityStats ??= new AbilityStats
                {
                    Damage = row.GetOrDefault<float>("DmgPB"),
                    EnvDamage = row.GetOrDefault<float>("EnvDmgPB"),
                    ImpactDamage = row.GetOrDefault<float>("ImpactDmgPB"),
                    BaseCritChance = row.GetOrDefault<float>("DiceCritChance"),
                    BaseCritDamage = row.GetOrDefault<float>("DiceCritDamageMultiplier"),
                    StunTime = row.GetOrDefault<float>("StunTime"),
                };
            }

            namedItemData.PreferredQuickbarSlot ??= gadget.GetOrDefault<int?>("PreferredQuickbarSlot");

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
            var staminaCost = abilityCosts.SingleOrDefault(ac => ac.CostSource == EFortAbilityCostSource.Stamina);
            namedItemData.EnergyCost ??= staminaCost?.CostValue?.GetScaledValue(logger);

            // load from cooldown effect
            Interlocked.Increment(ref assetsLoaded);
            var cooldownEffect = gaCdo.GetOrDefault<UBlueprintGeneratedClass>("CooldownGameplayEffectClass");
            Interlocked.Increment(ref assetsLoaded);
            var cooldownCdo = await cooldownEffect.ClassDefaultObject.LoadAsync();

            var dm = cooldownCdo?.GetOrDefault<FStructFallback>("DurationMagnitude");
            var sfm = dm?.GetOrDefault<FScalableFloat>("ScalableFloatMagnitude");
            namedItemData.CooldownSeconds ??= sfm?.GetScaledValue(logger);

            // load tooltip
            namedItemData.Description ??= await AbilityDescription.GetForActiveAbilityAsync(ga, gaCdo, this);
        }

        private async Task LoadFromGameplayEffectAsync(AbilityItemData namedItemData, FGameplayEffectApplicationInfoHard geaih)
        {
            // we only care about the gameplay effect that contains the granted tag
            Interlocked.Increment(ref assetsLoaded);
            var ge = await geaih.GameplayEffect.LoadAsync<UBlueprintGeneratedClass>();

            Interlocked.Increment(ref assetsLoaded);
            var cdo = await ge.ClassDefaultObject.LoadAsync();
            var tags = cdo?.GetOrDefault<FInheritedTagContainer?>("InheritableOwnedTagsContainer");
            namedItemData.GrantedTag ??= tags?.Added.First(t => t.ToString().StartsWith("Granted.Ability.", StringComparison.OrdinalIgnoreCase)).ToString();
        }
    }
}
