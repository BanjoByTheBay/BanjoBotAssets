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
namespace BanjoBotAssets.Extensions
{
    internal static class UObjectExtensions
    {
        /// <summary>
        /// Gets a property value from a <see cref="UObject"/>, potentially following the
        /// <see cref="UObject.Template">Template</see> chain to find an inherited property value if necessary.
        /// </summary>
        /// <typeparam name="T">The expected type of the property.</typeparam>
        /// <param name="obj">The <see cref="UObject"/> to search.</param>
        /// <param name="name">The property name.</param>
        /// <param name="assetCounter">A counter used to track the assets loaded during this search.</param>
        /// <returns>The property value, or <see langword="null"/> if the property wasn't found on <paramref name="obj"/> or any of its template ancestors.</returns>
        public static async Task<T?> GetInheritedOrDefaultAsync<T>(this UObject obj, string name, IAssetCounter assetCounter)
        {
            if (obj.GetOrDefault<T>(name) is T ret && !ret.Equals(default(T)))
                return ret;

            if (obj.Template != null)
            {
                assetCounter.CountAssetLoaded();
                var template = await obj.Template.LoadAsync();

                if (template != null)
                {
                    return await template.GetInheritedOrDefaultAsync<T>(name, assetCounter);
                }
            }

            return default;
        }

        /// <summary>
        /// Gets the path contained in the <c>ResourceObject</c> property of another named property,
        /// potentially following the <see cref="UObject.Template">Template</see> chain to find an
        /// inherited property value if necessary.
        /// </summary>
        /// <param name="obj">The object to search.</param>
        /// <param name="property">The name of the property that contains the <c>ResourceObject</c>.</param>
        /// <param name="assetCounter">A counter used to track the assets loaded during this search.</param>
        /// <returns><c><paramref name="obj"/>.<paramref name="property"/>.<see cref="FPackageIndex">ResourceObject</see>.<see cref="ResolvedObject.GetPathName(bool)">GetPathName()</see></c>
        /// if it exists on <paramref name="obj"/> or any of its template ancestors, otherwise <see langword="null"/>.</returns>
        public static async Task<string?> GetInheritedResourceObjectPathAsync(this UObject obj, string property, IAssetCounter assetCounter)
        {
            if (await obj.GetInheritedOrDefaultAsync<FStructFallback>(property, assetCounter) is FStructFallback brush &&
                PropertyUtil.GetOrDefault<FPackageIndex?>(brush, "ResourceObject") is FPackageIndex resobj &&
                resobj.ResolvedObject?.GetPathName() is string path)
            {
                return path;
            }

            return null;
        }
    }
}
