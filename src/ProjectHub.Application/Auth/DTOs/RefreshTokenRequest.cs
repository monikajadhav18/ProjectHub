using System.ComponentModel.DataAnnotations;

namespace ProjectHub.Application.Auth.DTOs;

/// <summary>Used by both the refresh and logout endpoints.</summary>
public sealed class RefreshTokenRequest
{
    [Required]
    [StringLength(500)]
    public string RefreshToken { get; init; } = string.Empty;
}