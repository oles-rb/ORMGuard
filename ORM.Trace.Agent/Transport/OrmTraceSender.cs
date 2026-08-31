using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ORM.Trace.Configuration;
using ORM.Trace.Telemetry;

namespace ORM.Trace.Transport
{

    /// <summary>
    /// Represents the wire payload for a batch of query telemetry.
    /// </summary>
    public sealed class QueryBatchPayload
    {
        /// <summary>
        /// Gets the reporting application or service name.
        /// </summary>
        [JsonPropertyName("service")]
        public string? Service { get; init; }

        /// <summary>
        /// Gets the reporting deployment environment.
        /// </summary>
        [JsonPropertyName("environment")]
        public string? Environment { get; init; }

        /// <summary>
        /// Gets the reporting application version.
        /// </summary>
        [JsonPropertyName("version")]
        public string? Version { get; init; }

        /// <summary>
        /// Gets the queries included in the batch.
        /// </summary>
        [JsonPropertyName("queries")]
        public required IReadOnlyList<QueryTelemetryDto> Queries { get; init; }
    }

    /// <summary>
    /// Represents telemetry for a database query, including the original and normalized SQL, execution duration,
    /// fingerprint, and associated contextual metadata such as trace/span identifiers, endpoint, service, environment, and
    /// parameters. 
    /// </summary>
    /// <remarks>Timestamp preserves the original offset; normalize to UTC for comparisons or ordering (for example,
    /// via ToUniversalTime()). NormalizedSql provides a stable, comparable representation (consistent whitespace, casing,
    /// and parameter placeholders) suitable for grouping, caching, or fingerprinting.</remarks>
    public sealed class QueryTelemetryDto
    {
        /// <summary>
        /// The date and time of the recorded event or measurement, including the offset from UTC.  
        /// </summary>
        /// <remarks>Preserves the original offset; normalize to UTC for comparisons or ordering (for example, via
        /// ToUniversalTime()).</remarks>
        [JsonPropertyName("timestamp")]
        public required DateTimeOffset Timestamp { get; init; }

        /// <summary>
        /// SQL statement or query to execute.  
        /// </summary>
        /// <remarks>Required; must be a non-empty, valid provider-specific SQL statement. Use parameterization to
        /// avoid SQL injection.</remarks>
        [JsonPropertyName("sql")]
        public required string Sql { get; init; }

        /// <summary>
        /// Normalized SQL statement with consistent whitespace, casing, and parameter placeholders for deterministic
        /// comparison, grouping, or caching.
        /// </summary>
        /// <remarks>Normalization may include trimming, collapsing whitespace, standardizing keyword/identifier
        /// casing, and replacing literal values with parameter placeholders to produce a stable, comparable
        /// representation.</remarks>
        [JsonPropertyName("normalizedSql")]
        public required string NormalizedSql { get; init; }

        /// <summary>
        /// Gets the stable fingerprint of the normalized SQL.
        /// </summary>
        [JsonPropertyName("fingerprint")]
        public required string Fingerprint { get; init; }

        /// <summary>
        /// Gets the execution duration in milliseconds.
        /// </summary>
        [JsonPropertyName("durationMs")]
        public required double DurationMs { get; init; }

        /// <summary>
        /// Gets the logical operation trace identifier.
        /// </summary>
        [JsonPropertyName("traceId")]
        public string? TraceId { get; init; }

        /// <summary>
        /// Gets the activity span identifier.
        /// </summary>
        [JsonPropertyName("spanId")]
        public string? SpanId { get; init; }

        /// <summary>
        /// Gets the HTTP endpoint or named desktop operation.
        /// </summary>
        [JsonPropertyName("endpoint")]
        public string? Endpoint { get; init; }

        /// <summary>
        /// Gets the HTTP method, when applicable.
        /// </summary>
        [JsonPropertyName("httpMethod")]
        public string? HttpMethod { get; init; }

        /// <summary>
        /// Gets the ORM or database provider.
        /// </summary>
        [JsonPropertyName("databaseProvider")]
        public string? DatabaseProvider { get; init; }

        /// <summary>
        /// Gets the target database name.
        /// </summary>
        [JsonPropertyName("databaseName")]
        public string? DatabaseName { get; init; }

        /// <summary>
        /// Gets the database command type.
        /// </summary>
        [JsonPropertyName("commandType")]
        public string? CommandType { get; init; }

        /// <summary>
        /// Gets whether the query reached the configured slow-query threshold.
        /// </summary>
        [JsonPropertyName("isSlow")]
        public bool IsSlow { get; init; }

        /// <summary>
        /// Gets the reporting application or service name.
        /// </summary>
        [JsonPropertyName("serviceName")]
        public string? ServiceName { get; init; }

        /// <summary>
        /// Gets the reporting deployment environment.
        /// </summary>
        [JsonPropertyName("environment")]
        public string? Environment { get; init; }

        /// <summary>
        /// Gets the reporting application version.
        /// </summary>
        [JsonPropertyName("version")]
        public string? Version { get; init; }

        /// <summary>
        /// Gets the captured database parameters.
        /// </summary>
        [JsonPropertyName("parameters")]
        public IReadOnlyList<QueryParameterDto>? Parameters { get; init; }
    }

    /// <summary>
    /// Represents a database parameter in the telemetry wire format.
    /// </summary>
    public sealed class QueryParameterDto
    {
        /// <summary>
        /// Gets the provider-specific parameter name.
        /// </summary>
        [JsonPropertyName("name")]
        public required string Name { get; init; }

        /// <summary>
        /// Gets the database parameter type.
        /// </summary>
        [JsonPropertyName("dbType")]
        public required string DbType { get; init; }

        /// <summary>
        /// Gets the captured value, when parameter value capture is enabled.
        /// </summary>
        [JsonPropertyName("value")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Value { get; init; }
    }

    /// <summary>
    /// Sends compressed telemetry batches to the configured ORM Trace endpoint.
    /// </summary>
    public sealed class OrmTraceSender : IOrmTraceSender
    {
        private readonly HttpClient _httpClient;
        private readonly OrmTraceOptions _options;
        private readonly ILogger<OrmTraceSender> _logger;

        /// <summary>
        /// Initializes a telemetry sender.
        /// </summary>
        /// <param name="httpClient">The HTTP client used to submit telemetry.</param>
        /// <param name="options">The agent configuration.</param>
        /// <param name="logger">The sender logger.</param>
        public OrmTraceSender(HttpClient httpClient, IOptions<OrmTraceOptions> options, ILogger<OrmTraceSender> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;

            _httpClient.BaseAddress = _options.Endpoint;
            _httpClient.Timeout = _options.HttpTimeout;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }

        /// <inheritdoc />
        public async Task SendAsync(IReadOnlyList<QueryTelemetry> batch, CancellationToken cancellationToken)
        {
            if (batch.Count == 0)
                return;

            var payload = new QueryBatchPayload
            {
                Service = _options.ServiceName,
                Environment = _options.Environment,
                Version = _options.Version,
                Queries = batch.Select(MapToDto).ToArray()
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "v1/telemetry/query-batch");

            request.Content = new GzipJsonContent(payload);

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("ORM Trace telemetry batch failed with status {StatusCode}", (int)response.StatusCode);
                response.EnsureSuccessStatusCode();
            }
        }

        private static QueryTelemetryDto MapToDto(QueryTelemetry telemetry) =>
            new()
            {
                Timestamp = telemetry.Timestamp,
                Sql = telemetry.Sql,
                NormalizedSql = telemetry.NormalizedSql,
                Fingerprint = telemetry.Fingerprint,
                DurationMs = telemetry.DurationMs,
                TraceId = telemetry.TraceId,
                SpanId = telemetry.SpanId,
                Endpoint = telemetry.Endpoint,
                HttpMethod = telemetry.HttpMethod,
                DatabaseProvider = telemetry.DatabaseProvider,
                DatabaseName = telemetry.DatabaseName,
                CommandType = telemetry.CommandType,
                IsSlow = telemetry.IsSlow,
                ServiceName = telemetry.ServiceName,
                Environment = telemetry.Environment,
                Version = telemetry.Version,
                Parameters = telemetry.Parameters?
                    .Select(p => new QueryParameterDto
                    {
                        Name = p.Name,
                        DbType = p.DbType,
                        Value = p.Value
                    })
                    .ToArray()
            };

        private sealed class GzipJsonContent : HttpContent
        {
            private readonly QueryBatchPayload _payload;

            public GzipJsonContent(QueryBatchPayload payload)
            {
                _payload = payload;
                Headers.ContentType = new MediaTypeHeaderValue("application/json");
                Headers.ContentEncoding.Add("gzip");
            }

            protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            {
                using var gzipStream = new GZipStream(stream, CompressionLevel.Fastest, leaveOpen: true);
                await JsonContent.Create(_payload).CopyToAsync(gzipStream);
            }

            protected override bool TryComputeLength(out long length)
            {
                length = -1;
                return false;
            }
        }
    }
}