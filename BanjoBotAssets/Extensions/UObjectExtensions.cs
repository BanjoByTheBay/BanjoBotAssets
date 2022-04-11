using BanjoBotAssets.Exporters.Helpers;

namespace BanjoBotAssets.Extensions
{
    internal static class UObjectExtensions
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

            return default;
        }

        public static string? GetResourceObjectPath(this UObject obj, string property)
        {
            if (obj.GetOrDefault<FStructFallback>(property) is FStructFallback brush &&
                brush.GetOrDefault<FPackageIndex?>("ResourceObject") is FPackageIndex resobj &&
                resobj.ResolvedObject?.GetPathName() is string path)
            {
                return path;
            }

            return null;
        }
    }
}
