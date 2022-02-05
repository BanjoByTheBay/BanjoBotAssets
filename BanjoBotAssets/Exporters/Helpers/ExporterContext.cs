using BanjoBotAssets.Config;
using BanjoBotAssets.Exporters.Options;
using Microsoft.Extensions.Options;

namespace BanjoBotAssets.Exporters.Helpers
{
    internal interface IExporterContext
    {
        AbstractVfsFileProvider Provider { get; }
        ILoggerFactory LoggerFactory { get; }
        IOptions<PerformanceOptions> PerformanceOptions { get; }
        IOptions<ScopeOptions> ScopeOptions { get; }
        AbilityDescription AbilityDescription { get; }
    }

    internal class ExporterContext : IExporterContext
    {
        public AbstractVfsFileProvider Provider { get; set; }

        public ILoggerFactory LoggerFactory { get; set; }

        public IOptions<PerformanceOptions> PerformanceOptions { get; set; }

        public AbilityDescription AbilityDescription { get; set; }
        public IOptions<ScopeOptions> ScopeOptions { get; set; }

        public ExporterContext(AbstractVfsFileProvider provider,
            ILoggerFactory loggerFactory, IOptions<PerformanceOptions> performanceOptions,
            AbilityDescription abilityDescription, IOptions<ScopeOptions> scopeOptions)
        {
            Provider = provider;
            LoggerFactory = loggerFactory;
            PerformanceOptions = performanceOptions;
            AbilityDescription = abilityDescription;
            ScopeOptions = scopeOptions;
        }
    }
}
