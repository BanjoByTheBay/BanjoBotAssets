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
    }
}
