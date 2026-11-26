using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectHub.Application.Auth;
using ProjectHub.Application.Auth.DTOs;
using ProjectHub.Application.Common.Constants;
using ProjectHub.Application.Common.Results;
using ProjectHub.Domain.Entities;
using ProjectHub.Infrastructure.Persistence;

namespace ProjectHub.Infrastructure.Identity;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IJwtTokenService _tokenService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext,
        IJwtTokenService tokenService,
        TimeProvider timeProvider,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _tokenService = tokenService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<Result<UserDto>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();

        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            return Result<UserDto>.Failure(Error.Conflict("A user with this email already exists."));
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = request.FullName.Trim()
        };

        // Creating the user and assigning the role must succeed or fail together.
        // UserManager uses the same scoped DbContext, so this transaction covers both calls.
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return Result<UserDto>.Failure(ToError(createResult));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, Roles.Employee);
        if (!roleResult.Succeeded)
        {
            // Returning without Commit rolls the transaction back when it is disposed.
            return Result<UserDto>.Failure(ToError(roleResult));
        }

        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation("User {UserId} registered.", user.Id);

        return Result<UserDto>.Success(ToUserDto(user, new[] { Roles.Employee }));
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        // One generic message for "unknown email" and "wrong password" so attackers can't discover which emails exist.
        var invalidCredentials = Result<AuthResponse>.Failure(Error.Unauthorized("Invalid email or password."));

        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            return invalidCredentials;
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return Result<AuthResponse>.Failure(
                Error.Unauthorized("This account is temporarily locked. Please try again later."));
        }

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            await _userManager.AccessFailedAsync(user);
            return invalidCredentials;
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        var roles = (await _userManager.GetRolesAsync(user)).ToArray();
        var response = await IssueTokensAsync(user, roles, tokenToReplace: null, cancellationToken);

        _logger.LogInformation("User {UserId} logged in.", user.Id);

        return Result<AuthResponse>.Success(response);
    }

    public async Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var invalidToken = Result<AuthResponse>.Failure(Error.Unauthorized("Invalid or expired refresh token."));
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var tokenHash = _tokenService.HashToken(request.RefreshToken);
        var storedToken = await _dbContext.RefreshTokens
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null)
        {
            return invalidToken;
        }

        if (storedToken.IsRevoked)
        {
            // An already-used token came back: it may have been stolen. End every session for this user.
            await RevokeAllActiveTokensAsync(storedToken.UserId, now, cancellationToken);
            _logger.LogWarning("A revoked refresh token was reused for user {UserId}.", storedToken.UserId);
            return invalidToken;
        }

        if (storedToken.IsExpired(now))
        {
            return invalidToken;
        }

        var user = await _userManager.FindByIdAsync(storedToken.UserId.ToString());
        if (user is null || await _userManager.IsLockedOutAsync(user))
        {
            return invalidToken;
        }

        var roles = (await _userManager.GetRolesAsync(user)).ToArray();
        var response = await IssueTokensAsync(user, roles, storedToken, cancellationToken);

        return Result<AuthResponse>.Success(response);
    }

    public async Task<Result> LogoutAsync(Guid userId, string refreshToken, CancellationToken cancellationToken)
    {
        var tokenHash = _tokenService.HashToken(refreshToken);

        // Only the owner can revoke a token. Logout is idempotent: an unknown or already-revoked token is still a success.
        var storedToken = await _dbContext.RefreshTokens
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash && t.UserId == userId, cancellationToken);

        if (storedToken is not null && !storedToken.IsRevoked)
        {
            storedToken.Revoke(_timeProvider.GetUtcNow().UtcDateTime);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }

    public async Task<Result<UserDto>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result<UserDto>.Failure(Error.NotFound("User was not found."));
        }

        var roles = (await _userManager.GetRolesAsync(user)).ToArray();

        return Result<UserDto>.Success(ToUserDto(user, roles));
    }

    private async Task<AuthResponse> IssueTokensAsync(
        ApplicationUser user,
        IReadOnlyCollection<string> roles,
        RefreshToken? tokenToReplace,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var accessToken = _tokenService.CreateAccessToken(user, roles);
        var refreshToken = _tokenService.CreateRefreshToken();

        // Rotation: the old token is revoked and the new one saved in the same SaveChanges (one transaction).
        tokenToReplace?.Revoke(now, refreshToken.TokenHash);

        _dbContext.RefreshTokens.Add(
            RefreshToken.Create(user.Id, refreshToken.TokenHash, refreshToken.ExpiresAtUtc));

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            refreshToken.Token,
            refreshToken.ExpiresAtUtc,
            ToUserDto(user, roles));
    }

    private async Task RevokeAllActiveTokensAsync(Guid userId, DateTime now, CancellationToken cancellationToken)
    {
        var activeTokens = await _dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null && t.ExpiresAtUtc > now)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.Revoke(now);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static UserDto ToUserDto(ApplicationUser user, IReadOnlyCollection<string> roles)
        => new(user.Id, user.FullName, user.Email ?? string.Empty, roles);

    private static Error ToError(IdentityResult result)
    {
        var isDuplicate = result.Errors.Any(e => e.Code is "DuplicateEmail" or "DuplicateUserName");

        if (isDuplicate)
        {
            return Error.Conflict("A user with this email already exists.");
        }

        var descriptions = result.Errors.Select(e => e.Description).ToArray();

        return Error.Validation("The request could not be completed.", descriptions);
    }
}