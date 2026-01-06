using Serilog.Events;
using Serilog.Formatting;

namespace Serilog.Sinks.Resilient.OTel.FileFallback.Formatters
{
    internal class OtlpFormatter : ITextFormatter
    {
        public static readonly string OtlpMessageContents = "OtlpMessageContents";

        public void Format(LogEvent logEvent, TextWriter output)
        {
            if (!logEvent.Properties.ContainsKey(OtlpMessageContents))
            {
                return;
            }

            var exportServiceRequest = logEvent.Properties["OtlpMessageContents"];
            exportServiceRequest.Render(output);
        }
    }
}
