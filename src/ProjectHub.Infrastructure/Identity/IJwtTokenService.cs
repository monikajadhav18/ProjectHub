using System;
using System.Collections.Generic;

namespace ProjectHub.Infrastructure.Identity;

public interface IJwtTokenService
{
    AccessTokenResult CreateAccessToken(ApplicationUser user, IReadOnlyCollection<string> roles);

    RefreshTokenResult CreateRefreshToken();

    string HashToken(string token);
}

public sealed record AccessTokenResult(string Token, DateTime ExpiresAtUtc);

/// <summary>Token is the raw value sent to the client. Only TokenHash is stored in the database.</summary>
public sealed record RefreshTokenResult(string Token, string TokenHash, DateTime ExpiresAtUtc);