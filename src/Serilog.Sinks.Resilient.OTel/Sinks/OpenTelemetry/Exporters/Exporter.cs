﻿// Copyright 2022 Serilog Contributors
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

namespace Serilog.Sinks.Resilient.OTel.Exporters;

static class Exporter
{
    public static IExporter Create(
        string? logsEndpoint,
        string? tracesEndpoint,
        OtlpProtocol protocol,
        IReadOnlyDictionary<string, string> headers,
        HttpMessageHandler? httpMessageHandler,
        Func<IDisposable>? onBeginSuppressInstrumentation)
    {
        var exporter = protocol switch
        {
            OtlpProtocol.HttpProtobuf => (IExporter)new HttpExporter(logsEndpoint, tracesEndpoint, headers, httpMessageHandler),
            OtlpProtocol.Grpc => new GrpcExporter(logsEndpoint, tracesEndpoint, headers, httpMessageHandler),
            _ => throw new NotSupportedException($"OTLP protocol {protocol} is unsupported.")
        };

        return onBeginSuppressInstrumentation != null
            ? new InstrumentationSuppressingExporter(exporter, onBeginSuppressInstrumentation)
            : exporter;
    }
}