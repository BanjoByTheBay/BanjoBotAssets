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
using BanjoBotAssets.Config;
using Microsoft.Extensions.Options;

namespace BanjoBotAssets.Exporters
{
    internal abstract class BaseExporter : IExporter
    {
        protected readonly List<string> assetPaths = [];
        protected int assetsLoaded;

        protected readonly AbstractVfsFileProvider provider;
        protected readonly ILogger logger;
        protected readonly IOptions<PerformanceOptions> performanceOptions;
        protected readonly IOptions<ScopeOptions> scopeOptions;
        protected readonly AbilityDescription abilityDescription;
        protected readonly CultureInfo exportCulture;

        protected BaseExporter(IExporterContext services)
        {
            provider = services.Provider;
            performanceOptions = services.PerformanceOptions;
            scopeOptions = services.ScopeOptions;
            logger = services.LoggerFactory.CreateLogger(GetType());
            abilityDescription = services.AbilityDescription;
            exportCulture = services.ExportCulture;
        }

        public int AssetsLoaded => assetsLoaded;

        public void CountAssetLoaded()
        {
            Interlocked.Increment(ref assetsLoaded);
        }

        protected async Task<UDataTable?> TryLoadTableAsync(string? path)
        {
            if (path == null)
                return null;

            var file = provider[path];
            CountAssetLoaded();
            return await provider.LoadObjectAsync<UDataTable>(file.PathWithoutExtension);
        }

        public abstract Task ExportAssetsAsync(IProgress<ExportProgress> progress, IAssetOutput output, CancellationToken cancellationToken);
        protected abstract bool InterestedInAsset(string name);

        public void ObserveAsset(string name)
        {
            if (InterestedInAsset(name))
            {
                assetPaths.Add(name);
            }
        }
    }
}
