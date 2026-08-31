using Microsoft.Extensions.DependencyInjection;
using ORM.Trace.Interceptors;
using ORM.Trace.Runtime;

namespace ORM.Trace.DependencyInjection
{

    /// <summary>
    /// Provides dependency-injection registration for EF Core query tracing.
    /// </summary>
    public static class EntityFrameworkCoreServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the EF Core command interceptor to the shared ORM Trace telemetry pipeline.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        /// <returns>The same service collection for chaining.</returns>
        public static IServiceCollection AddOrmTraceEntityFrameworkCore(this IServiceCollection services)
        {
            services.AddSingleton<OrmTraceCommandInterceptor>();
            return services;
        }
    }

    /// <summary>
    /// Provides EF Core integration for manually managed ORM Trace runtimes.
    /// </summary>
    public static class EntityFrameworkCoreRuntimeExtensions
    {
        /// <summary>
        /// Adds EF Core query tracing to a manual ORM Trace runtime.
        /// </summary>
        /// <param name="builder">The runtime builder to configure.</param>
        /// <returns>The same builder for chaining.</returns>
        public static OrmTraceRuntimeBuilder UseEntityFrameworkCore(this OrmTraceRuntimeBuilder builder)
        {
            builder.ServiceCollection.AddOrmTraceEntityFrameworkCore();
            return builder;
        }

        /// <summary>
        /// Gets the EF Core command interceptor managed by the runtime.
        /// </summary>
        /// <param name="runtime">The active ORM Trace runtime.</param>
        /// <returns>The interceptor to add to an EF Core <c>DbContextOptionsBuilder</c>.</returns>
        public static OrmTraceCommandInterceptor GetEntityFrameworkCoreInterceptor(this OrmTraceRuntime runtime) =>
            runtime.GetRequiredService<OrmTraceCommandInterceptor>();
    }
}
