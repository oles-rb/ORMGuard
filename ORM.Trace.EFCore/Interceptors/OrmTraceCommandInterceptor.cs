using System.Data.Common;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using ORM.Trace.Collection;
using ORM.Trace.Configuration;
using ORM.Trace.Processing;
using ORM.Trace.Telemetry;
using ORM.Trace.Runtime;

namespace ORM.Trace.Interceptors
{
    /// <summary>
    /// Captures completed EF Core database commands and forwards them to ORM Trace.
    /// </summary>
    public sealed class OrmTraceCommandInterceptor : DbCommandInterceptor
    {
        private readonly IQueryCollector collector;
        private readonly OrmTraceOptions options;
        private readonly IHttpContextAccessor httpContextAccessor;

        /// <summary>
        /// Initializes the EF Core command interceptor.
        /// </summary>
        /// <param name="collector">The shared query telemetry collector.</param>
        /// <param name="options">The agent configuration.</param>
        /// <param name="httpContextAccessor">Provides request context when the query runs during an HTTP request.</param>
        public OrmTraceCommandInterceptor(IQueryCollector collector, IOptions<OrmTraceOptions> options, IHttpContextAccessor httpContextAccessor)
        {
            this.collector = collector;
            this.options = options.Value;
            this.httpContextAccessor = httpContextAccessor;
        }

        /// <inheritdoc />
        public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
        {
            Capture(command, eventData);
            return base.ReaderExecuted(command, eventData, result);
        }
#if NET48
        /// <inheritdoc />
        public override Task<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
        {
            Capture(command, eventData);
            return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
        }
#else
    /// <inheritdoc />
        public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default) 
        {
            Capture(command, eventData); 
            return base.ReaderExecutedAsync(command, eventData, result, cancellationToken); 
        }
#endif
        /// <inheritdoc />
        public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
        {
            Capture(command, eventData);
            return base.ScalarExecuted(command, eventData, result);
        }
#if NET48
        /// <inheritdoc />
        public override Task<object> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object result, CancellationToken cancellationToken = default)
        { Capture(command, eventData); return base.ScalarExecutedAsync(command, eventData, result, cancellationToken); }
#else
        /// <inheritdoc />
        public override ValueTask<object?> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken cancellationToken = default)
        {
            Capture(command, eventData);
            return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
        }
#endif
        /// <inheritdoc />
        public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
        {
            Capture(command, eventData);
            return base.NonQueryExecuted(command, eventData, result);
        }
#if NET48
        /// <inheritdoc />
        public override Task<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            Capture(command, eventData);
            return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
        }
#else
        /// <inheritdoc />
        public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            Capture(command, eventData);
            return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
        }
#endif


        private void Capture(DbCommand command, CommandExecutedEventData eventData)
        {
            if (!SamplingDecider.ShouldCapture(options.SamplingRate)) return;
            try
            {
                var activity = Activity.Current;
                var httpContext = httpContextAccessor.HttpContext;
                var sql = command.CommandText;
                var normalizedSql = SqlNormalizer.Normalize(sql);
                var durationMs = eventData.Duration.TotalMilliseconds;
                var traceId = activity?.TraceId.ToString();
                if (string.IsNullOrWhiteSpace(traceId)) traceId = httpContext?.TraceIdentifier;
                collector.TryAdd(new QueryTelemetry
                {
                    Timestamp = DateTimeOffset.UtcNow,
                    Sql = sql,
                    NormalizedSql = normalizedSql,
                    Fingerprint = QueryFingerprintGenerator.Create(normalizedSql),
                    DurationMs = durationMs,
                    TraceId = traceId,
                    SpanId = activity?.SpanId.ToString(),
                    Endpoint = httpContext?.Request.Path.Value ?? activity?.GetTagItem(OrmTraceTrace.OperationTag)?.ToString(),
                    HttpMethod = httpContext?.Request.Method,
                    DatabaseProvider = eventData.Context?.Database.ProviderName,
                    DatabaseName = command.Connection?.Database,
                    CommandType = command.CommandType.ToString(),
                    IsSlow = durationMs >= options.SlowQueryThresholdMs,
                    ServiceName = options.ServiceName,
                    Environment = options.Environment,
                    Version = options.Version,
                    Parameters = ReadParameters(command)
                });
            }
            catch { }
        }

        private IReadOnlyList<QueryParameterInfo>? ReadParameters(DbCommand command)
        {
            if (command.Parameters.Count == 0) return null;
            return command.Parameters.Cast<DbParameter>().Select(parameter => new QueryParameterInfo
            {
                Name = parameter.ParameterName,
                DbType = parameter.DbType.ToString(),
                Value = options.CaptureParameterValues ? parameter.Value?.ToString() : null
            }).ToArray();
        }
    }
}
