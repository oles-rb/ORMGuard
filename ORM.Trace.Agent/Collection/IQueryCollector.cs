using ORM.Trace.Telemetry;

namespace ORM.Trace.Collection
{

    /// <summary>
    /// Buffers captured queries until the telemetry sender consumes them.
    /// </summary>
    public interface IQueryCollector
    {
        /// <summary>
        /// Attempts to enqueue a captured query without blocking.
        /// </summary>
        /// <param name="telemetry">The query telemetry to enqueue.</param>
        /// <returns><see langword="true"/> when the item was accepted; otherwise <see langword="false"/>.</returns>
        bool TryAdd(QueryTelemetry telemetry);

        /// <summary>
        /// Reads buffered queries as they become available.
        /// </summary>
        /// <param name="cancellationToken">A token that stops the asynchronous enumeration.</param>
        /// <returns>An asynchronous sequence of captured queries.</returns>
        IAsyncEnumerable<QueryTelemetry> ReadAllAsync(CancellationToken cancellationToken);
    }
}
