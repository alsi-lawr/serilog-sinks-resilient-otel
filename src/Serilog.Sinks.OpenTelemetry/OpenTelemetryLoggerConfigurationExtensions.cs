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

using System.Diagnostics;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.OpenTelemetry;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog;

/// <summary>
/// Adds OpenTelemetry sink configuration methods to <see cref="LoggerSinkConfiguration"/>.
/// </summary>
public static class OpenTelemetryLoggerConfigurationExtensions
{
    /// <summary>
    /// Send log events to an OTLP exporter.
    /// </summary>
    public static LoggerConfiguration OpenTelemetry(
        this LoggerSinkConfiguration loggerSinkConfiguration,
        Action<OpenTelemetrySinkOptions> configure)
    {
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        var options = new OpenTelemetrySinkOptions();
        configure(options);

        var collector = new ActivityContextCollector();
        
        var openTelemetrySink = new OpenTelemetrySink(
            endpoint: options.Endpoint,
            protocol: options.Protocol,
            formatProvider: options.FormatProvider,
            resourceAttributes: options.ResourceAttributes,
            headers: options.Headers,
            includedData: options.IncludedData,
            httpMessageHandler: options.HttpMessageHandler,
            activityContextCollector: collector);

        ILogEventSink sink = openTelemetrySink;
        if (options.BatchingOptions != null)
        {
            sink = new PeriodicBatchingSink(openTelemetrySink, options.BatchingOptions);
        }

        sink = new ActivityContextCollectorSink(collector, sink);
        
        return loggerSinkConfiguration.Sink(sink, options.RestrictedToMinimumLevel, options.LevelSwitch);
    }

    /// <summary>
    /// Send log events to an OTLP exporter.
    /// </summary>
    /// <param name="loggerSinkConfiguration">
    /// The logger configuration.
    /// </param>
    /// <param name="endpoint">
    /// The full URL of the OTLP exporter endpoint.
    /// </param>
    /// <param name="httpMessageHandler">
    /// Custom HTTP message handler.
    /// </param>
    /// <param name="protocol">
    /// The OTLP protocol to use.
    /// </param>
    /// <param name="resourceAttributes">
    /// A attributes of the resource attached to the logs generated by the sink. The values must be simple primitive
    /// values: integers, doubles, strings, or booleans. Other values will be silently ignored.
    /// </param>
    /// <param name="headers">
    /// Headers to send with network requests.
    /// </param>
    /// <param name="formatProvider">
    /// Supplies culture-specific formatting information, or null.
    /// </param>
    /// <param name="restrictedToMinimumLevel">
    /// The minimum level for events passed through the sink. The default value is to not restrict events based on
    /// level. Ignored when <paramref name="levelSwitch"/> is specified.
    /// </param>
    /// <param name="levelSwitch">A switch allowing the pass-through minimum level
    /// to be changed at runtime.</param>
    /// <param name="batchSizeLimit">
    /// The maximum number of log events to include in a single batch.
    /// </param>
    /// <param name="period">
    /// The time to wait between checking for event batches. The default is two seconds.
    /// </param>
    /// <param name="queueLimit">
    /// The maximum number of events to hold in the sink's internal queue, or <c>null</c>
    /// for an unbounded queue. The default is <c>100000</c>.
    /// </param>
    /// <param name="disableBatching">
    /// The flag disabling batching in the sink.
    /// </param>
    /// <param name="includedData">
    /// Which fields should be included in the log events generated by the sink. The default is to include <c>TraceId</c>
    /// and <c>SpanId</c> when <see cref="Activity.Current"/> is not null, and <c>message_template.text</c>.
    /// </param>
    /// <returns>Logger configuration, allowing configuration to continue.</returns>
    public static LoggerConfiguration OpenTelemetry(
        this LoggerSinkConfiguration loggerSinkConfiguration,
        string endpoint = OpenTelemetrySinkOptions.DefaultEndpoint,
        HttpMessageHandler? httpMessageHandler = null,
        OtlpProtocol protocol = OpenTelemetrySinkOptions.DefaultProtocol,
        IDictionary<string, object>? resourceAttributes = null,
        IDictionary<string, string>? headers = null,
        IFormatProvider? formatProvider = null,
        LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
        LoggingLevelSwitch? levelSwitch = null,
        int batchSizeLimit = OpenTelemetrySinkOptions.DefaultBatchSizeLimit,
        TimeSpan? period = null,
        int queueLimit = OpenTelemetrySinkOptions.DefaultQueueLimit,
        bool disableBatching = false,
        IncludedData includedData = OpenTelemetrySinkOptions.DefaultIncludedData)
    {
        if (loggerSinkConfiguration == null) throw new ArgumentNullException(nameof(loggerSinkConfiguration));

        return loggerSinkConfiguration.OpenTelemetry(options =>
        {
            options.Endpoint = endpoint;
            options.Protocol = protocol;
            options.FormatProvider = formatProvider;
            options.ResourceAttributes = resourceAttributes ?? new Dictionary<string, object>();
            options.Headers = headers ?? new Dictionary<string, string>();
            options.RestrictedToMinimumLevel = restrictedToMinimumLevel;
            options.LevelSwitch = levelSwitch;
            options.IncludedData = includedData;
            options.HttpMessageHandler = httpMessageHandler;
            
            if (disableBatching)
            {
                options.BatchingOptions = null;
            }
            else
            {
                // Preserve the defaults to maintain consistency between overloads.
                options.BatchingOptions ??= new PeriodicBatchingSinkOptions();
                options.BatchingOptions.BatchSizeLimit = batchSizeLimit;
                options.BatchingOptions.Period =
                    period ?? TimeSpan.FromSeconds(OpenTelemetrySinkOptions.DefaultPeriodSeconds);
                options.BatchingOptions.QueueLimit = queueLimit;
            }
        });
    }
}
