using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectHub.Application.Common.Results;

namespace ProjectHub.API.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Turns an application error into an RFC 7807 ProblemDetails response with the right status code.</summary>
    protected IActionResult ToProblem(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = error.Message
        };

        if (error.Details is { Count: > 0 })
        {
            problem.Extensions["errors"] = error.Details;
        }

        return new ObjectResult(problem) { StatusCode = statusCode };
    }

    /// <summary>Reads the user id from the "sub" claim of the validated JWT.</summary>
    protected Guid? GetCurrentUserId()
    {
        var value = User.FindFirst("sub")?.Value;

        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}