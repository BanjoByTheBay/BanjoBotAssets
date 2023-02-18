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
using BanjoBotAssets.Exporters.Blueprints;
using BanjoBotAssets.Exporters.Groups;
using BanjoBotAssets.Exporters.UObjects;

namespace BanjoBotAssets.Config
{
    public class ScopeOptions
    {
        /// <summary>
        /// A comma-separated list of Exporter class names to run. (By default, they all run.)
        /// </summary>
        public string? Only { get; set; }
        /// <summary>
        /// The maximum number of items for a <see cref="BlueprintExporter"/>, <see cref="GroupExporter{TAsset}"/>,
        /// or <see cref="UObjectExporter"/> to export. (By default, they're all exported.)
        /// </summary>
        public int? Limit { get; set; }
        /// <summary>
        /// Whether to keep an existing output file and merge in the newly exported items, rather than overwriting
        /// the file.
        /// </summary>
        public bool Merge { get; set; }
    }
}
