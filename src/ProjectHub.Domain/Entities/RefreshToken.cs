using System;
using ProjectHub.Domain.Common;
using ProjectHub.Domain.Exceptions;

namespace ProjectHub.Domain.Entities;

public class RefreshToken : BaseEntity
{
    // A SHA-256 hash written as hexadecimal is always 64 characters.
    public const int TokenHashLength = 64;

    // Needed by EF Core.
    private RefreshToken()
    {
    }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; private set; }

    public DateTime? RevokedAtUtc { get; private set; }

    public string? ReplacedByTokenHash { get; private set; }

    public bool IsRevoked => RevokedAtUtc.HasValue;

    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAtUtc;

    public bool IsActive(DateTime utcNow) => !IsRevoked && !IsExpired(utcNow);

    public static RefreshToken Create(Guid userId, string tokenHash, DateTime expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(tokenHash) || tokenHash.Length != TokenHashLength)
        {
            throw new DomainException("Refresh token hash is invalid.");
        }

        return new RefreshToken
        {
            UserId = Guard.RequiredId(userId, "User"),
            TokenHash = tokenHash,
            ExpiresAtUtc = expiresAtUtc
        };
    }

    public void Revoke(DateTime utcNow, string? replacedByTokenHash = null)
    {
        if (IsRevoked)
        {
            return;
        }

        RevokedAtUtc = utcNow;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}