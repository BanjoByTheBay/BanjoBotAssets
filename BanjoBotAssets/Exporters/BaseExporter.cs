using BanjoBotAssets.Exporters.Helpers;
using BanjoBotAssets.Exporters.Options;
using Microsoft.Extensions.Options;

namespace BanjoBotAssets.Exporters
{
    internal abstract class BaseExporter : IExporter
    {
        protected readonly List<string> assetPaths = new();
        protected int assetsLoaded;

        protected readonly AbstractVfsFileProvider provider;
        protected readonly ILogger logger;
        protected readonly IOptions<PerformanceOptions> performanceOptions;
        protected readonly AbilityDescription abilityDescription;

        protected BaseExporter(IExporterContext services)
        {
            provider = services.Provider;
            performanceOptions = services.PerformanceOptions;
            logger = services.LoggerFactory.CreateLogger(GetType().FullName ?? "");
            abilityDescription = services.AbilityDescription;
        }

        public int AssetsLoaded => assetsLoaded;

        public void CountAssetLoaded()
        {
            Interlocked.Increment(ref assetsLoaded);
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
