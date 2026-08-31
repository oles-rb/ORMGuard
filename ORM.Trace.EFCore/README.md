# ORM Trace Agent

The shared telemetry runtime behind the ORM Trace integrations for Entity Framework Core and DevExpress XPO.

ORM Trace Agent receives completed database-command telemetry from an ORM-specific integration, enriches it with application and trace context, buffers it without blocking the executing request, and sends compressed batches to [ORM Trace](https://ormtrace.dev) for analysis.

It is the common foundation for detecting:

- Slow database queries
- N+1 and repeated-query patterns
- Operations that execute too many database commands
- Inefficient SQL filtering patterns
- Performance regressions between application versions
- Which endpoint, operation, service, environment, version, and ORM produced a query

## Which package should I install?

Most applications should **not install this package directly**. Install the integration for the ORM used by the application:

### Entity Framework Core

```shell
dotnet add package ORM.Trace.EntityFrameworkCore
```

### DevExpress XPO

```shell
dotnet add package ORM.Trace.DevExpressXpo
```

Both integration packages bring `ORM.Trace.Agent` as a dependency. They can also be enabled together in the same application.

Install `ORM.Trace.Agent` directly only when:

- Building a custom ORM or ADO.NET integration
- Using the telemetry runtime independently of the provided integrations
- Referencing shared telemetry types such as `QueryTelemetry`

> The Agent alone does not intercept database commands. An ORM-specific or custom integration must submit queries to its telemetry pipeline.

## How it works

```text
EF Core interceptor / XPO logger / custom integration
                         │
                         ▼
              ORM Trace Agent queue
                         │
                  compressed batches
                         │
                         ▼
                  ormtrace.dev
                         │
                         ▼
          Problems, requests, queries and versions
```

The in-memory queue is bounded and query collection is non-blocking. Telemetry is sent in the background in batches. Temporary delivery failures are retried with exponential backoff.

## Supported application targets

| Target | Typical application |
| --- | --- |
| .NET 10 | ASP.NET Core, worker service, console, WinForms or WPF |
| .NET Framework 4.8 | WinForms, WPF, console or Windows service |

## Dependency-injection setup

ORM-specific packages use the same base registration:

```csharp
using ORM.Trace.DependencyInjection;

var ormTraceSection = builder.Configuration.GetSection("OrmTrace");

builder.Services.AddOrmTrace(options =>
{
    options.ApiKey = ormTraceSection["ApiKey"]!;
    options.ServiceName = "MyApplication";
    options.Environment = builder.Environment.EnvironmentName;
    options.Version = typeof(Program).Assembly.GetName().Version?.ToString();
    options.SlowQueryThresholdMs =
        ormTraceSection.GetValue<int?>("SlowQueryThresholdMs") ?? 300;

    if (Uri.TryCreate(ormTraceSection["Endpoint"], UriKind.Absolute, out var endpoint))
        options.Endpoint = endpoint;
});
```

Example configuration:

```json
{
  "OrmTrace": {
    "ApiKey": "YOUR_APPLICATION_API_KEY",    
    "SlowQueryThresholdMs": 300
  }
}
```

Then register one or both ORM integrations:

```csharp
builder.Services.AddOrmTraceEntityFrameworkCore();
builder.Services.AddOrmTraceDevExpressXpo();
```

The API key identifies the ORM Trace account/application receiving telemetry. Keep it in configuration, environment variables, or a secret store rather than source code.

## Applications without dependency injection

WinForms, WPF, console, and .NET Framework 4.8 applications can use a manually managed runtime:

```csharp
using ORM.Trace.Configuration;
using ORM.Trace.DependencyInjection;
using ORM.Trace.Runtime;

var ormTrace = OrmTraceRuntime.Create(new OrmTraceOptions
{
    ApiKey = Properties.Settings.Default.OrmTraceApiKey,    
    ServiceName = "MyDesktopApplication",
    Environment = "Production",
    Version = typeof(Program).Assembly.GetName().Version.ToString(),
    SlowQueryThresholdMs = 300
})
.UseDevExpressXpo() // Or .UseEntityFrameworkCore()
.Start();
```

Always dispose the runtime during application shutdown so pending telemetry can be flushed:

```csharp
try
{
    Application.Run(new MainForm());
}
finally
{
    ormTrace.Dispose();
}
```

## Traces and logical operations

In ASP.NET Core, the integrations use the current activity or HTTP request context to group queries into a trace.

Desktop and background applications should identify each meaningful operation explicitly:

```csharp
using ORM.Trace.Runtime;

using (OrmTraceTrace.Start("Open sales report"))
{
    LoadSalesReport();
    LoadReportTotals();
}
```

Every EF Core or XPO query executed inside this block receives the same trace identifier and operation name. This allows ORM Trace to calculate the total database cost of one user action and detect N+1 or excessive-query patterns.

Use one trace per user action, command, message, or background job. Do not keep one trace open for the entire application lifetime.

## Configuration reference

| Option | Default | Description |
| --- | --- | --- |
| `ApiKey` | required | Authenticates telemetry and identifies its ORM Trace application |
| `Endpoint` | `https://ormtrace.dev/` | ORM Trace service endpoint |
| `ServiceName` | `null` | Name of the monitored application or service |
| `Environment` | `Production` | Deployment environment, such as Development or Production |
| `Version` | `null` | Application version used for regression comparisons |
| `SlowQueryThresholdMs` | `300` | Duration at which a query is marked as slow |
| `BatchSize` | `100` | Maximum queries sent in one request |
| `FlushInterval` | 5 seconds | Maximum wait before a partial batch is sent |
| `SamplingRate` | `1.0` | Fraction of queries captured, from `0` to `1` |
| `CaptureParameterValues` | `false` | Includes parameter values when explicitly enabled |
| `QueueCapacity` | `10000` | Maximum telemetry items waiting in memory |
| `HttpTimeout` | 10 seconds | Timeout for a telemetry request |

## What data is sent?

Depending on the integration and available runtime context, query telemetry can include:

- SQL command text and its normalized fingerprint
- Execution duration and slow-query status
- Trace and span identifiers
- HTTP endpoint and method, or a named desktop operation
- ORM/database provider and database name
- Command type
- Service name, environment, and application version
- Parameter names and types
- Parameter values only when `CaptureParameterValues` is explicitly enabled

The Agent does not open its own database connection and does not read result rows or application entities. It observes commands already executed by an integration.

Parameter values are disabled by default. Generated SQL can still contain inline literal values depending on the ORM/provider, so review the SQL produced by your application before sending telemetry from a sensitive environment.

## Building a custom integration

A custom integration can submit `QueryTelemetry` instances through `IQueryCollector`. The Agent also exposes `SqlNormalizer` and `QueryFingerprintGenerator` to produce the same normalized SQL and fingerprints as the built-in integrations.

At minimum, custom telemetry should include:

```csharp
collector.TryAdd(new QueryTelemetry
{
    Timestamp = DateTimeOffset.UtcNow,
    Sql = sql,
    NormalizedSql = SqlNormalizer.Normalize(sql),
    Fingerprint = QueryFingerprintGenerator.Create(
        SqlNormalizer.Normalize(sql)),
    DurationMs = duration.TotalMilliseconds,
    DatabaseProvider = "MyOrm",
    IsSlow = duration.TotalMilliseconds >= options.SlowQueryThresholdMs,
    ServiceName = options.ServiceName,
    Environment = options.Environment,
    Version = options.Version
});
```

## Troubleshooting

- **401 Unauthorized:** the API key is invalid, revoked, disabled, or belongs to another ORM Trace account.
- **No queries appear:** installing Agent alone is not enough; enable an ORM-specific integration or custom collector.
- **Desktop queries have no useful trace:** wrap each logical operation in `OrmTraceTrace.Start(...)`.
- **Telemetry appears after a delay:** queries are sent in batches; review `BatchSize` and `FlushInterval`.
- **Telemetry is lost during desktop shutdown:** dispose `OrmTraceRuntime` before the process exits.

Manage applications, API keys, queries, traces, and detected issues at [ormtrace.dev](https://ormtrace.dev).
