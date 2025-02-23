﻿// Copyright © Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

// ReSharper disable once RedundantUsingDirective
using System.Net.Http;
using Serilog.Collections;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.Resilient.OTel;
using Serilog.Sinks.Resilient.OTel.Configuration;
using Serilog.Sinks.Resilient.OTel.Exporters;
using Serilog.Sinks.Resilient.OTel.FileFallback;
// ReSharper disable MemberCanBePrivate.Global

namespace Serilog;

/// <summary>
/// Adds OpenTelemetry sink configuration methods to <see cref="LoggerSinkConfiguration"/>.
/// </summary>
public static class OpenTelemetryLoggerConfigurationExtensions
{
    // ReSharper disable once ReturnTypeCanBeNotNullable
    static HttpMessageHandler? CreateDefaultHttpMessageHandler() =>
#if FEATURE_SOCKETS_HTTP_HANDLER
        new SocketsHttpHandler { ActivityHeadersPropagator = null };
#else
        null;
#endif

    /// <summary>
    /// Send log events to an OTLP exporter.
    /// </summary>
    /// <param name="loggerSinkConfiguration">
    /// The <c>WriteTo</c> configuration object.
    /// </param>
    /// <param name="configure">The configuration callback.</param>
    /// <param name="ignoreEnvironment">If false the configuration will be overridden with values
    /// from the <see href="https://opentelemetry.io/docs/languages/sdk-configuration/otlp-exporter/">OTLP Exporter
    /// Configuration environment variables</see>, if present.</param>
    public static LoggerConfiguration OpenTelemetry(
        this LoggerSinkConfiguration loggerSinkConfiguration,
        Action<BatchedOpenTelemetrySinkOptions> configure,
        bool ignoreEnvironment = false)
    {
        return loggerSinkConfiguration.OpenTelemetry(
            configure,
            ignoreEnvironment ? null : Environment.GetEnvironmentVariable
        );
    }

    /// <summary>
    /// Send log events to an OTLP exporter.
    /// </summary>
    /// <param name="loggerSinkConfiguration">
    /// The `WriteTo` configuration object.
    /// </param>
    /// <param name="configure">The configuration callback.</param>
    /// <param name="getConfigurationVariable">Provides <see href="https://opentelemetry.io/docs/languages/sdk-configuration/otlp-exporter/">OTLP Exporter
    /// Configuration variables</see> that will override other options when present.</param>
    public static LoggerConfiguration OpenTelemetry(
        this LoggerSinkConfiguration loggerSinkConfiguration,
        Action<BatchedOpenTelemetrySinkOptions> configure,
        Func<string, string?>? getConfigurationVariable)
    {
        if (loggerSinkConfiguration == null) throw new ArgumentNullException(nameof(loggerSinkConfiguration));
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        var options = new BatchedOpenTelemetrySinkOptions();
        configure(options);

        if (getConfigurationVariable != null)
        {
            OpenTelemetryEnvironment.Configure(options, getConfigurationVariable);
        }

        var exporter = Exporter.Create(
            logsEndpoint: options.LogsEndpoint,
            tracesEndpoint: options.TracesEndpoint,
            protocol: options.Protocol,
            headers: new Dictionary<string, string>(options.Headers),
            httpMessageHandler: options.HttpMessageHandler ?? CreateDefaultHttpMessageHandler(),
            onBeginSuppressInstrumentation: options.OnBeginSuppressInstrumentation != null ?
                () => options.OnBeginSuppressInstrumentation(true)
                : null);

        ILogEventSink? logsSink = null, tracesSink = null;
        var logsFallback = new ConcreteFileFallback(options.Fallback.LogFallback);
        var tracesFallback = new ConcreteFileFallback(options.Fallback.TraceFallback);

        if (options.LogsEndpoint != null)
        {
            var openTelemetryLogsSink = new OpenTelemetryLogsSink(
                exporter: exporter,
                formatProvider: options.FormatProvider,
                resourceAttributes: new Dictionary<string, object>(options.ResourceAttributes),
                includedData: options.IncludedData,
                fallback: logsFallback);

            logsSink = LoggerSinkConfiguration.CreateSink(wt => wt.Sink(openTelemetryLogsSink, options.BatchingOptions));
        }

        if (options.TracesEndpoint != null)
        {
            var openTelemetryTracesSink = new OpenTelemetryTracesSink(
                exporter: exporter,
                resourceAttributes: new Dictionary<string, object>(options.ResourceAttributes),
                includedData: options.IncludedData,
                fallback: tracesFallback);

            tracesSink = LoggerSinkConfiguration.CreateSink(wt => wt.Sink(openTelemetryTracesSink, options.BatchingOptions));
        }

        var sink = new OpenTelemetrySink(exporter, logsSink, tracesSink);

        return loggerSinkConfiguration.Sink(sink, options.RestrictedToMinimumLevel, options.LevelSwitch);
    }

    /// <summary>
    /// Send log events to an OTLP exporter.
    /// </summary>
    /// <param name="loggerSinkConfiguration">
    /// The <c>WriteTo</c> configuration object.
    /// </param>
    /// <param name="endpoint">
    /// The full URL of the OTLP exporter endpoint.
    /// </param>
    /// <param name="protocol">
    /// The OTLP protocol to use.
    /// </param>
    /// <param name="headers">
    /// Headers to send with network requests.
    /// </param>
    /// <param name="resourceAttributes">
    /// A attributes of the resource attached to the logs generated by the sink. The values must be simple primitive
    /// values: integers, doubles, strings, or booleans. Other values will be silently ignored.
    /// </param>
    /// <param name="includedData">
    /// Which fields should be included in the log events generated by the sink.
    /// </param>
    /// <param name="restrictedToMinimumLevel">
    /// The minimum level for events passed through the sink. Ignored when <paramref name="levelSwitch"/> is specified.
    /// </param>
    /// <param name="levelSwitch">
    /// A switch allowing the pass-through minimum level to be changed at runtime.
    /// </param>
    /// <param name="fallbackOptions">
    /// Configuration for a file system fallback mechanism that can log the OTLP request to file.
    /// </param>
    /// <returns>Logger configuration, allowing configuration to continue.</returns>
    public static LoggerConfiguration OpenTelemetry(
        this LoggerSinkConfiguration loggerSinkConfiguration,
        string endpoint = OpenTelemetrySinkOptions.DefaultEndpoint,
        OtlpProtocol protocol = OpenTelemetrySinkOptions.DefaultProtocol,
        IDictionary<string, string>? headers = null,
        IDictionary<string, object>? resourceAttributes = null,
        IncludedData? includedData = null,
        LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
        LoggingLevelSwitch? levelSwitch = null,
        FallbackConfigurationOptions? fallbackOptions = null)
    {
        if (loggerSinkConfiguration == null) throw new ArgumentNullException(nameof(loggerSinkConfiguration));

        return loggerSinkConfiguration.OpenTelemetry(options =>
        {
            options.Endpoint = endpoint;
            options.Protocol = protocol;
            options.IncludedData = includedData ?? options.IncludedData;
            options.RestrictedToMinimumLevel = restrictedToMinimumLevel;
            options.LevelSwitch = levelSwitch;
            if(fallbackOptions is not null)
            {
                options.Fallback = fallbackOptions;
            }
            headers?.AddTo(options.Headers);
            resourceAttributes?.AddTo(options.ResourceAttributes);
        });
    }

    /// <summary>
    /// Audit to an OTLP exporter, waiting for each event to be acknowledged, and propagating errors to the caller.
    /// </summary>
    /// <param name="loggerAuditSinkConfiguration">
    /// The <c>AuditTo</c> configuration object.
    /// </param>
    /// <param name="configure">The configuration callback.</param>
    public static LoggerConfiguration OpenTelemetry(
        this LoggerAuditSinkConfiguration loggerAuditSinkConfiguration,
        Action<OpenTelemetrySinkOptions> configure)
    {
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        var options = new OpenTelemetrySinkOptions();
        configure(options);

        var exporter = Exporter.Create(
            logsEndpoint: options.LogsEndpoint,
            tracesEndpoint: options.TracesEndpoint,
            protocol: options.Protocol,
            headers: new Dictionary<string, string>(options.Headers),
            httpMessageHandler: options.HttpMessageHandler ?? CreateDefaultHttpMessageHandler(),
            onBeginSuppressInstrumentation: options.OnBeginSuppressInstrumentation != null ?
                () => options.OnBeginSuppressInstrumentation(true)
                : null);

        ILogEventSink? logsSink = null, tracesSink = null;
        var logsFallback = new ConcreteFileFallback(options.Fallback.LogFallback);
        var tracesFallback = new ConcreteFileFallback(options.Fallback.TraceFallback);

        if (options.LogsEndpoint != null)
        {
            logsSink = new OpenTelemetryLogsSink(
                exporter: exporter,
                formatProvider: options.FormatProvider,
                resourceAttributes: new Dictionary<string, object>(options.ResourceAttributes),
                includedData: options.IncludedData,
                fallback: logsFallback);
        }

        if (options.TracesEndpoint != null)
        {
            tracesSink = new OpenTelemetryTracesSink(
                exporter: exporter,
                resourceAttributes: new Dictionary<string, object>(options.ResourceAttributes),
                includedData: options.IncludedData,
                fallback: tracesFallback);
        }

        var sink = new OpenTelemetrySink(exporter, logsSink, tracesSink);

        return loggerAuditSinkConfiguration.Sink(sink, options.RestrictedToMinimumLevel, options.LevelSwitch);
    }

    /// <summary>
    /// Audit to an OTLP exporter, waiting for each event to be acknowledged, and propagating errors to the caller.
    /// </summary>
    /// <param name="loggerAuditSinkConfiguration">
    /// The `AuditTo` configuration object.
    /// </param>
    /// <param name="endpoint">
    /// The full URL of the OTLP exporter endpoint.
    /// </param>
    /// <param name="protocol">
    /// The OTLP protocol to use.
    /// </param>
    /// <param name="headers">
    /// Headers to send with network requests.
    /// </param>
    /// <param name="resourceAttributes">
    /// A attributes of the resource attached to the logs generated by the sink. The values must be simple primitive
    /// values: integers, doubles, strings, or booleans. Other values will be silently ignored.
    /// </param>
    /// <param name="includedData">
    /// Which fields should be included in the log events generated by the sink.
    /// </param>
    /// <param name="fallbackOptions">
    /// Configuration for a file system fallback mechanism that can log the OTLP request to file.
    /// </param>
    /// <returns>Logger configuration, allowing configuration to continue.</returns>
    public static LoggerConfiguration OpenTelemetry(
        this LoggerAuditSinkConfiguration loggerAuditSinkConfiguration,
        string endpoint = OpenTelemetrySinkOptions.DefaultEndpoint,
        OtlpProtocol protocol = OpenTelemetrySinkOptions.DefaultProtocol,
        IDictionary<string, string>? headers = null,
        IDictionary<string, object>? resourceAttributes = null,
        IncludedData? includedData = null,
        FallbackConfigurationOptions? fallbackOptions = null)
    {
        if (loggerAuditSinkConfiguration == null) throw new ArgumentNullException(nameof(loggerAuditSinkConfiguration));

        return loggerAuditSinkConfiguration.OpenTelemetry(options =>
        {
            options.Endpoint = endpoint;
            options.Protocol = protocol;
            options.IncludedData = includedData ?? options.IncludedData;
            if (fallbackOptions is not null)
            {
                options.Fallback = fallbackOptions;
            }
            headers?.AddTo(options.Headers);
            resourceAttributes?.AddTo(options.ResourceAttributes);
        });
    }
}
