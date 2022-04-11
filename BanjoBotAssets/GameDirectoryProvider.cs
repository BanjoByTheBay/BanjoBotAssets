using BanjoBotAssets.Config;
using Microsoft.Extensions.Options;

namespace BanjoBotAssets
{
    internal sealed class GameDirectoryProvider
    {
        private readonly Lazy<GameDirectory> lazyGameDirectory;

        public GameDirectoryProvider(IOptions<GameFileOptions> options)
        {
            lazyGameDirectory = new Lazy<GameDirectory>(() =>
            {
                try
                {
                    return new GameDirectory(options.Value.GameDirectories.First(Directory.Exists));
                }
                catch (InvalidOperationException ex)
                {
                    throw new InvalidOperationException(Resources.Error_GameNotFound, ex);
                }
            });
        }

        public GameDirectory GetGameDirectory() => lazyGameDirectory.Value;
    }

    internal sealed class GameDirectory
    {
        private readonly Lazy<DateTime> lazyLastWriteTime;

        public GameDirectory(string path)
        {
            Path = path;

            lazyLastWriteTime = new Lazy<DateTime>(() => new DirectoryInfo(Path).GetFiles().Max(f => f.LastWriteTime));
        }

        public string Path { get; }

        public DateTime LastWriteTime => lazyLastWriteTime.Value;
    }
}
