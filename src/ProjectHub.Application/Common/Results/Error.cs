using System.Collections.Generic;

namespace ProjectHub.Application.Common.Results;

public enum ErrorType
{
    Validation,
    Unauthorized,
    NotFound,
    Conflict
}

public sealed record Error(ErrorType Type, string Message, IReadOnlyCollection<string>? Details = null)
{
    public static Error Validation(string message, IReadOnlyCollection<string>? details = null)
        => new(ErrorType.Validation, message, details);

    public static Error Unauthorized(string message)
        => new(ErrorType.Unauthorized, message);

    public static Error NotFound(string message)
        => new(ErrorType.NotFound, message);

    public static Error Conflict(string message)
        => new(ErrorType.Conflict, message);
}