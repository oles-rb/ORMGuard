namespace ORM.Trace.Configuration
{

    /// <summary>
    /// Configures telemetry collection and delivery for the ORM Trace agent.
    /// </summary>
    public sealed class OrmTraceOptions
    {
        /// <summary>
        /// Gets or sets the API key used to authenticate telemetry requests.
        /// </summary>
        public string ApiKey { get; set; } = null!;

        /// <summary>
        /// Gets or sets the ORM Trace service endpoint.
        /// </summary>
        public Uri Endpoint { get; set; } = new("https://ormtrace.dev/");

        /// <summary>
        /// Gets or sets the maximum number of queries sent in one telemetry batch.
        /// </summary>
        public int BatchSize { get; set; } = 100;

        /// <summary>
        /// Gets or sets the maximum interval between telemetry batch submissions.
        /// </summary>
        public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets or sets the query duration, in milliseconds, at which a query is marked as slow.
        /// </summary>
        public int SlowQueryThresholdMs { get; set; } = 300;

        /// <summary>
        /// Gets or sets the fraction of queries to capture, from <c>0</c> to <c>1</c>.
        /// </summary>
        public double SamplingRate { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets whether database parameter values are included in telemetry.
        /// </summary>
        public bool CaptureParameterValues { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of pending telemetry items held in memory.
        /// </summary>
        public int QueueCapacity { get; set; } = 10_000;

        /// <summary>Gets or sets the deployment environment reported with telemetry.
        /// </summary>
        public string Environment { get; set; } = "Production";

        /// <summary>
        /// Gets or sets the application or service name reported with telemetry.
        /// </summary>
        public string? ServiceName { get; set; }

        /// <summary>
        /// Gets or sets the application version reported with telemetry.
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Gets or sets the timeout for sending a telemetry batch.
        /// </summary>
        public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(10);
    }
}
