using DevExpress.Xpo.Logger;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ORM.Trace.Collection;
using ORM.Trace.Configuration;

namespace ORM.Trace.Xpo
{
    internal sealed class XpoTelemetryRegistrationService : IHostedService
    {
        private readonly IQueryCollector collector;
        private readonly OrmTraceOptions options;
        private readonly IHttpContextAccessor httpContextAccessor;
        private ILogger? previousLogger;

        public XpoTelemetryRegistrationService(IQueryCollector collector, IOptions<OrmTraceOptions> options, IHttpContextAccessor httpContextAccessor)
        {
            this.collector = collector;
            this.options = options.Value;
            this.httpContextAccessor = httpContextAccessor;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            previousLogger = LogManager.HasTransport ? LogManager.LogServer : null;
            LogManager.SetTransport(new XpoTelemetryLogger(collector, options, httpContextAccessor, previousLogger));
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (previousLogger is not null)
                LogManager.SetTransport(previousLogger);
            else
                LogManager.ResetTransport();

            return Task.CompletedTask;
        }
    }
}
