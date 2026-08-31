using ORM.Trace.Telemetry;

namespace ORM.Trace.Transport
{
    /// <summary>
    /// Sends captured query telemetry to an ORM Trace service.
    /// </summary>
    public interface IOrmTraceSender
    {
        /// <summary>
        /// Sends one telemetry batch.
        /// </summary>
        /// <param name="batch">The queries to send.</param>
        /// <param name="cancellationToken">A token that cancels the request.</param>
        /// <returns>A task representing the send operation.</returns>
        Task SendAsync(IReadOnlyList<QueryTelemetry> batch, CancellationToken cancellationToken);
    }
}