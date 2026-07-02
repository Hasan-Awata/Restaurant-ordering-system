using OrderingSystem.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderingSystem.Domain.Common
{
    public class Result
    {
        public bool IsSuccess { get; }
        public string? ErrorMessage { get; }
        public enErrorType ErrorType { get; }

        protected Result(bool isSuccess, string? errorMessage, enErrorType errorType)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
            ErrorType = errorType;
        }

        public static Result Success() => new(true, null, enErrorType.None);
        public static Result Failure(string message, enErrorType errorType = enErrorType.Failure)
            => new(false, message, errorType);
    }

    public class Result<T> : Result
    {
        public T? Value { get; }

        private Result(T? value, bool isSuccess, string? errorMessage, enErrorType errorType)
            : base(isSuccess, errorMessage, errorType)
        {
            Value = value;
        }

        public static Result<T> Success(T value) => new(value, true, null, enErrorType.None);
        public static new Result<T> Failure(string message, enErrorType errorType = enErrorType.Failure)
            => new(default, false, message, errorType);
    }
}