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
