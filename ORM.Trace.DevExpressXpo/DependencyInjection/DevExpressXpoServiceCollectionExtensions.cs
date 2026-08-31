using Microsoft.Extensions.DependencyInjection;
using ORM.Trace.Xpo;
using ORM.Trace.Runtime;

namespace ORM.Trace.DependencyInjection
{
    /// <summary>
    /// Provides dependency-injection registration for DevExpress XPO query tracing.
    /// </summary>
    public static class DevExpressXpoServiceCollectionExtensions
    {
        /// <summary>
        /// Adds DevExpress XPO logging to the shared ORM Trace telemetry pipeline.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        /// <returns>The same service collection for chaining.</returns>
        public static IServiceCollection AddOrmTraceDevExpressXpo(this IServiceCollection services)
        {
            services.AddHostedService<XpoTelemetryRegistrationService>();
            return services;
        }

    }

    /// <summary>
    /// Provides DevExpress XPO integration for manually managed ORM Trace runtimes.
    /// </summary>
    public static class DevExpressXpoRuntimeExtensions
    {
        /// <summary>
        /// Adds DevExpress XPO query tracing to a manual ORM Trace runtime.
        /// </summary>
        /// <param name="builder">The runtime builder to configure.</param>
        /// <returns>The same builder for chaining.</returns>
        public static OrmTraceRuntimeBuilder UseDevExpressXpo(this OrmTraceRuntimeBuilder builder)
        {
            builder.ServiceCollection.AddOrmTraceDevExpressXpo();
            return builder;
        }
    }
}
