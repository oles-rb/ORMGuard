using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ORM.Trace.Background;
using ORM.Trace.Collection;
using ORM.Trace.Configuration;
using ORM.Trace.Transport;

namespace ORM.Trace.DependencyInjection
{

    /// <summary>
    /// Provides dependency-injection registration for the ORM Trace agent.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the ORM Trace telemetry pipeline and its background sender.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        /// <param name="configure">A delegate that configures the agent.</param>
        /// <returns>The same service collection for chaining.</returns>
        public static IServiceCollection AddOrmTrace(this IServiceCollection services, Action<OrmTraceOptions> configure)
        {
            services.Configure(configure);
            services.AddHttpContextAccessor();
            services.AddSingleton<IQueryCollector, QueryCollector>();
            services.AddHttpClient<IOrmTraceSender, OrmTraceSender>();
            services.AddHostedService<TelemetryWorker>();

            return services;
        }
    }
}
