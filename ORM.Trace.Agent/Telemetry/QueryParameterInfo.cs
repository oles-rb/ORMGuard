namespace ORM.Trace.Telemetry
{

    /// <summary>
    /// Describes a database command parameter captured with query telemetry.
    /// </summary>
    public sealed record QueryParameterInfo
    {
        /// <summary>
        /// Gets the provider-specific parameter name.
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// Gets the database parameter type.
        /// </summary>
        public required string DbType { get; init; }

        /// <summary>
        /// Gets the captured value, or <see langword="null"/> when value capture is disabled.
        /// </summary>
        public string? Value { get; init; }
    }
}
