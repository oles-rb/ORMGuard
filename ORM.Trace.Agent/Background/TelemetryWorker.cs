using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ORM.Trace.Collection;
using ORM.Trace.Configuration;
using ORM.Trace.Telemetry;
using ORM.Trace.Transport;

namespace ORM.Trace.Background
{
    /// <summary>
    /// Collects queued query telemetry into batches and sends it in the background.
    /// </summary>
    public sealed class TelemetryWorker : BackgroundService
    {
        private readonly IQueryCollector _collector;
        private readonly IOrmTraceSender _sender;
        private readonly OrmTraceOptions _options;
        private readonly ILogger<TelemetryWorker> _logger;

        /// <summary>
        /// Initializes the telemetry background worker.
        /// </summary>
        /// <param name="collector">The source of captured queries.</param>
        /// <param name="sender">The telemetry batch sender.</param>
        /// <param name="options">The agent configuration.</param>
        /// <param name="logger">The worker logger.</param>
        public TelemetryWorker(IQueryCollector collector, IOrmTraceSender sender, IOptions<OrmTraceOptions> options, ILogger<TelemetryWorker> logger)
        {
            _collector = collector;
            _sender = sender;
            _options = options.Value;
            _logger = logger;
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ORM Trace Agent started");

            var batch = new List<QueryTelemetry>(_options.BatchSize);

            var reader = _collector
                .ReadAllAsync(stoppingToken)
                .GetAsyncEnumerator(stoppingToken);

            var readTask = reader.MoveNextAsync().AsTask();
            var timerTask = Task.Delay(_options.FlushInterval, stoppingToken);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.WhenAny(readTask, timerTask);

                    if (readTask.IsCompleted)
                    {
                        if (!await readTask)
                            break;

                        batch.Add(reader.Current);

                        if (batch.Count >= _options.BatchSize)
                            await FlushAsync(batch, stoppingToken);

                        readTask = reader.MoveNextAsync().AsTask();
                    }

                    if (timerTask.IsCompleted)
                    {
                        if (batch.Count > 0)
                            await FlushAsync(batch, stoppingToken);

                        timerTask = Task.Delay(_options.FlushInterval, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogDebug("ORM Trace Agent stopping");
            }
            finally
            {
                try
                {
                    await reader.DisposeAsync();
                }
                catch (NotSupportedException) when (stoppingToken.IsCancellationRequested)
                {
                    // ChannelReader's async iterator can reject disposal while its
                    // final MoveNext is completing during host shutdown.
                }
            }

            if (batch.Count > 0)
            {
                using var shutdownFlush = new CancellationTokenSource(_options.HttpTimeout);
                await FlushAsync(batch, shutdownFlush.Token);
            }

            _logger.LogInformation("ORM Trace Agent stopped");
        }

        private async Task FlushAsync(List<QueryTelemetry> batch, CancellationToken cancellationToken)
        {
            var retryDelay = TimeSpan.FromSeconds(1);

            while (batch.Count > 0 && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await _sender.SendAsync(batch, cancellationToken);
                    batch.Clear();
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "ORM Trace telemetry batch send failed; retrying in {RetryDelay}", retryDelay);

                    try
                    {
                        await Task.Delay(retryDelay, cancellationToken);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    retryDelay = TimeSpan.FromSeconds(Math.Min(retryDelay.TotalSeconds * 2, 30));
                }
            }
        }
    }
}