using OpenTelemetry.Proto.Collector.Logs.V1;
using Serilog.Sinks.Resilient.OTel.Exporters;
using Serilog.Sinks.Resilient.OTel.Tests.Support;
using Xunit;

namespace Serilog.Sinks.Resilient.OTel.Tests;

public class InstrumentationSuppressingExporterTests
{
    [Fact]
    public void RequestsAreNotInstrumentedWhenSuppressed()
    {
        var exporter = new CollectingExporter();
        
        exporter.Export(new ExportLogsServiceRequest());
        Assert.Equal(1, exporter.InstrumentedRequestCount);
        Assert.Single(exporter.ExportLogsServiceRequests);

        var wrapper = new InstrumentationSuppressingExporter(exporter, TestSuppressInstrumentationScope.Begin);
        wrapper.Export(new ExportLogsServiceRequest());
        Assert.Equal(1, exporter.InstrumentedRequestCount);
        Assert.Equal(2, exporter.ExportLogsServiceRequests.Count);
    }
}
