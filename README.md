# Serilog.Sinks.Resilient.OTel

This Serilog sink transforms Serilog events into OpenTelemetry
`LogRecord`s and sends them to an OTLP (gRPC or HTTP) endpoint.

The sink aims for full compliance with the OpenTelemetry Logs protocol. It
does not depend on the OpenTelemetry SDK or .NET API.

OpenTelemetry supports attributes with scalar values, arrays, and maps.
Serilog does as well. Consequently, the sink does a one-to-one
mapping between Serilog properties and OpenTelemetry attributes.
There is no flattening, renaming, or other modifications done to the
properties by default.

## Getting started

To use the OpenTelemetry sink, first install the
[NuGet package](https://nuget.org/packages/Serilog.Sinks.Resilient.OTel):

```shell
dotnet add package Serilog.Sinks.Resilient.OTel
```

Then enable the sink using `WriteTo.OpenTelemetry()`:

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.OpenTelemetry()
    .CreateLogger();
```

Generate logs using the `Log.Information(...)` and similar methods to
send transformed logs to a local OpenTelemetry OTLP endpoint.

A more complete configuration would specify `Endpoint`, `Protocol`,
and other parameters, such as`ResourceAttributes`, as shown in the
examples below.

## Configuration

This sink supports two configuration styles: inline and options.
Inline configuration is appropriate for simple, local logging
setups, and looks like:

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.OpenTelemetry(
        endpoint: "http://127.0.0.1:4318",
        protocol: OtlpProtocol.HttpProtobuf)
    .CreateLogger();
```

More complicated use cases need to use options-style
configuration, which looks like:

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.OpenTelemetry(options =>
    {
        options.Endpoint = "http://127.0.0.1:4318";
        options.Protocol = OtlpProtocol.HttpProtobuf;
    })
    .CreateLogger();
```

This supports the sink's full set of configuration options. See the
`OpenTelemetrySinkOptions.cs` file for the full set of options.
Some of the more important parameters are discussed in the following
sections.

### Endpoint and protocol

The default endpoint and protocol are `http://localhost:4317` and `OtlpProtocol.Grpc`.

In most production scenarios, you'll need to set an endpoint and protocol to suit your
deployment environment. To do so, add the `endpoint` argument to the `WriteTo.OpenTelemetry()` call.

You may also want to set the protocol. The supported values
are:

- `OtlpProtocol.Grpc`: Sends a protobuf representation of the
   OpenTelemetry Logs over a gRPC connection (the default).
- `OtlpProtocol.HttpProtobuf`: Sends a protobuf representation of the
   OpenTelemetry Logs over an HTTP connection.

### Resource attributes

OpenTelemetry logs may contain a "resource" that provides metadata concerning
the entity associated with the logs, typically a service or library. These
may contain "resource attributes" and are emitted for all logs flowing through
the configured logger.

These resource attributes may be provided as a `Dictionary<string, Object>`
when configuring a logger. OpenTelemetry allows resource attributes
with rich values; however, this implementation _only_ supports resource
attributes with primitive values.

> :warning: Resource attributes with non-primitive values will be
> silently ignored.

This example shows how the resource attributes can be specified when
the logger is configured.

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.OpenTelemetry(options =>
    {
        options.Endpoint = "http://127.0.0.1:4317";
        options.ResourceAttributes = new Dictionary<string, object>
        {
            ["service.name"] = "test-logging-service",
            ["index"] = 10,
            ["flag"] = true,
            ["value"] = 3.14
        };
    })
    .CreateLogger();
```

### Environment variable overrides

The sink also recognizes a selection of the `OTEL_OTLP_EXPORTER_*` environment variables described in
the [OpenTelemetry documentation](https://opentelemetry.io/docs/specs/otel/protocol/exporter/), and will
override programmatic configuration with any environment variable values present at runtime.

To switch off this behavior, pass `ignoreEnvironment: true` to the `WriteTo.OpenTelemetry()` configuration
methods.

## Serilog `LogEvent` to OpenTelemetry log record mapping

The following table provides the mapping between the Serilog log
events and the OpenTelemetry log records.

Serilog `LogEvent`               | OpenTelemetry `LogRecord`                  | Comments                                                                                      |
---------------------------------|--------------------------------------------|-----------------------------------------------------------------------------------------------|
`Exception.GetType().ToString()` | `Attributes["exception.type"]`             |                                                                                               |
`Exception.Message`              | `Attributes["exception.message"]`          | Ignored if empty                                                                              |
`Exception.StackTrace`           | `Attributes[ "exception.stacktrace"]`      | Value of `ex.ToString()`                                                                      |
`Level`                          | `SeverityNumber`                           | Serilog levels are mapped to corresponding OpenTelemetry severities                           |
`Level.ToString()`               | `SeverityText`                             |                                                                                               |
`Message`                        | `Body`                                     | Culture-specific formatting can be provided via sink configuration                            |
`MessageTemplate`                | `Attributes[ "message_template.text"]`     | Requires `IncludedData. MessageTemplateText` (enabled by default)                             |
`MessageTemplate` (MD5)          | `Attributes[ "message_template.hash.md5"]` | Requires `IncludedData. MessageTemplateMD5 HashAttribute`                                     |
`Properties`                     | `Attributes`                               | Each property is mapped to an attribute keeping the name; the value's structure is maintained |
`SpanId` (`Activity.Current`)    | `SpanId`                                   | Requires `IncludedData.SpanId` (enabled by default)                                           |
`Timestamp`                      | `TimeUnixNano`                             | .NET provides 100-nanosecond precision                                                        |
`TraceId` (`Activity.Current`)   | `TraceId`                                  | Requires `IncludedData.TraceId` (enabled by default)                                          |

### Configuring included data

This sink supports configuration of how common OpenTelemetry fields are populated from
the Serilog `LogEvent` and .NET `Activity` context via the `IncludedData` flags enum:

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.OpenTelemetry(options =>
    {
        options.Endpoint = "http://127.0.0.1:4317";
        options.IncludedData: IncludedData.MessageTemplate |
                              IncludedData.TraceId | IncludedData.SpanId;
    })
    .CreateLogger();
```

The example shows the default value; `IncludedData.MessageTemplateMD5HashAttribute` can
also be used to add the MD5 hash of the message template.

## Sending traces through the sink

Serilog `LogEvents` that carry a `SpanStartTimestamp` property of type `DateTime` will be
recognized as spans by this sink, and sent using the appropriate OpenTelemetry endpoint
and schema. The properties recognized by the sink match the ones emitted by
[SerilogTracing](https://github.com/serilog-tracing/serilog-tracing).

In addition to the field mapping performed for log records, events that represent trace
spans can carry the special properties listed below.

Serilog `LogEvent`               | OpenTelemetry `Span` | Comments                               |
---------------------------------|----------------------|----------------------------------------|
`MessageTemplate`                | `Name`               |                                        |
`Properties["ParentSpanId"]` | `ParentSpanId`       | Value must be of type `ActivitySpanId` |
`Properties["SpanKind"]` | `Kind`               | Value must be of type `ActivityKind`   |
`Properties["SpanStartTimestamp"]` | `StartTimeUnixNano`  | Value must be of type `DateTime`; .NET provides 100-nanosecond precision     |
`Timestamp`                | `EndTimeUnixNano`    | .NET provides 100-nanosecond precision |

## Suppressing other instrumentation

If the sink is used in an application that also instruments HTTP or gRPC requests using the OpenTelemetry libraries,
this can be suppressed for outbound requests made by the sink using `OnBeginSuppressInstrumentation`:

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.OpenTelemetry(options =>
    {
        options.OnBeginSuppressInstrumentation =
            OpenTelemetry.SuppressInstrumentationScope.Begin;
        // ...
```

## Fallbacks for resilience

If resilient logging is required, this sink provides a highly configurable file system fallback API that allows you to capture
the otlp requests in the original form they would be sent to your OTLP endpoint. This supports both the HTTP and gRPC protocols
and can be configured on a per-sink basis if you need different fallbacks for traces and logs, or a unified fallback if these
sinks can be unified.

Support is exposed for logging the OTLP messages as Newline delimited JSON or as delimited protobuf messages using the `LogFormat` switch.

Configuration is achievied using the fluent options configuration exposed with the `opts.FallbackWith(...)` api. The individual
fallback sinks can be configured using the `FallbackWith(Action<FallbackConfigurationOptions> config)` api for granular control,
or using the `FallbackWith(Action<FileSinkOptions> fileSink, LogFormat logFormat)` api for unified fallbacks.

### Example using one fallback

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.OpenTelemetry(options =>
    {
        options.FallbackWith(
            fs =>
            {
                fs.Path = "/var/logs/mylog.log";
            },
            LogFormat.Protobuf);
        // ...
```

### Example using separate fallbacks

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.OpenTelemetry(options =>
    {
        options.FallbackWith(fb =>
            fb.ToLogFile(fs => fs.Path = "/var/logs/mylog.log")
                .ToTraceFile(fs => fs.Path = "/var/logs/mytrace.log"));
        // ...
```

## Example

The `example/Example` subdirectory contains an example application that logs
to a local [OpenTelemetry collector](https://opentelemetry.io/docs/collector/) using a file fallback profile.
See the README in that directory for instructions on how to run the example.

_Copyright &copy; Serilog Contributors - Provided under the [Apache License, Version 2.0](http://apache.org/licenses/LICENSE-2.0.html)._
