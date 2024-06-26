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
    internal static class PropertyHolderExtensions
    {
        /// <summary>
        /// Gets the path contained in the <c>ResourceObject</c> property of another named property.
        /// </summary>
        /// <param name="obj">The object to search.</param>
        /// <param name="property">The name of the property that contains the <c>ResourceObject</c>.</param>
        /// <returns><c><paramref name="obj"/>.<paramref name="property"/>.<see cref="FPackageIndex">ResourceObject</see>.<see cref="ResolvedObject.GetPathName(bool)">GetPathName()</see></c>
        /// if it exists, otherwise <see langword="null"/>.</returns>
        public static string? GetResourceObjectPath(this IPropertyHolder obj, string property)
        {
            if (PropertyUtil.GetOrDefault<FStructFallback>(obj, property) is FStructFallback brush &&
                PropertyUtil.GetOrDefault<FPackageIndex?>(brush, "ResourceObject") is FPackageIndex resobj &&
                resobj.ResolvedObject?.GetPathName() is string path)
            {
                return path;
            }

            return null;
        }

        /// <summary>
        /// Gets the asset path contained in an <see cref="FSoftObjectPath"/> in a named property.
        /// </summary>
        /// <param name="obj">The object to search.</param>
        /// <param name="property">The name of the soft object property.</param>
        /// <returns><c><paramref name="obj"/>.<paramref name="property"/>.<see cref="FSoftObjectPath.AssetPathName">AssetPathName</see>.<see cref="FName.Text">Text</see></c>
        /// if it exists, otherwise <see langword="null"/>.</returns>
        public static string? GetSoftAssetPath(this IPropertyHolder obj, string property) =>
            PropertyUtil.GetOrDefault<FSoftObjectPath>(obj, property).AssetPathName switch
            {
                { IsNone: false, Text: var t } => t,
                _ => null
            };

        public static string? GetSoftAssetPathFromDataList(this IPropertyHolder obj, string property)
        {
            var dataList = PropertyUtil.GetOrDefault<FInstancedStruct[]>(obj, "DataList");
            if(dataList is null)
                return null;
            foreach (var item in dataList)
            {
                if (item.NonConstStruct?.Properties.Exists(p => p.Name.Text == property) == true)
                {
                    return PropertyUtil.GetOrDefault<FSoftObjectPath>(item.NonConstStruct, property).AssetPathName switch
                    {
                        { IsNone: false, Text: var t } => t,
                        _ => null
                    };
                }
            }
            return null;
        }

    }
}
