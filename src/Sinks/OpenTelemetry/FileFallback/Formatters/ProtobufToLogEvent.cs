using Google.Protobuf;
using Serilog.Events;

namespace Serilog.Sinks.Resilient.OTel.FileFallback.Formatters
{
    internal class ProtobufToLogEvent : ILogEventFormatter
    {
        public LogEvent ToLogEvent(IMessage message) =>
            LogEventGenerator.GenerateLogEvent(new ProtobufLogProperty(message));
    }
}
