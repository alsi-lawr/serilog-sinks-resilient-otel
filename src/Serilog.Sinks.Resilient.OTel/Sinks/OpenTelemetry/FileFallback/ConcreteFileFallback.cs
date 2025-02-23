﻿using Google.Protobuf;
using Serilog.Core;
using Serilog.Sinks.Resilient.OTel.Exporters.ExportResults;
using Serilog.Sinks.Resilient.OTel.FileFallback.Formatters;

namespace Serilog.Sinks.Resilient.OTel.FileFallback
{
    internal class ConcreteFileFallback : IDisposable
    {
        private readonly FormattedLogger? _logger;

        public ConcreteFileFallback(FileSystemFallback fallback)
        {
            _logger = GetLogger(fallback);
        }

        public Task LogToFallBack(Task<ExportResult> result,
            IMessage message)
        {
            return result.Match(
                onSuccess: _ => { },
                onFailure: result =>
                {
                    _logger?.Write(message);
                    result.Rethrow();
                });
        }

        public void LogToFallBack(ExportResult result,
            IMessage message)
        {
            result.Match(
                onSuccess: _ => { },
                onFailure: result =>
                {
                    _logger?.Write(message);
                    result.Rethrow();
                });
        }

        private static FormattedLogger? GetLogger(FileSystemFallback fallback)
        {
            if (!fallback.IsEnabled)
            {
                return null;
            }

            return new FormattedLogger()
            {
                Logger = new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .WriteTo.FallbackFile(fallback.FileOptions)
                    .CreateLogger(),
                LogEventFormatter = GetFormatter(fallback.LogFormat),
            };
        }

        private static ILogEventFormatter GetFormatter(LogFormat logFormat) => logFormat switch
        {
            LogFormat.NDJson => new JsonToLogEvent(),
            LogFormat.Protobuf => new ProtobufToLogEvent(),
            _ => throw new NotImplementedException(),
        };

        public void Dispose()
        {
            _logger?.Logger.Dispose();
        }

        private record struct FormattedLogger
        {
            public Logger Logger { get; set; }

            public ILogEventFormatter LogEventFormatter { get; set; }

            public void Write(IMessage message)
            {
                // When instantiated using "default" or without appropriate parameters.
                if (Logger is null || LogEventFormatter is null)
                {
                    return;
                }

                Logger.Write(LogEventFormatter.ToLogEvent(message));
            }
        }
    }
}
