using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ORM.Trace.Configuration;
using ORM.Trace.DependencyInjection;

namespace ORM.Trace.Runtime
{

    /// <summary>
    /// Manual ORM Trace lifecycle for WinForms, WPF, console and other applications without a host or DI setup.
    /// </summary>
    public sealed class OrmTraceRuntimeBuilder
    {
        internal IServiceCollection ServiceCollection { get; } = new ServiceCollection();

        internal OrmTraceRuntimeBuilder(OrmTraceOptions options)
        {
            ServiceCollection.AddLogging();
            ServiceCollection.AddOrmTrace(target => Copy(options, target));
        }

        /// <summary>
        /// Builds the agent services and starts telemetry processing.
        /// </summary>
        /// <returns>A runtime handle that stops and disposes the agent.</returns>
        public OrmTraceRuntime Start()
        {
            var provider = ServiceCollection.BuildServiceProvider();
            var hostedServices = provider.GetServices<IHostedService>().ToArray();
            var started = new List<IHostedService>(hostedServices.Length);
            try
            {
                foreach (var service in hostedServices)
                {
                    service.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
                    started.Add(service);
                }
                return new OrmTraceRuntime(provider, started);
            }
            catch
            {
                foreach (var service in started.AsEnumerable().Reverse())
                    service.StopAsync(CancellationToken.None).GetAwaiter().GetResult();
                provider.Dispose();
                throw;
            }
        }

        private static void Copy(OrmTraceOptions source, OrmTraceOptions target)
        {
            target.ApiKey = source.ApiKey;
            target.Endpoint = source.Endpoint;
            target.BatchSize = source.BatchSize;
            target.FlushInterval = source.FlushInterval;
            target.SlowQueryThresholdMs = source.SlowQueryThresholdMs;
            target.SamplingRate = source.SamplingRate;
            target.CaptureParameterValues = source.CaptureParameterValues;
            target.QueueCapacity = source.QueueCapacity;
            target.Environment = source.Environment;
            target.ServiceName = source.ServiceName;
            target.Version = source.Version;
            target.HttpTimeout = source.HttpTimeout;
        }
    }

    /// <summary>
    /// Owns a manually configured ORM Trace agent and its background services.
    /// </summary>
    public sealed class OrmTraceRuntime : IDisposable
    {
        private readonly ServiceProvider provider;
        private readonly IReadOnlyList<IHostedService> hostedServices;
        private bool disposed;

        internal OrmTraceRuntime(ServiceProvider provider, IReadOnlyList<IHostedService> hostedServices)
        {
            this.provider = provider;
            this.hostedServices = hostedServices;
        }

        /// <summary>
        /// Creates a runtime builder for applications that do not use dependency injection.
        /// </summary>
        /// <param name="options">The agent configuration.</param>
        /// <returns>A builder that can be extended with ORM-specific integrations.</returns>
        public static OrmTraceRuntimeBuilder Create(OrmTraceOptions options) => new(options);

        /// <summary>
        /// Resolves a required service from the agent's internal service provider.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <returns>The registered service instance.</returns>
        public T GetRequiredService<T>() where T : notnull => provider.GetRequiredService<T>();

        /// <summary>
        /// Stops background processing and releases all runtime resources.
        /// </summary>
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            foreach (var service in hostedServices.Reverse())
            {
                try { service.StopAsync(timeout.Token).GetAwaiter().GetResult(); }
                catch (OperationCanceledException) when (timeout.IsCancellationRequested) { }
            }
            provider.Dispose();
        }
    }
}
