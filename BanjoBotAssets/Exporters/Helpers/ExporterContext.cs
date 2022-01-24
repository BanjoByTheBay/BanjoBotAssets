using Microsoft.Extensions.Options;

namespace BanjoBotAssets.Exporters.Impl
{
    internal interface IExporterContext
    {
        AbstractVfsFileProvider Provider { get; }
        ILoggerFactory LoggerFactory { get; }
        IOptions<PerformanceOptions> PerformanceOptions { get; }
        AbilityDescription AbilityDescription { get; }
    }

    internal class ExporterContext : IExporterContext
    {
        public AbstractVfsFileProvider Provider { get; set; }

        public ILoggerFactory LoggerFactory { get; set; }

        public IOptions<PerformanceOptions> PerformanceOptions { get; set; }

        public AbilityDescription AbilityDescription { get; set; }

        public ExporterContext(AbstractVfsFileProvider provider, ILoggerFactory loggerFactory,
            IOptions<PerformanceOptions> performanceOptions, AbilityDescription abilityDescription)
        {
            Provider = provider;
            LoggerFactory = loggerFactory;
            PerformanceOptions = performanceOptions;
            AbilityDescription = abilityDescription;
        }
    }
}
