// Copyright © Serilog Contributors
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

using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Collector.Trace.V1;
using Serilog.Sinks.Resilient.OTel.Exporters.ExportResults;

namespace Serilog.Sinks.Resilient.OTel.Exporters;

/// <summary>
/// Uses a callback to suppress instrumentation while telemetry is being exported by the wrapped exporter.
/// </summary>
sealed class InstrumentationSuppressingExporter : IExporter, IDisposable
{
    readonly IExporter _exporter;
    readonly Func<IDisposable> _onBeginSuppressInstrumentation;

    public InstrumentationSuppressingExporter(IExporter exporter, Func<IDisposable> onBeginSuppressInstrumentation)
    {
        _exporter = exporter;
        _onBeginSuppressInstrumentation = onBeginSuppressInstrumentation;
    }

    public ExportResult Export(ExportLogsServiceRequest request)
    {
        using (_onBeginSuppressInstrumentation())
        {
            return _exporter.Export(request);
        }
    }

    public async Task<ExportResult> ExportAsync(ExportLogsServiceRequest request)
    {
        using (_onBeginSuppressInstrumentation())
        {
            return await _exporter.ExportAsync(request);
        }
    }

    public ExportResult Export(ExportTraceServiceRequest request)
    {
        using (_onBeginSuppressInstrumentation())
        {
            return _exporter.Export(request);
        }
    }

    public async Task<ExportResult> ExportAsync(ExportTraceServiceRequest request)
    {
        using (_onBeginSuppressInstrumentation())
        {
            return await _exporter.ExportAsync(request);
        }
    }

    public void Dispose()
    {
        (_exporter as IDisposable)?.Dispose();
    }
}
