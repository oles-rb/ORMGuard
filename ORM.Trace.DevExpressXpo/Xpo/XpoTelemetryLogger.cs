using System.Diagnostics;
using DevExpress.Xpo.Logger;
using Microsoft.AspNetCore.Http;
using ORM.Trace.Collection;
using ORM.Trace.Configuration;
using ORM.Trace.Processing;
using ORM.Trace.Telemetry;
using ORM.Trace.Runtime;

namespace ORM.Trace.Xpo
{
    internal sealed class XpoTelemetryLogger : ILogger
    {
        private readonly IQueryCollector collector;
        private readonly OrmTraceOptions options;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ILogger? next;

        public XpoTelemetryLogger(IQueryCollector collector, OrmTraceOptions options, IHttpContextAccessor httpContextAccessor, ILogger? next)
        {
            this.collector = collector;
            this.options = options;
            this.httpContextAccessor = httpContextAccessor;
            this.next = next;
        }

        public int Count => next?.Count ?? 0;
        public int LostMessageCount => next?.LostMessageCount ?? 0;
        public bool IsServerActive => true;
        public bool Enabled { get; set; } = true;
        public int Capacity => next?.Capacity ?? int.MaxValue;
        public void ClearLog() => next?.ClearLog();

        public void Log(LogMessage message)
        {
            next?.Log(message);
            if (!Enabled || message.MessageType != LogMessageType.DbCommand || !SamplingDecider.ShouldCapture(options.SamplingRate)) return;
            try
            {
                var sql = ExtractSql(message.MessageText);
                if (string.IsNullOrWhiteSpace(sql)) return;
                var normalizedSql = SqlNormalizer.Normalize(sql!);
                var durationMs = Math.Max(0, message.Duration.TotalMilliseconds);
                var activity = Activity.Current;
                var httpContext = httpContextAccessor.HttpContext;
                var traceId = activity?.TraceId.ToString();
                if (string.IsNullOrWhiteSpace(traceId)) traceId = httpContext?.TraceIdentifier;
                collector.TryAdd(new QueryTelemetry
                {
                    Timestamp = message.Date == default ? DateTimeOffset.UtcNow : new DateTimeOffset(message.Date.ToUniversalTime()),
                    Sql = sql!,
                    NormalizedSql = normalizedSql,
                    Fingerprint = QueryFingerprintGenerator.Create(normalizedSql),
                    DurationMs = durationMs,
                    TraceId = traceId,
                    SpanId = activity?.SpanId.ToString(),
                    Endpoint = httpContext?.Request.Path.Value ?? activity?.GetTagItem(OrmTraceTrace.OperationTag)?.ToString(),
                    HttpMethod = httpContext?.Request.Method,
                    DatabaseProvider = "DevExpress.Xpo",
                    CommandType = "XPO DbCommand",
                    IsSlow = durationMs >= options.SlowQueryThresholdMs,
                    ServiceName = options.ServiceName,
                    Environment = options.Environment,
                    Version = options.Version,
                    Parameters = MapParameters(message.Parameters)
                });
            }
            catch { }
        }

        public void Log(LogMessage[] messages)
        {
            foreach (var message in messages)
                Log(message);
        }

        internal static string? ExtractSql(string? messageText)
        {
            var text = messageText?.Trim();
            if (string.IsNullOrWhiteSpace(text)) return null;
            const string prefix = "Executing sql '";
            if (!text!.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return text;
            var sql = text!.Substring(prefix.Length);
            if (sql.EndsWith("'", StringComparison.Ordinal)) sql = sql.Substring(0, sql.Length - 1);
            return sql.Trim();
        }

        private IReadOnlyList<QueryParameterInfo>? MapParameters(LogMessageParameter[]? parameters) =>
            parameters is null || parameters.Length == 0 ? null : parameters.Select(parameter => new QueryParameterInfo
            {
                Name = parameter.Name ?? "parameter",
                DbType = parameter.Value?.GetType().Name ?? "Unknown",
                Value = options.CaptureParameterValues ? parameter.Value?.ToString() : null
            }).ToArray();
    }
}
