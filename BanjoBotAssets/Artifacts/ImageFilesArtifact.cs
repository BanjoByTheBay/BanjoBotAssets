using BanjoBotAssets.Config;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.Textures;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace BanjoBotAssets.Artifacts
{
    internal sealed class ImageFilesArtifact : IExportArtifact
    {
        private readonly AbstractVfsFileProvider provider;
        private readonly IOptions<ImageExportOptions> options;
        private readonly ILogger<ImageFilesArtifact> logger;

        public ImageFilesArtifact(AbstractVfsFileProvider provider, IOptions<ImageExportOptions> options, ILogger<ImageFilesArtifact> logger)
        {
            this.provider = provider;
            this.options = options;
            this.logger = logger;
        }

        private string ConvertPath(string texturePath)
        {
            return Path.Combine(options.Value.OutputDirectory, texturePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        }

        public async Task RunAsync(ExportedAssets exportedAssets, IList<ExportedRecipe> exportedRecipes, CancellationToken cancellationToken = default)
        {
            var timesEncountered = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var (imageType, wantExport) in options.Value.Type)
            {
                if (wantExport == WantImageExport.Yes)
                {
                    logger.LogInformation(Resources.Status_ExportingImagesType, imageType);

                    foreach (var i in exportedAssets.NamedItems.Values)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (i.ImagePaths?.TryGetValue(imageType, out var imagePath) == true)
                        {
                            if (timesEncountered.AddOrUpdate(imagePath, 1, (_, x) => x + 1) != 1)
                            {
                                continue;
                            }

                            var asset = await provider.LoadObjectAsync<UTexture2D>(imagePath);
                            using var bitmap = asset.Decode();
                            if (bitmap == null)
                            {
                                logger.LogError(Resources.Error_CannotDecodeTexture, imagePath);
                                continue;
                            }

                            var exportedPath = Path.ChangeExtension(ConvertPath(imagePath), ".png");
                            if (Path.GetDirectoryName(exportedPath) is string dir)
                            {
                                Directory.CreateDirectory(dir);
                            }

                            using var stream = new FileStream(exportedPath, FileMode.Create, FileAccess.Write);
                            if (!bitmap.Encode(stream, SkiaSharp.SKEncodedImageFormat.Png, 100))
                            {
                                logger.LogError(Resources.Error_CannotEncodeTexture, imagePath);
                            }
                        }
                    }
                }
            }
        }
    }
}
