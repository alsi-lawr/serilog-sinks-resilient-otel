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

using Serilog.Configuration;
using Serilog.Sinks.OpenTelemetry;
using Serilog.Sinks.OpenTelemetry.Exporters;
using Serilog.Sinks.PeriodicBatching;
using Serilog.Collections;

namespace Serilog;

/// <summary>
/// Adds OpenTelemetry sink configuration methods to <see cref="LoggerSinkConfiguration"/>.
/// </summary>
public static class OpenTelemetryLoggerConfigurationExtensions
{
    /// <summary>
    /// Send log events to an OTLP exporter.
    /// </summary>
    /// <param name="loggerSinkConfiguration">
    /// The `WriteTo` configuration object.
    /// </param>
    /// <param name="configure">The configuration callback.</param>
    public static LoggerConfiguration OpenTelemetry(
        this LoggerSinkConfiguration loggerSinkConfiguration,
        Action<BatchedOpenTelemetrySinkOptions> configure)
    {
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        var options = new BatchedOpenTelemetrySinkOptions();
        configure(options);

        var exporter = Exporter.Create(
            endpoint: options.Endpoint,
            protocol: options.Protocol,
            headers: new Dictionary<string, string>(options.Headers),
            httpMessageHandler: options.HttpMessageHandler);

        var openTelemetrySink = new OpenTelemetrySink(
            exporter: exporter,
            formatProvider: options.FormatProvider,
            resourceAttributes: new Dictionary<string, object>(options.ResourceAttributes),
            includedData: options.IncludedData);

        var sink = new PeriodicBatchingSink(openTelemetrySink, options.BatchingOptions);

        return loggerSinkConfiguration.Sink(sink, options.RestrictedToMinimumLevel, options.LevelSwitch);
    }

    /// <summary>
    /// Send log events to an OTLP exporter.
    /// </summary>
    /// <param name="loggerSinkConfiguration">
    /// The `WriteTo` configuration object.
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
    /// <returns>Logger configuration, allowing configuration to continue.</returns>
    public static LoggerConfiguration OpenTelemetry(
        this LoggerSinkConfiguration loggerSinkConfiguration,
        string endpoint = OpenTelemetrySinkOptions.DefaultEndpoint,
        OtlpProtocol protocol = OpenTelemetrySinkOptions.DefaultProtocol,
        IDictionary<string, string>? headers = null,
        IDictionary<string, object>? resourceAttributes = null,
        string includedData = null)
    {
        if (loggerSinkConfiguration == null) throw new ArgumentNullException(nameof(loggerSinkConfiguration));

        return loggerSinkConfiguration.OpenTelemetry(options =>
        {
            options.Endpoint = endpoint;
            options.Protocol = protocol;
            options.IncludedData = (IncludedData)Enum.Parse(typeof(IncludedData), includedData);
            headers?.AddTo(options.Headers);
            resourceAttributes?.AddTo(options.ResourceAttributes);
        });
    }
    
    /// <summary>
    /// Audit to an OTLP exporter, waiting for each event to be acknowledged, and propagating errors to the caller.
    /// </summary>
    /// <param name="loggerAuditSinkConfiguration">
    /// The `AuditTo` configuration object.
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
            endpoint: options.Endpoint,
            protocol: options.Protocol,
            headers: new Dictionary<string, string>(options.Headers),
            httpMessageHandler: options.HttpMessageHandler);

        var sink = new OpenTelemetrySink(
            exporter: exporter,
            formatProvider: options.FormatProvider,
            resourceAttributes: new Dictionary<string, object>(options.ResourceAttributes),
            includedData: options.IncludedData);

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
    /// <returns>Logger configuration, allowing configuration to continue.</returns>
    public static LoggerConfiguration OpenTelemetry(
        this LoggerAuditSinkConfiguration loggerAuditSinkConfiguration,
        string endpoint = OpenTelemetrySinkOptions.DefaultEndpoint,
        OtlpProtocol protocol = OpenTelemetrySinkOptions.DefaultProtocol,
        IDictionary<string, string>? headers = null,
        IDictionary<string, object>? resourceAttributes = null,
        string includedData = null)
    {
        if (loggerAuditSinkConfiguration == null) throw new ArgumentNullException(nameof(loggerAuditSinkConfiguration));

        return loggerAuditSinkConfiguration.OpenTelemetry(options =>
        {
            options.Endpoint = endpoint;
            options.Protocol = protocol;
            options.IncludedData = (IncludedData)Enum.Parse(typeof(IncludedData), includedData);
            headers?.AddTo(options.Headers);
            resourceAttributes?.AddTo(options.ResourceAttributes);
        });
    }
}
