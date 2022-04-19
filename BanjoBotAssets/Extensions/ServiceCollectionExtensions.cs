using System.Reflection;

// TODO: use a source generator instead of ServiceCollectionExtensions.AddDerivedServices

namespace BanjoBotAssets.Extensions
{
    internal static partial class ServiceCollectionExtensions
    {
        private static bool HasSoloAttribute(Type t) => t.GetCustomAttribute<SoloForTestingAttribute>(false) != null;

        public static IServiceCollection AddDerivedServices<TService, TImplementationBase>(this IServiceCollection services, ServiceLifetime lifetime)
            where TService : class
            where TImplementationBase : class
        {
            var types = from t in typeof(TImplementationBase).Assembly.GetTypes()
                        where typeof(TImplementationBase).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract
                        select t;

            if (types.Any(HasSoloAttribute))
            {
                types = types.Where(HasSoloAttribute);
            }

            foreach (var t in types)
            {
                services.Add(new ServiceDescriptor(typeof(TService), t, lifetime));
            }

            return services;
        }
    }
}
