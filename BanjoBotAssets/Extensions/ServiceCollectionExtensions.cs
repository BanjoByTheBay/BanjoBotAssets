namespace BanjoBotAssets.Extensions
{
    internal static partial class ServiceCollectionExtensions
    {
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
