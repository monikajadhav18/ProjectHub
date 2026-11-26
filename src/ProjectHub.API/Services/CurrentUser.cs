using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ProjectHub.Application.Common.Constants;
using ProjectHub.Application.Common.Interfaces;

namespace ProjectHub.API.Services;

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var value = Principal?.FindFirst("sub")?.Value;

            return Guid.TryParse(value, out var userId) ? userId : null;
        }
    }

    public bool IsAdmin => Principal?.IsInRole(Roles.Admin) ?? false;
}