using CUE4Parse.FileProvider;

namespace BanjoBotAssets.Exporters
{
    internal abstract class BaseExporter : IExporter
    {
        protected readonly DefaultFileProvider provider;
        protected readonly List<string> assetPaths = new();

        protected int assetsLoaded;

        public BaseExporter(DefaultFileProvider provider)
        {
            this.provider = provider;
        }

        public int AssetsLoaded => assetsLoaded;

        public void CountAssetLoaded()
        {
            Interlocked.Increment(ref assetsLoaded);
        }

        public abstract Task ExportAssets(IProgress<ExportProgress> progress, ExportedAssets output);
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
