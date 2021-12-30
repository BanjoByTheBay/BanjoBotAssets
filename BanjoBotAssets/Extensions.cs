namespace BanjoBotAssets
{
    internal static class Extensions
    {
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
                    return await GetInheritedOrDefaultAsync<T>(template, name, assetCounter);
                }
            }

            return default(T);
        }

    }
}
