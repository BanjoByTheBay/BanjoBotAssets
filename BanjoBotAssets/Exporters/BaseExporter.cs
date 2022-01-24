using Microsoft.Extensions.Options;

namespace BanjoBotAssets.Exporters.Impl
{
    internal abstract class BaseExporter : IExporter
    {
        protected readonly AbstractVfsFileProvider provider;
        protected readonly ILogger logger;
        protected readonly List<string> assetPaths = new();
        protected readonly IOptions<PerformanceOptions> performanceOptions;

        protected int assetsLoaded;

        protected BaseExporter(IExporterContext services)
        {
            (provider, logger, performanceOptions) = (services.Provider, services.LoggerFactory.CreateLogger(GetType().FullName ?? ""), services.PerformanceOptions);
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
