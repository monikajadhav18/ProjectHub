using System;

namespace ProjectHub.Domain.Exceptions;

/// <summary>
/// Thrown when a business rule inside the domain is violated.
/// In a later phase, global exception handling will turn this into an HTTP 400/422 response.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}