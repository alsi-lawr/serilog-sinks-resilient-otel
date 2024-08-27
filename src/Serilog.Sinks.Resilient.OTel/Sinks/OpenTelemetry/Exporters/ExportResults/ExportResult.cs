﻿using System.Runtime.ExceptionServices;

namespace Serilog.Sinks.Resilient.OTel.Exporters.ExportResults
{
    internal record struct ExportResult
    {
        private bool _isSuccess;

        internal ExceptionDispatchInfo? Exception { get; private set; }

        public readonly bool IsSuccess => Exception is null && _isSuccess;

        public readonly bool IsFailure => Exception is not null || !_isSuccess;

        public static ExportResult Success() => new() { _isSuccess = true };

        public static ExportResult Failure() => new() { _isSuccess = false };

        public static Task<ExportResult> SuccessTask() => Task.FromResult(Success());

        public static Task<ExportResult> FailureTask() => Task.FromResult(Failure());

        public ExportResult WithException(ExceptionDispatchInfo? ex) => this with
        {
            Exception = ex
        };

        public void Rethrow()
        {
            Exception?.Throw();
        }
    }
}
