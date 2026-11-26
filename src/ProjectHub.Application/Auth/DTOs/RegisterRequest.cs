using System.ComponentModel.DataAnnotations;

namespace ProjectHub.Application.Auth.DTOs;

public sealed class RegisterRequest
{
    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string FullName { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; init; } = string.Empty;

    // The upper limit stops someone from sending a huge string to make the server hash it.
    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; init; } = string.Empty;
}