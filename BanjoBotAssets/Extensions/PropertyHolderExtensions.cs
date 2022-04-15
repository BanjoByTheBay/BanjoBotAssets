namespace BanjoBotAssets.Extensions
{
    internal static class PropertyHolderExtensions
    { /// <summary>
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
    }
}
