using System.Text;

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

            return default;
        }

        public static string CapitalizeFirst(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            var sb = new StringBuilder(str);

            sb[0] = char.ToUpper(sb[0]);

            for (int i = 1; i < sb.Length; i++)
                sb[i] = char.ToLower(sb[i]);

            return sb.ToString();
        }
    }
}
