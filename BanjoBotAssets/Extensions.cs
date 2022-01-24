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

        public static IServiceCollection AddDerivedServices<TService, TImplementationBase>(this IServiceCollection services, ServiceLifetime lifetime)
            where TService : class
            where TImplementationBase : class
        {
            var types = from t in typeof(TImplementationBase).Assembly.GetTypes()
                        where typeof(TImplementationBase).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract
                        select t;

            foreach (var t in types)
            {
                services.Add(new ServiceDescriptor(typeof(TService), t, lifetime));
            }

            return services;
        }
    }
}
