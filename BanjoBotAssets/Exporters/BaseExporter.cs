namespace BanjoBotAssets.Exporters
{
    internal abstract class BaseExporter : IExporter
    {
        protected readonly AbstractVfsFileProvider provider;
        protected readonly ILogger logger;
        protected readonly List<string> assetPaths = new();

        protected int assetsLoaded;

        protected BaseExporter(AbstractVfsFileProvider provider, ILogger logger)
        {
            this.provider = provider;
            this.logger = logger;
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
