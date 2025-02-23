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
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.Resilient.OTel.FileFallback;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Serilog.Sinks.Resilient.OTel;

/// <summary>
/// Initialization options for <see cref="OpenTelemetrySink"/>.
/// </summary>
public class OpenTelemetrySinkOptions
{
    internal const string DefaultEndpoint = "http://localhost:4317";
    internal const OtlpProtocol DefaultProtocol = OtlpProtocol.Grpc;

    internal const IncludedData DefaultIncludedData = IncludedData.MessageTemplateTextAttribute |
                                             IncludedData.TraceIdField | IncludedData.SpanIdField |
                                             IncludedData.SpecRequiredResourceAttributes;

    string? _endpoint = DefaultEndpoint;
    string? _logsEndpoint, _tracesEndpoint;

    /// <summary>
    /// The URL of the OTLP exporter endpoint. This should include full scheme, host, and port information. When the
    /// protocol is <see cref="OtlpProtocol.HttpProtobuf"/>, this may also include path information, but the standard
    /// OTLP path components <c>/v1/logs</c> and <c>/v1/traces</c> should not be specified, and will be trimmed if
    /// present. Set this value to <c langword="null"/> and specify one of either <see cref="LogsEndpoint"/> or
    /// <see cref="TracesEndpoint"/> if only a single signal is desired.
    /// </summary>
    public string? Endpoint
    {
        get => _endpoint;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                _endpoint = null;
                return;
            }

            var endpoint = value!.Trim().TrimEnd('/');
            if (endpoint.EndsWith("/v1/logs"))
                endpoint = endpoint.Substring(0, endpoint.Length - "/v1/logs".Length);
            else if (endpoint.EndsWith("/v1/traces"))
                endpoint = endpoint.Substring(0, endpoint.Length - "/v1/traces".Length);
            _endpoint = endpoint;
        }
    }
    
    /// <summary>
    /// Override the URL for the OTLP exporter logs endpoint. This should be a full URL, and if the protocol is
    /// <see cref="OtlpProtocol.HttpProtobuf"/> this should include path components like <c>/v1/logs</c>. By default,
    /// an endpoint will be computed from <see cref="Endpoint"/>.
    /// </summary>
    public string? LogsEndpoint
    {
        get => _logsEndpoint ??
               (Protocol == OtlpProtocol.HttpProtobuf ?
                   _endpoint != null ? $"{_endpoint}/v1/logs" : null :
                   _endpoint);
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                _logsEndpoint = null;
                return;
            }

            _logsEndpoint = value!.Trim();
        }
    }

    /// <summary>
    /// Override the URL for the OTLP exporter traces endpoint. This should be a full URL, and if the protocol is
    /// <see cref="OtlpProtocol.HttpProtobuf"/> this should include path components like <c>/v1/traces</c>. By default,
    /// an endpoint will be computed from <see cref="Endpoint"/>.
    /// </summary>
    /// <remarks>Log events reaching the sink will be considered spans if they carry a <see cref="DateTime"/>
    /// <c>SpanStartTimestamp</c> property. Additional <c>ParentSpanId</c> and <c>SpanKind</c> properties are
    /// recognized. The SerilogTracing project can be used to generate spans, or these can be manually constructed
    /// <see cref="LogEvent"/>s.</remarks>
    public string? TracesEndpoint
    {
        get => _tracesEndpoint ??
               (Protocol == OtlpProtocol.HttpProtobuf ?
                   _endpoint != null ? $"{_endpoint}/v1/traces" : null :
                   _endpoint);
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                _tracesEndpoint = null;
                return;
            }

            _tracesEndpoint = value!.Trim();
        }
    }
    
    /// <summary>
    /// A custom HTTP message handler. To suppress tracing of HTTP requests from the sink, set the handler to
    /// a <c>SocketsHttpHandler</c> with <c>null</c> <c>ActivityHeadersPropagator</c>:
    /// <code>
    /// options.HttpMessageHandler = new SocketsHttpHandler { ActivityHeadersPropagator = null };
    /// </code>
    /// </summary>
    public HttpMessageHandler? HttpMessageHandler { get; set; }

    /// <summary>
    /// The OTLP protocol to use.
    /// </summary>
    public OtlpProtocol Protocol { get; set; } = DefaultProtocol;

    /// <summary>
    /// A attributes of the resource attached to the logs generated by the sink. The values must be simple primitive
    /// values: integers, doubles, strings, or booleans. Other values will be silently ignored.
    /// </summary>
    public IDictionary<string, object> ResourceAttributes { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Which fields should be included in the log events generated by the sink. The default is to include <c>TraceId</c>
    /// and <c>SpanId</c> when <see cref="Activity.Current"/> is not null, and <c>message_template.text</c>.
    /// </summary>
    public IncludedData IncludedData { get; set; } = DefaultIncludedData;

    /// <summary>
    /// Headers to send with network requests.
    /// </summary>
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Supplies culture-specific formatting information, or null.
    /// </summary>
    public IFormatProvider? FormatProvider { get; set; }

    /// <summary>
    /// The minimum level for events passed through the sink. The default value is to not restrict events based on
    /// level. Ignored when <see cref="LevelSwitch"/> is specified.
    /// </summary>
    public LogEventLevel RestrictedToMinimumLevel { get; set; } = LevelAlias.Minimum;

    /// <summary>
    /// A switch allowing the pass-through minimum level
    /// to be changed at runtime.
    /// </summary>
    public LoggingLevelSwitch? LevelSwitch { get; set; }
    
    /// <summary>
    /// A callback used by the sink before triggering behaviors that may themselves generate log or trace information.
    /// Set this value to <c>OpenTelemetry.SuppressInstrumentationScope.Begin</c> when using this sink in applications
    /// that instrument HTTP or gRPC requests using OpenTelemetry.
    /// </summary>
    /// <example>
    /// options.OnBeginSuppressInstrumentation = OpenTelemetry.SuppressInstrumentationScope.Begin;
    /// </example>
    /// <remarks>This callback accepts a <c langword="bool"/> in order to match the signature of the OpenTelemetry SDK method
    /// that is typically assigned to it. The sink always provides the callback with the value <c langword="true" />.</remarks>
    public Func<bool, IDisposable>? OnBeginSuppressInstrumentation { get; set; }

    internal FallbackConfigurationOptions Fallback { get; set; } = new();

    /// <summary>
    /// Configures the fallback options for the OpenTelemetry sink. Can be used with either a Fallback for both
    /// sinks or a separate one for each of the Log and Trace endpoints.
    /// If no fallback is configured, the OpenTelemetry sink will graciously discard
    /// failed messages.
    /// </summary>
    /// <param name="configure">
    /// An action that configures the fallback options.
    /// </param>
    public void FallbackWith(Action<FallbackConfigurationOptions> configure)
    {
        configure(Fallback);
    }

    /// <summary>
    /// Configures the fallback options for the OpenTelemetry sink. If granular control
    /// is required for configuring the Logs and Traces fallbacks separately,
    /// use <see cref="FallbackWith(Action{FallbackConfigurationOptions})"/> instead.
    /// If no fallback is configured, the OpenTelemetry sink will graciously discard
    /// failed messages.
    /// </summary>
    /// <param name="fileSinkOptions">
    /// The file sink configuration for the OpenTelemetry fallback.
    /// </param>
    /// <param name="logFormat">
    /// The format that the fallback logs will be written in.
    /// See <see cref="LogFormat"/> for available formats.
    /// </param>
    public void FallbackWith(Action<FileSinkOptions> fileSinkOptions, LogFormat logFormat)
    {
        Fallback.ToFile(fileSinkOptions, logFormat);
    }
}
