using System.Threading.Channels;
using Microsoft.Extensions.Options;
using ORM.Trace.Configuration;
using ORM.Trace.Telemetry;

namespace ORM.Trace.Collection
{
    /// <summary>
    /// Provides a bounded, thread-safe in-memory query telemetry queue.
    /// </summary>
    public sealed class QueryCollector : IQueryCollector
    {
        private readonly Channel<QueryTelemetry> _channel;

        /// <summary>
        /// Initializes a collector with the configured queue capacity.
        /// </summary>
        /// <param name="options">The agent configuration.</param>
        public QueryCollector(IOptions<OrmTraceOptions> options)
        {
            _channel = Channel.CreateBounded<QueryTelemetry>(
                new BoundedChannelOptions(options.Value.QueueCapacity)
                {
                    FullMode = BoundedChannelFullMode.DropWrite,
                    SingleReader = true,
                    SingleWriter = false
                });
        }

        /// <inheritdoc />
        public bool TryAdd(QueryTelemetry telemetry) =>
            _channel.Writer.TryWrite(telemetry);

        /// <inheritdoc />
        public IAsyncEnumerable<QueryTelemetry> ReadAllAsync(CancellationToken cancellationToken) =>
            _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
