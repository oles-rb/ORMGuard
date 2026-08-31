namespace ORM.Trace.Telemetry
{

    /// <summary>
    /// Represents one captured database command and its execution context.
    /// </summary>
    public sealed record QueryTelemetry
    {
        /// <summary>
        /// Gets the time at which the query was captured.
        /// </summary>
        public required DateTimeOffset Timestamp { get; init; }

        /// <summary>
        /// Gets the original SQL statement.
        /// </summary>
        public required string Sql { get; init; }

        /// <summary>
        /// Gets the normalized SQL used for grouping equivalent statements.
        /// </summary>
        public required string NormalizedSql { get; init; }

        /// <summary>
        /// Gets the stable fingerprint of the normalized SQL.
        /// </summary>
        public required string Fingerprint { get; init; }

        /// <summary>
        /// Gets the query execution duration in milliseconds.
        /// </summary>
        public required double DurationMs { get; init; }

        /// <summary>
        /// Gets the identifier of the logical operation containing the query.
        /// </summary>
        public string? TraceId { get; init; }

        /// <summary>
        /// Gets the identifier of the activity span that captured the query.
        /// </summary>
        public string? SpanId { get; init; }

        /// <summary>
        /// Gets the HTTP endpoint or named desktop operation associated with the query.
        /// </summary>
        public string? Endpoint { get; init; }

        /// <summary>
        /// Gets the HTTP method associated with the query, when applicable.
        /// </summary>
        public string? HttpMethod { get; init; }

        /// <summary>
        /// Gets the ORM or database provider that executed the query.
        /// </summary>
        public string? DatabaseProvider { get; init; }

        /// <summary>
        /// Gets the target database name.
        /// </summary>
        public string? DatabaseName { get; init; }

        /// <summary>Gets the database command type.
        /// </summary>
        public string? CommandType { get; init; }

        /// <summary>
        /// Gets whether the duration reached the configured slow-query threshold.
        /// </summary>
        public bool IsSlow { get; init; }

        /// <summary>
        /// Gets the application or service name.
        /// </summary>
        public string? ServiceName { get; init; }

        /// <summary>Gets the deployment environment.
        /// </summary>
        public string? Environment { get; init; }

        /// <summary>
        /// Gets the application version.
        /// </summary>
        public string? Version { get; init; }

        /// <summary>
        /// Gets the captured database parameters.
        /// </summary>
        public IReadOnlyList<QueryParameterInfo>? Parameters { get; init; }
    }
}
