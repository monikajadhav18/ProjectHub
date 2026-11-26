using System;

namespace ProjectHub.Application.Common.Interfaces;

/// <summary>Who is calling? Implemented in the API layer, so Application never touches HttpContext.</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }

    bool IsAdmin { get; }
}