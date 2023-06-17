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
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.Textures;
using Microsoft.Extensions.Options;

namespace BanjoBotAssets.PostExporters
{
    internal sealed partial class ImageFilesPostExporter : IPostExporter
    {
        private readonly AbstractVfsFileProvider provider;
        private readonly IOptions<ImageExportOptions> options;
        private readonly ILogger<ImageFilesPostExporter> logger;

        private int assetsLoaded;

        public int AssetsLoaded => assetsLoaded;

        public ImageFilesPostExporter(AbstractVfsFileProvider provider, IOptions<ImageExportOptions> options, ILogger<ImageFilesPostExporter> logger)
        {
            this.provider = provider;
            this.options = options;
            this.logger = logger;
        }

        public void CountAssetLoaded()
        {
            Interlocked.Increment(ref assetsLoaded);
        }

        public async Task ProcessExportsAsync(ExportedAssets exportedAssets, IList<ExportedRecipe> exportedRecipes, CancellationToken cancellationToken = default)
        {
            var outputFilenameTransformer = new ConcurrentUniqueTransformer<string, string>(
                Path.GetFileName,
                IncrementFilenameSuffix,
                StringComparer.OrdinalIgnoreCase,
                StringComparer.OrdinalIgnoreCase);
            int filesWritten = 0, pathsUpdated = 0;

            Directory.CreateDirectory(options.Value.OutputDirectory);

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
                            // map the texture asset path to an output file path
                            bool novel = outputFilenameTransformer.TryTransformIfNovel(imagePath, out var transformedFilename);

                            // update the path in the NamedItem to point to the output file
                            var exportedPath = Path.Combine(options.Value.OutputDirectory, Path.ChangeExtension(transformedFilename, ".png"));
                            i.ImagePaths[imageType] = exportedPath;
                            pathsUpdated++;

                            if (!novel)
                            {
                                // the texture was already saved the first time
                                continue;
                            }

                            var asset = await provider.LoadObjectAsync<UTexture2D>(imagePath);
                            using var bitmap = asset.Decode();
                            if (bitmap == null)
                            {
                                logger.LogError(Resources.Error_CannotDecodeTexture, imagePath);
                                continue;
                            }

                            using var stream = new FileStream(exportedPath, FileMode.Create, FileAccess.Write);
                            if (!bitmap.Encode(stream, SkiaSharp.SKEncodedImageFormat.Png, 100))
                            {
                                logger.LogError(Resources.Error_CannotEncodeTexture, imagePath);
                            }
                            filesWritten++;
                        }
                    }
                }
            }

            logger.LogInformation(Resources.Status_WroteImageFilesUpdatedPaths, filesWritten, pathsUpdated);
        }

        private static readonly Regex numberSuffixedFilenameRegex = NumberSuffixedFilenameRegex();

        private static string IncrementFilenameSuffix(string filename)
        {
            if (numberSuffixedFilenameRegex.Match(filename) is { Success: true, Groups: var g })
            {
                // Foo_1.png -> Foo_2.png
                var num = int.Parse(g[2].Value, CultureInfo.InvariantCulture) + 1;
                return $"{g[1].Value}_{num}{g[3].Value}";
            }
            else
            {
                // Foo.png -> Foo_1.png
                return $"{Path.GetFileNameWithoutExtension(filename)}_1{Path.GetExtension(filename)}";
            }
        }

        [GeneratedRegex(@"^(.*)_(\d+)(\..+)?$", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
        private static partial Regex NumberSuffixedFilenameRegex();
    }
}
