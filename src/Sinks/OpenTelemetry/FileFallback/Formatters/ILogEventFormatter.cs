using Google.Protobuf;
using Serilog.Events;

namespace Serilog.Sinks.Resilient.OTel.FileFallback.Formatters
{
    internal interface ILogEventFormatter
    {
        public LogEvent ToLogEvent(IMessage message);
    }
}
