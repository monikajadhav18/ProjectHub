using System;
using System.Threading;
using System.Threading.Tasks;
using ProjectHub.Application.Auth.DTOs;
using ProjectHub.Application.Common.Results;

namespace ProjectHub.Application.Auth;

public interface IAuthService
{
    Task<Result<UserDto>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken);

    Task<Result> LogoutAsync(Guid userId, string refreshToken, CancellationToken cancellationToken);

    Task<Result<UserDto>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken);
}